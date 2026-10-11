using System.Data;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Application.Interfaces;

public interface IOperationalReferenceService
{
    Task PrepareBusinessCodeAsync(
        Business business,
        IDbConnection connection,
        IDbTransaction transaction);

    Task PrepareBranchCodeAsync(
        Branch branch,
        IDbConnection connection,
        IDbTransaction transaction);

    Task PrepareAssessmentAsync(
        Assessment assessment,
        bool isCreate,
        IDbConnection connection,
        IDbTransaction transaction);

    Task FinalizeAssessmentCaseNumberAsync(
        Assessment assessment,
        long assessmentId,
        IDbConnection connection,
        IDbTransaction transaction);
}
