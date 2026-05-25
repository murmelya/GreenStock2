using System.Threading.Tasks;
using GreenStock.Services;

namespace GreenStock.Interfaces;

public interface ICounterpartyService
{
    Task<CounterpartyCheckResult> CheckCounterpartyAsync(string inn);
    CounterpartyCheckResult CheckCounterparty(string inn);
}