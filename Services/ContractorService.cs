using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GreenStock.Data;
using GreenStock.Models;

namespace GreenStock.Services;

public class ContractorCheckResult
{
    public bool IsValid { get; set; }
    public string Inn { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Kpp { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CheckDate { get; set; }
    public bool IsInBlacklist { get; set; }
}

public static class ContractorService
{
    private static readonly HttpClient _httpClient = new();

    
    private const string DADATA_API_KEY = "5932f15f0a13e155e5a51284b8ae246203693d79";

    private static readonly HashSet<string> _blacklistInns = new()
{
    "7707083893",
    "7736050003",
    "7703027480",
    "7710140670"
};

    private const string DADATA_URL = "https://suggestions.dadata.ru/suggestions/api/4_1/rs/findById/party";

    public static async Task<ContractorCheckResult> CheckContractorAsync(string inn, string? checkedBy = null)
    {
        var result = new ContractorCheckResult
        {
            Inn = inn,
            CheckDate = DateTime.UtcNow,
            IsValid = true
        };

        
        if (string.IsNullOrWhiteSpace(inn))
        {
            result.IsValid = false;
            result.Status = "Ошибка";
            result.Reason = "ИНН не может быть пустым";
            return result;
        }

        var cleanInn = new string(inn.Where(char.IsDigit).ToArray());
        if (cleanInn.Length != 10 && cleanInn.Length != 12)
        {
            result.IsValid = false;
            result.Status = "Ошибка";
            result.Reason = "ИНН должен содержать 10 или 12 цифр";
            return result;
        }

        result.Inn = cleanInn;

        try
        {
           
            var request = new { query = cleanInn };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {DADATA_API_KEY}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await _httpClient.PostAsync(DADATA_URL, content);

            if (!response.IsSuccessStatusCode)
            {
                result.IsValid = false;
                result.Status = "Ошибка API";
                result.Reason = $"Сервис проверки недоступен ({(int)response.StatusCode})";
                return result;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            
            if (!root.TryGetProperty("suggestions", out var suggestions) || suggestions.GetArrayLength() == 0)
            {
                result.IsValid = false;
                result.Status = "Не найден";
                result.Reason = "Контрагент с таким ИНН не найден в ЕГРЮЛ";
                return result;
            }

            var company = suggestions[0].GetProperty("data");

            
            if (company.TryGetProperty("name", out var name))
            {
                if (name.TryGetProperty("short_with_opf", out var shortName))
                    result.Name = shortName.GetString() ?? "";
                else if (name.TryGetProperty("full_with_opf", out var fullName))
                    result.Name = fullName.GetString() ?? "";
            }

            // КПП
            if (company.TryGetProperty("kpp", out var kpp))
                result.Kpp = kpp.GetString() ?? "";

            // Адрес
            if (company.TryGetProperty("address", out var address))
            {
                if (address.TryGetProperty("value", out var addrValue))
                    result.Address = addrValue.GetString() ?? "";
            }

            string status = "ACTIVE";
            string statusReason = "";

            if (company.TryGetProperty("state", out var state))
            {
                if (state.TryGetProperty("status", out var statusElem))
                    status = statusElem.GetString() ?? "ACTIVE";
            }

            switch (status)
            {
                case "ACTIVE":
                    result.Status = "Чист";
                    result.Reason = "Компания действующая, проверка пройдена";
                    result.IsInBlacklist = false;
                    break;

                case "LIQUIDATING":
                    result.Status = "В ЧС";
                    result.Reason = "⚠️ Компания находится в процессе ликвидации";
                    result.IsInBlacklist = true;
                    break;

                case "LIQUIDATED":
                    result.Status = "В ЧС";
                    result.Reason = "❌ Компания ликвидирована! Отгрузка не рекомендуется";
                    result.IsInBlacklist = true;
                    break;

                case "BANKRUPT":
                    result.Status = "В ЧС";
                    result.Reason = "⚠️ Компания банкрот! Высокий риск";
                    result.IsInBlacklist = true;
                    break;

                case "REORGANIZING":
                    result.Status = "В ЧС";
                    result.Reason = "⚠️ Компания реорганизуется, возможны риски";
                    result.IsInBlacklist = true;
                    break;

                default:
                    result.Status = "Неизвестно";
                    result.Reason = "Статус компании не определён, рекомендуется дополнительная проверка";
                    result.IsInBlacklist = true;
                    break;
            }
            if (_blacklistInns.Contains(cleanInn))
            {
                result.IsInBlacklist = true;
                result.Status = "В ЧС";
                result.Reason = "⚠️ Компания в чёрном списке! Отгрузка не рекомендуется";
                result.Name = "ООО \"Проблемный контрагент\"";
            }
        }
        catch (HttpRequestException ex)
        {
            result.IsValid = false;
            result.Status = "Ошибка сети";
            result.Reason = $"Нет соединения с сервером: {ex.Message}";
            return result;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Status = "Ошибка";
            result.Reason = $"Ошибка проверки: {ex.Message}";
            return result;
        }

       
        await SaveCheckResultAsync(result, checkedBy);

        return result;
    }

    private static async Task SaveCheckResultAsync(ContractorCheckResult result, string? checkedBy)
    {
        using var db = new AppDbContext();

        
        var utcCheckDate = result.CheckDate.Kind == DateTimeKind.Utc
            ? result.CheckDate
            : result.CheckDate.ToUniversalTime();

        
        var contractor = db.Contractors.FirstOrDefault(c => c.Inn == result.Inn);
        if (contractor == null)
        {
            contractor = new Contractor
            {
                Id = Guid.NewGuid(),
                Inn = result.Inn,
                Name = result.Name,
                Kpp = result.Kpp,
                Address = result.Address,
                Status = result.Status,
                CheckDate = utcCheckDate,
                CheckReason = result.Reason,
                CheckedBy = checkedBy
            };
            db.Contractors.Add(contractor);
            await db.SaveChangesAsync();
        }
        else
        {
            contractor.Name = result.Name;
            contractor.Kpp = result.Kpp;
            contractor.Address = result.Address;
            contractor.Status = result.Status;
            contractor.CheckDate = utcCheckDate;
            contractor.CheckReason = result.Reason;
            contractor.CheckedBy = checkedBy;
            await db.SaveChangesAsync();
        }

     
        db.ContractorCheckHistories.Add(new ContractorCheckHistory
        {
            Id = Guid.NewGuid(),
            ContractorId = contractor.Id,
            CheckDate = utcCheckDate,
            Inn = result.Inn,
            Status = result.Status,
            Reason = result.Reason,
            CheckedBy = checkedBy
        });

        await db.SaveChangesAsync();
    }

    public static List<Contractor> GetAllContractors()
    {
        using var db = new AppDbContext();
        return db.Contractors.OrderByDescending(c => c.CheckDate).ToList();
    }

    public static List<ContractorCheckHistory> GetCheckHistory(Guid contractorId)
    {
        using var db = new AppDbContext();
        return db.ContractorCheckHistories
            .Where(h => h.ContractorId == contractorId)
            .OrderByDescending(h => h.CheckDate)
            .ToList();
    }
}
