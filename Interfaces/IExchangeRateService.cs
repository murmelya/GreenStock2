using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenStock.Interfaces;

public interface IExchangeRateService
{
    Task<Dictionary<string, decimal>?> FetchRatesAsync();
    Dictionary<string, decimal>? FetchRates();
}