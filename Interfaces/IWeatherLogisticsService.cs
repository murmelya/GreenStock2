using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GreenStock.Services;

namespace GreenStock.Interfaces;

public interface IWeatherLogisticsService
{
    Task<WeatherForecast?> GetForecastAsync(string region, DateTime date);
    WeatherForecast? GetForecast(string region, DateTime date);
    List<string> GetSupportedRegions();
}