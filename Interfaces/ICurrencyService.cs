using GreenStock.Services;
using System.Collections.Generic;

namespace GreenStock.Interfaces;

public interface ICurrencyService
{
    Currency CurrentCurrency { get; }
    void SetCurrency(Currency currency);
    decimal GetRate(Currency currency);
    void SetRates(Dictionary<string, decimal> rates);
    decimal Convert(decimal amount, Currency from, Currency to);
    string GetSymbol(Currency currency);
    string GetCode(Currency currency);
    string Format(decimal amount, Currency? currency = null);
    List<Currency> GetAvailableCurrencies();
    decimal ConvertFromPurchaseCurrency(decimal amount, string purchaseCurrency, decimal purchaseRate, Currency targetCurrency);
}