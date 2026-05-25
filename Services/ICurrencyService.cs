using GreenStock.Services;

public interface ICurrencyService2
{
    Currency CurrentCurrency { get; set; }
    void SetCurrency(Currency currency);
    string GetSymbol();
    string Format(decimal amount);  
}