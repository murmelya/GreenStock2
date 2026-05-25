using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NLog;
using GreenStock.Interfaces;
using GreenStock.Logging;

namespace GreenStock.Services;

public class ExchangeRateService : IExchangeRateService 
{
    private readonly ILogger _log;
    private readonly HttpClient _httpClient;

    public ExchangeRateService(HttpClient httpClient)
    {
        _log = AppLogger.For("GreenStock.Services.ExchangeRateService");
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<Dictionary<string, decimal>?> FetchRatesAsync()  
    {
        try
        {
            var response = await _httpClient.GetAsync("https://api.exchangerate-api.com/v4/latest/USD");

            if (!response.IsSuccessStatusCode)
            {
                _log.Warn("Не удалось получить курсы валют. Код ответа: {0}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (!root.TryGetProperty("rates", out var ratesElement))
            {
                _log.Warn("Неверный формат ответа API");
                return null;
            }

            var rates = new Dictionary<string, decimal>
            {
                { "USD", 1.0m }
            };

            if (ratesElement.TryGetProperty("EUR", out var eurElement) && eurElement.TryGetDecimal(out var eur))
                rates["EUR"] = eur;

            if (ratesElement.TryGetProperty("RUB", out var rubElement) && rubElement.TryGetDecimal(out var rub))
                rates["RUB"] = rub;

            _log.Info("Курсы валют обновлены: USD=1, EUR={0}, RUB={1}",
                rates.GetValueOrDefault("EUR", 0), rates.GetValueOrDefault("RUB", 0));

            return rates;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Ошибка при получении курсов валют");
            return null;
        }
    }

    public Dictionary<string, decimal>? FetchRates()  
    {
        try
        {
            var task = FetchRatesAsync();
            task.Wait(TimeSpan.FromSeconds(5));
            return task.Result;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Ошибка при получении курсов валют (синхронно)");
            return null;
        }
    }
}
