using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.Interfaces;

public interface IAccountsProjectionService
{
    Task<AccountsProjectionResult> CalculateAsync(long assessmentId);
}
