namespace GreenStock.Interfaces;

public interface IExpiryService
{
    void ProcessBatches(string connStr);
}