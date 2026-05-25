using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenStock.Interfaces;  

namespace GreenStock.Services;


public class CounterpartyCheckResult
{
    public bool IsValid { get; set; }
    public string Inn { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
    public bool IsInBlacklist { get; set; }
    public string? BlacklistReason { get; set; }
}

public class CounterpartyService : ICounterpartyService  
{
    private readonly HashSet<string> _blacklistInns = new()  
    {
        "7707083893",
        "7736050003"
    };

    public async Task<CounterpartyCheckResult> CheckCounterpartyAsync(string inn)  
    {
        await Task.Delay(500);

        var result = new CounterpartyCheckResult { Inn = inn };

        if (string.IsNullOrWhiteSpace(inn))
        {
            result.IsValid = false;
            result.Warnings.Add("ИНН не может быть пустым");
            return result;
        }

        inn = new string(inn.Where(char.IsDigit).ToArray());

        if (inn.Length != 10 && inn.Length != 12)
        {
            result.IsValid = false;
            result.Warnings.Add("ИНН должен содержать 10 или 12 цифр");
            return result;
        }

        result.Inn = inn;
        result.IsValid = true;
        result.Name = $"ООО \"Контрагент\" (ИНН: {inn})";
        result.Status = "Действующее";

        if (_blacklistInns.Contains(inn))
        {
            result.IsInBlacklist = true;
            result.BlacklistReason = "Компания находится в списке дисквалифицированных лиц";
            result.Warnings.Add(result.BlacklistReason);
            result.IsValid = false;
        }

        return result;
    }

    public CounterpartyCheckResult CheckCounterparty(string inn)  
    {
        var task = CheckCounterpartyAsync(inn);
        task.Wait(TimeSpan.FromSeconds(10));
        return task.Result;
    }
}