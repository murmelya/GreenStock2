using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using GreenStock.Interfaces;

namespace GreenStock.Services;

public class WeatherForecast
{
    public string Region { get; set; } = string.Empty;
    public DateTime ForecastDate { get; set; }
    public decimal TemperatureC { get; set; }
    public int Humidity { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public bool NeedsThermoContainer { get; set; }
    public bool NeedsInsurance { get; set; }
}

public class WeatherLogisticsService : IWeatherLogisticsService
{
    private readonly HttpClient _httpClient;

    private readonly Dictionary<string, (double Lat, double Lon)> _regionCoords = new()
    {
        ["Москва"] = (55.7558, 37.6173),
        ["Санкт-Петербург"] = (59.9343, 30.3351),
        ["Новосибирск"] = (55.0084, 82.9357),
        ["Екатеринбург"] = (56.8389, 60.6057),
        ["Казань"] = (55.7887, 49.1221),
        ["Красноярск"] = (56.0106, 92.8526),
        ["Сочи"] = (43.5855, 39.7231),
        ["Владивосток"] = (43.1155, 131.8855)
    };

    public WeatherLogisticsService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public WeatherLogisticsService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public List<string> GetSupportedRegions()
    {
        return _regionCoords.Keys.ToList();
    }

    public async Task<WeatherForecast?> GetForecastAsync(string region, DateTime date)
    {
        if (!_regionCoords.ContainsKey(region))
        {
            return new WeatherForecast
            {
                Region = region,
                ForecastDate = date,
                Warning = $"Регион '{region}' не поддерживается"
            };
        }

        var coords = _regionCoords[region];
        string lat = coords.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string lon = coords.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string targetDate = date.ToString("yyyy-MM-dd");

        // Запрашиваем прогноз на конкретную дату
        string url = $"https://api.open-meteo.com/v1/forecast?" +
                     $"latitude={lat}&longitude={lon}&" +
                     $"daily=temperature_2m_max,temperature_2m_min&" +
                     $"timezone=Europe/Moscow&" +
                     $"start_date={targetDate}&end_date={targetDate}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return GetFallbackForecast(region, date);
            }

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("daily", out var daily))
            {
                decimal tempMax = 0;
                decimal tempMin = 0;

                if (daily.TryGetProperty("temperature_2m_max", out var maxTemp))
                    tempMax = (decimal)maxTemp[0].GetDouble();

                if (daily.TryGetProperty("temperature_2m_min", out var minTemp))
                    tempMin = (decimal)minTemp[0].GetDouble();

                // Берём среднюю температуру на день
                var avgTemp = (tempMax + tempMin) / 2;

                var forecast = new WeatherForecast
                {
                    Region = region,
                    ForecastDate = date,
                    TemperatureC = Math.Round(avgTemp, 1),
                    Humidity = 65,
                    Condition = avgTemp >= 0 ? "Тепло" : "Холодно"
                };

                if (avgTemp <= -15)
                {
                    forecast.Warning = $"⚠️ Сильный мороз ({avgTemp}°C)! Требуется термоконтейнер!";
                    forecast.NeedsThermoContainer = true;
                    forecast.NeedsInsurance = true;
                }
                else if (avgTemp >= 30)
                {
                    forecast.Warning = $"⚠️ Сильная жара ({avgTemp}°C)! Рекомендуется термоконтейнер!";
                    forecast.NeedsThermoContainer = true;
                }
                else
                {
                    forecast.Warning = $"✅ Погода благоприятная: {avgTemp}°C";
                }

                return forecast;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"API error: {ex.Message}");
        }

        return GetFallbackForecast(region, date);
    }

    private WeatherForecast GetFallbackForecast(string region, DateTime date)
    {
        return new WeatherForecast
        {
            Region = region,
            ForecastDate = date,
            TemperatureC = 20,
            Humidity = 60,
            Condition = "Данные недоступны",
            Warning = "⚠️ Данные о погоде временно недоступны.",
            NeedsThermoContainer = false,
            NeedsInsurance = false
        };
    }

    public WeatherForecast? GetForecast(string region, DateTime date)
    {
        var task = GetForecastAsync(region, date);
        task.Wait(TimeSpan.FromSeconds(15));
        return task.Result;
    }
}