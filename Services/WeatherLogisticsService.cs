using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    private static readonly string[] _supportedRegions = new[]
    {
        "Москва", "Санкт-Петербург", "Новосибирск", "Екатеринбург",
        "Казань", "Красноярск", "Сочи", "Владивосток"
    };

    public List<string> GetSupportedRegions()  
    {
        return _supportedRegions.ToList();
    }

    public async Task<WeatherForecast?> GetForecastAsync(string region, DateTime date)  
    {
        await Task.Delay(500);

        var random = new Random(date.GetHashCode() + region.GetHashCode());
        var temp = random.Next(-30, 40);
        var humidity = random.Next(30, 90);

        var forecast = new WeatherForecast
        {
            Region = region,
            ForecastDate = date,
            TemperatureC = temp,
            Humidity = humidity,
            Condition = temp >= 0 ? "Тепло" : "Холодно"
        };

        if (temp <= -15)
        {
            forecast.Warning = $"Ожидается сильный мороз ({temp}°C)";
            forecast.NeedsThermoContainer = true;
            forecast.NeedsInsurance = true;
        }
        else if (temp >= 30)
        {
            forecast.Warning = $"Ожидается сильная жара ({temp}°C)";
            forecast.NeedsThermoContainer = true;
        }
        else if (humidity > 70)
        {
            forecast.Warning = "Высокая влажность, требуется влагозащита";
        }
        else
        {
            forecast.Warning = $"Благоприятная погода: {temp}°C";
        }

        return forecast;
    }

    public WeatherForecast? GetForecast(string region, DateTime date)  
    {
        var task = GetForecastAsync(region, date);
        task.Wait(TimeSpan.FromSeconds(10));
        return task.Result;
    }
}