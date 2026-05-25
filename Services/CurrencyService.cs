using System;
using System.Collections.Generic;
using System.Linq;
using GreenStock.Interfaces;
using GreenStock.Services;

namespace GreenStock.Services;

public class CurrencyService : ICurrencyService
{
    private static CurrencyService? _instance;
    public static CurrencyService Instance => _instance ??= new CurrencyService();

    private Dictionary<string, decimal> _rates;
    private Currency _currentCurrency;

    public CurrencyService()
    {
        _rates = new Dictionary<string, decimal>
        {
            { "USD", 1.0m },
            { "EUR", 1.1m },
            { "RUB", 98.0m }
        };
        _currentCurrency = Currency.RUB;
    }

    public Currency CurrentCurrency => _currentCurrency;

    public void SetCurrency(Currency currency)
    {
        _currentCurrency = currency;
    }

    public decimal GetRate(Currency currency)
    {
        return currency switch
        {
            Currency.RUB => _rates["RUB"],
            Currency.USD => _rates["USD"],
            Currency.EUR => _rates["EUR"],
            _ => 1.0m
        };
    }

    public void SetRates(Dictionary<string, decimal> rates)
    {
        if (rates != null)
        {
            foreach (var kvp in rates)
            {
                if (_rates.ContainsKey(kvp.Key))
                    _rates[kvp.Key] = kvp.Value;
            }
        }
    }

    public decimal Convert(decimal amount, Currency from, Currency to)
    {
        if (from == to) return amount;
        var inUSD = amount / GetRate(from);
        return inUSD * GetRate(to);
    }

    public string GetSymbol(Currency currency)
    {
        return currency switch
        {
            Currency.RUB => "₽",
            Currency.USD => "$",
            Currency.EUR => "€",
            _ => ""
        };
    }

    public string GetCode(Currency currency)
    {
        return currency switch
        {
            Currency.RUB => "RUB",
            Currency.USD => "USD",
            Currency.EUR => "EUR",
            _ => ""
        };
    }

    public string Format(decimal amount, Currency? currency = null)
    {
        currency ??= _currentCurrency;
        var symbol = GetSymbol(currency.Value);
        return $"{amount:N2} {symbol}";
    }

    public List<Currency> GetAvailableCurrencies()
    {
        return new List<Currency> { Currency.RUB, Currency.USD, Currency.EUR };
    }
    public decimal ConvertFromPurchaseCurrency(
        decimal amount,
        string purchaseCurrency,
        decimal purchaseRate,
        Currency targetCurrency)
    {
        decimal inRub = purchaseCurrency switch
        {
            "USD" => amount * purchaseRate,
            "EUR" => amount * purchaseRate,
            _ => amount 
        };

        return Convert(inRub, Currency.RUB, targetCurrency);
    }

}