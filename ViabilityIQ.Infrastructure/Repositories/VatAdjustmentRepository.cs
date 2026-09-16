using Dapper;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class VatAdjustmentRepository : IVatAdjustmentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public VatAdjustmentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<VatAdjustmentMonth>> GetActiveAsync(long assessmentId)
    {
        const string sql = """
            SELECT
                [Period],
                SUM([OutputVAT]) AS [OutputVat],
                SUM([InputVAT]) AS [InputVat],
                MAX([Remarks]) AS [Notes]
            FROM [dbo].[tblAssessmentVATTransactions]
            WHERE [AssessmentId] = @AssessmentId
              AND [Active] = 1
              AND [SourceType] IN (N'Adjustment', N'Manual')
            GROUP BY [Period]
            ORDER BY [Period];
            """;

        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<VatAdjustmentMonth>(
            sql,
            new { AssessmentId = assessmentId });
        return rows.ToList();
    }

    public async Task SaveAsync(SaveVatAdjustmentsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AssessmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.AssessmentId));
        }

        if (request.Months.Any(x => x.Period is < 1 or > 12))
        {
            throw new ArgumentException("VAT adjustment periods must be between 1 and 12.", nameof(request));
        }

        if (request.Months.Count != 12
            || request.Months.Select(x => x.Period).Distinct().Count() != 12)
        {
            throw new ArgumentException(
                "VAT adjustments must contain one entry for every period from 1 to 12.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.AuditJustification))
        {
            throw new ArgumentException(
                "An audit justification is required.",
                nameof(request));
        }

        const string deactivateSql = """
            UPDATE [dbo].[tblAssessmentVATTransactions]
            SET
                [Active] = 0,
                [ModifiedDate] = SYSUTCDATETIME(),
                [ModifiedBy] = @UserId
            WHERE [AssessmentId] = @AssessmentId
              AND [Active] = 1
              AND [SourceType] IN (N'Adjustment', N'Manual');
            """;

        const string insertSql = """
            INSERT INTO [dbo].[tblAssessmentVATTransactions]
            (
                [AssessmentId],
                [SourceId],
                [SourceType],
                [Period],
                [VATRate],
                [TaxableAmount],
                [OutputVAT],
                [InputVAT],
                [VATPayable],
                [Active],
                [Remarks],
                [CreatedDate],
                [CreatedBy],
                [ModifiedDate],
                [ModifiedBy]
            )
            VALUES
            (
                @AssessmentId,
                @Period,
                N'Adjustment',
                @Period,
                0,
                0,
                @OutputVat,
                @InputVat,
                @VatPayable,
                1,
                @Remarks,
                SYSUTCDATETIME(),
                @UserId,
                SYSUTCDATETIME(),
                @UserId
            );
            """;

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(
                deactivateSql,
                new { request.AssessmentId, request.UserId },
                transaction);

            var rows = request.Months
                .Where(x => x.OutputVat != 0m || x.InputVat != 0m)
                .Select(x => new
                {
                    request.AssessmentId,
                    x.Period,
                    x.OutputVat,
                    x.InputVat,
                    VatPayable = x.OutputVat - x.InputVat,
                    Remarks = BuildRemarks(request, x),
                    request.UserId
                })
                .ToList();

            if (rows.Count > 0)
            {
                await connection.ExecuteAsync(insertSql, rows, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static string BuildRemarks(
        SaveVatAdjustmentsRequest request,
        VatAdjustmentMonth month)
    {
        var reason = string.IsNullOrWhiteSpace(request.ReasonCode)
            ? "Manual adjustment"
            : request.ReasonCode.Trim();
        var note = string.IsNullOrWhiteSpace(month.Notes)
            ? string.Empty
            : $" | {month.Notes.Trim()}";
        return $"{reason}: {request.AuditJustification.Trim()}{note}";
    }
}
