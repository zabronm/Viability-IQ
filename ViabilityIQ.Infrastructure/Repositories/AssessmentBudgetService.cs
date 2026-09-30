using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;
using ViabilityIQ.Shared.FinancialModels;
using ViabilityIQ.Shared.SharedModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class AssessmentBudgetService : IAssessmentBudgetService
{
    private const int MonthCount = 12;

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantAuthorizationService _authorization;
    private readonly ICashflowProjectionService _projectionService;
    private readonly ISessionService _session;
    private readonly ILogger<AssessmentBudgetService> _logger;

    public AssessmentBudgetService(
        IDbConnectionFactory connectionFactory,
        ITenantAuthorizationService authorization,
        ICashflowProjectionService projectionService,
        ISessionService session,
        ILogger<AssessmentBudgetService> logger)
    {
        _connectionFactory = connectionFactory;
        _authorization = authorization;
        _projectionService = projectionService;
        _session = session;
        _logger = logger;
    }

    public async Task<AssessmentBudgetWorkspace?> GetAsync(
        long assessmentId,
        CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureCanAccessAssessmentAsync(
            assessmentId,
            TenantRecordAccess.Read,
            cancellationToken);

        using var connection = _connectionFactory.CreateConnection();
        var header = await connection.QuerySingleOrDefaultAsync<BudgetHeaderRow>(
            new CommandDefinition(
                """
                SELECT
                    budget.BudgetId,
                    budget.AssessmentId,
                    budget.BudgetName,
                    budget.Status,
                    budget.Remarks,
                    budget.ProjectionCalculatedAtUtc,
                    budget.ProjectionImportedAtUtc,
                    budget.OpeningBank,
                    budget.ModifiedDate,
                    budget.RowVersion,
                    assessment.AssessmentStartDate
                FROM tblAssessmentBudget budget
                INNER JOIN tblAssessments assessment
                    ON assessment.AssessmentId = budget.AssessmentId
                   AND assessment.TenantId = budget.TenantId
                WHERE budget.AssessmentId = @AssessmentId
                  AND budget.TenantId = @TenantId
                  AND budget.Active = 1;
                """,
                new
                {
                    AssessmentId = assessmentId,
                    TenantId = _session.TenantId
                },
                cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        var lines = (await connection.QueryAsync<BudgetLineRow>(
            new CommandDefinition(
                """
                SELECT *
                FROM tblAssessmentBudgetLine
                WHERE BudgetId = @BudgetId
                  AND AssessmentId = @AssessmentId
                  AND TenantId = @TenantId
                  AND Active = 1
                ORDER BY DisplayOrder, BudgetLineId;
                """,
                new
                {
                    header.BudgetId,
                    AssessmentId = assessmentId,
                    TenantId = _session.TenantId
                },
                cancellationToken: cancellationToken))).AsList();

        var canWrite = await CanWriteAssessmentAsync(assessmentId, cancellationToken);
        var access = await _authorization.GetAccessContextAsync(cancellationToken);
        return MapWorkspace(
            header,
            lines,
            canWrite && header.Status == AssessmentBudgetStatuses.Draft,
            access.CanWriteAllOperationalRecords);
    }

    public async Task<AssessmentBudgetWorkspace> CreateFromProjectionAsync(
        CreateAssessmentBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAssessmentId(request.AssessmentId);
        var budgetName = ValidateBudgetName(request.BudgetName);
        await _authorization.EnsureCanAccessAssessmentAsync(
            request.AssessmentId,
            TenantRecordAccess.Write,
            cancellationToken);

        var projection = await _projectionService.CalculateAsync(request.AssessmentId);
        var lines = BuildProjectionLines(projection);
        var now = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        Open(connection);
        using var transaction = connection.BeginTransaction();
        try
        {
            var existing = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    """
                    SELECT COUNT_BIG(1)
                    FROM tblAssessmentBudget
                    WHERE AssessmentId = @AssessmentId
                      AND TenantId = @TenantId
                      AND Active = 1;
                    """,
                    new
                    {
                        request.AssessmentId,
                        TenantId = _session.TenantId
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            if (existing > 0)
            {
                throw new InvalidOperationException(
                    "This assessment already has an active budget.");
            }

            var budgetId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    """
                    INSERT INTO tblAssessmentBudget
                    (
                        TenantId, AssessmentId, BudgetName, Status, Remarks,
                        OpeningBank, ProjectionCalculatedAtUtc, ProjectionImportedAtUtc,
                        Active, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy
                    )
                    OUTPUT INSERTED.BudgetId
                    VALUES
                    (
                        @TenantId, @AssessmentId, @BudgetName, @Status, @Remarks,
                        @OpeningBank, @ProjectionCalculatedAtUtc, @ProjectionImportedAtUtc,
                        1, @Now, @UserId, @Now, @UserId
                    );
                    """,
                    new
                    {
                        TenantId = _session.TenantId,
                        request.AssessmentId,
                        BudgetName = budgetName,
                        Status = AssessmentBudgetStatuses.Draft,
                        request.Remarks,
                        OpeningBank = projection.Summary.OpeningBank,
                        ProjectionCalculatedAtUtc = projection.CalculatedAtUtc,
                        ProjectionImportedAtUtc = now,
                        Now = now,
                        UserId = _session.UserId
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await InsertLinesAsync(
                connection,
                transaction,
                budgetId,
                request.AssessmentId,
                lines,
                now,
                cancellationToken);
            await InsertStatusHistoryAsync(
                connection,
                transaction,
                budgetId,
                request.AssessmentId,
                null,
                AssessmentBudgetStatuses.Draft,
                "Budget created from the assessment projection.",
                now,
                cancellationToken);

            transaction.Commit();
            _logger.LogInformation(
                "Created budget {BudgetId} from projection for assessment {AssessmentId}.",
                budgetId,
                request.AssessmentId);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return await GetRequiredAsync(request.AssessmentId, cancellationToken);
    }

    public async Task<AssessmentBudgetWorkspace> SaveAsync(
        SaveAssessmentBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAssessmentId(request.AssessmentId);
        ValidateBudgetId(request.BudgetId);
        ValidateRowVersion(request.RowVersion);
        var budgetName = ValidateBudgetName(request.BudgetName);
        await _authorization.EnsureCanAccessAssessmentAsync(
            request.AssessmentId,
            TenantRecordAccess.Write,
            cancellationToken);

        using var connection = _connectionFactory.CreateConnection();
        Open(connection);
        using var transaction = connection.BeginTransaction();
        try
        {
            var header = await GetHeaderForUpdateAsync(
                connection,
                transaction,
                request.AssessmentId,
                request.BudgetId,
                cancellationToken);
            EnsureDraft(header);

            var updated = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE tblAssessmentBudget
                    SET BudgetName = @BudgetName,
                        Remarks = @Remarks,
                        ModifiedDate = @Now,
                        ModifiedBy = @UserId
                    WHERE BudgetId = @BudgetId
                      AND AssessmentId = @AssessmentId
                      AND TenantId = @TenantId
                      AND Active = 1
                      AND Status = @Draft
                      AND RowVersion = @RowVersion;
                    """,
                    new
                    {
                        request.BudgetId,
                        request.AssessmentId,
                        TenantId = _session.TenantId,
                        BudgetName = budgetName,
                        request.Remarks,
                        Now = DateTime.UtcNow,
                        UserId = _session.UserId,
                        Draft = AssessmentBudgetStatuses.Draft,
                        request.RowVersion
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            EnsureConcurrentUpdate(updated);

            var lines = await LoadLinesAsync(
                connection,
                transaction,
                request.BudgetId,
                request.AssessmentId,
                cancellationToken);
            ApplyEditableValues(lines, request.Lines);
            RecalculateBudget(lines, header.OpeningBank);
            await UpdateBudgetValuesAsync(
                connection,
                transaction,
                lines,
                cancellationToken);

            transaction.Commit();
            _logger.LogInformation(
                "Saved budget {BudgetId} for assessment {AssessmentId}.",
                request.BudgetId,
                request.AssessmentId);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return await GetRequiredAsync(request.AssessmentId, cancellationToken);
    }

    public async Task<AssessmentBudgetWorkspace> RefreshProjectionAsync(
        long assessmentId,
        long budgetId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        ValidateAssessmentId(assessmentId);
        ValidateBudgetId(budgetId);
        ValidateRowVersion(rowVersion);
        await _authorization.EnsureCanAccessAssessmentAsync(
            assessmentId,
            TenantRecordAccess.Write,
            cancellationToken);

        var projection = await _projectionService.CalculateAsync(assessmentId);
        var projectionLines = BuildProjectionLines(projection)
            .ToDictionary(line => line.LineCode, StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        Open(connection);
        using var transaction = connection.BeginTransaction();
        try
        {
            var header = await GetHeaderForUpdateAsync(
                connection,
                transaction,
                assessmentId,
                budgetId,
                cancellationToken);
            EnsureDraft(header);

            var updated = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE tblAssessmentBudget
                    SET OpeningBank = @OpeningBank,
                        ProjectionCalculatedAtUtc = @ProjectionCalculatedAtUtc,
                        ProjectionImportedAtUtc = @Now,
                        ModifiedDate = @Now,
                        ModifiedBy = @UserId
                    WHERE BudgetId = @BudgetId
                      AND AssessmentId = @AssessmentId
                      AND TenantId = @TenantId
                      AND Active = 1
                      AND Status = @Draft
                      AND RowVersion = @RowVersion;
                    """,
                    new
                    {
                        budgetId,
                        assessmentId,
                        TenantId = _session.TenantId,
                        OpeningBank = projection.Summary.OpeningBank,
                        ProjectionCalculatedAtUtc = projection.CalculatedAtUtc,
                        Now = now,
                        UserId = _session.UserId,
                        Draft = AssessmentBudgetStatuses.Draft,
                        rowVersion
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            EnsureConcurrentUpdate(updated);

            var lines = await LoadLinesAsync(
                connection,
                transaction,
                budgetId,
                assessmentId,
                cancellationToken);
            foreach (var line in lines)
            {
                if (projectionLines.TryGetValue(line.LineCode, out var refreshed))
                {
                    line.ProjectedMonths = refreshed.ProjectedMonths;
                }
            }

            RecalculateBudget(lines, projection.Summary.OpeningBank);
            await UpdateAllLineValuesAsync(
                connection,
                transaction,
                lines,
                cancellationToken);
            transaction.Commit();
            _logger.LogInformation(
                "Refreshed projection comparison for budget {BudgetId}, assessment {AssessmentId}.",
                budgetId,
                assessmentId);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return await GetRequiredAsync(assessmentId, cancellationToken);
    }

    public Task<AssessmentBudgetWorkspace> SubmitAsync(
        long assessmentId,
        long budgetId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(
            assessmentId,
            budgetId,
            rowVersion,
            AssessmentBudgetStatuses.Draft,
            AssessmentBudgetStatuses.Submitted,
            requireApprover: false,
            "Budget submitted for approval.",
            cancellationToken);

    public Task<AssessmentBudgetWorkspace> ApproveAsync(
        long assessmentId,
        long budgetId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(
            assessmentId,
            budgetId,
            rowVersion,
            AssessmentBudgetStatuses.Submitted,
            AssessmentBudgetStatuses.Approved,
            requireApprover: true,
            "Budget approved.",
            cancellationToken);

    public Task<AssessmentBudgetWorkspace> ReopenAsync(
        long assessmentId,
        long budgetId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(
            assessmentId,
            budgetId,
            rowVersion,
            AssessmentBudgetStatuses.Approved,
            AssessmentBudgetStatuses.Draft,
            requireApprover: true,
            "Approved budget reopened as a draft.",
            cancellationToken);

    public IReadOnlyList<AssessmentBudgetAlert> BuildAlerts(
        AssessmentBudgetWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var alerts = new List<AssessmentBudgetAlert>();

        if (workspace.BudgetClosingBank < 0m)
        {
            alerts.Add(new()
            {
                Severity = "Critical",
                Title = "Funding shortfall",
                Message = $"The budget closes with a cash deficit of {Math.Abs(workspace.BudgetClosingBank):N0}."
            });
        }

        var lossMonths = workspace.FindLine("PBT")?.BudgetMonths.Count(value => value < 0m) ?? 0;
        if (lossMonths > 0)
        {
            alerts.Add(new()
            {
                Severity = lossMonths >= 6 ? "Critical" : "Warning",
                Title = "Loss-making months",
                Message = $"The budget records a loss before tax in {lossMonths} of 12 months."
            });
        }

        if (workspace.ProjectedProfitBeforeTax != 0m)
        {
            var variance = workspace.BudgetProfitBeforeTax - workspace.ProjectedProfitBeforeTax;
            var variancePercentage =
                variance / Math.Abs(workspace.ProjectedProfitBeforeTax) * 100m;
            if (variancePercentage <= -10m)
            {
                alerts.Add(new()
                {
                    Severity = "Warning",
                    Title = "Profit below assessment projection",
                    Message = $"Budgeted profit before tax is {Math.Abs(variancePercentage):N1}% below the assessment projection."
                });
            }
        }

        if (alerts.Count == 0)
        {
            alerts.Add(new()
            {
                Severity = "Information",
                Title = "Budget checks passed",
                Message = "No material cash or profitability exceptions were identified."
            });
        }

        return alerts;
    }

    private async Task<AssessmentBudgetWorkspace> ChangeStatusAsync(
        long assessmentId,
        long budgetId,
        byte[] rowVersion,
        string expectedStatus,
        string newStatus,
        bool requireApprover,
        string historyRemarks,
        CancellationToken cancellationToken)
    {
        ValidateAssessmentId(assessmentId);
        ValidateBudgetId(budgetId);
        ValidateRowVersion(rowVersion);
        await _authorization.EnsureCanAccessAssessmentAsync(
            assessmentId,
            TenantRecordAccess.Write,
            cancellationToken);
        var access = await _authorization.GetAccessContextAsync(cancellationToken);
        if (requireApprover && !access.CanWriteAllOperationalRecords)
        {
            throw new UnauthorizedAccessException(
                "Only a Tenant Owner or Tenant Administrator may approve or reopen a budget.");
        }

        using var connection = _connectionFactory.CreateConnection();
        Open(connection);
        using var transaction = connection.BeginTransaction();
        try
        {
            var now = DateTime.UtcNow;
            var updated = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE tblAssessmentBudget
                    SET Status = @NewStatus,
                        SubmittedDate = CASE WHEN @NewStatus = @Submitted THEN @Now ELSE SubmittedDate END,
                        SubmittedBy = CASE WHEN @NewStatus = @Submitted THEN @UserId ELSE SubmittedBy END,
                        ApprovedDate = CASE WHEN @NewStatus = @Approved THEN @Now ELSE NULL END,
                        ApprovedBy = CASE WHEN @NewStatus = @Approved THEN @UserId ELSE NULL END,
                        ModifiedDate = @Now,
                        ModifiedBy = @UserId
                    WHERE BudgetId = @BudgetId
                      AND AssessmentId = @AssessmentId
                      AND TenantId = @TenantId
                      AND Active = 1
                      AND Status = @ExpectedStatus
                      AND RowVersion = @RowVersion;
                    """,
                    new
                    {
                        budgetId,
                        assessmentId,
                        TenantId = _session.TenantId,
                        ExpectedStatus = expectedStatus,
                        NewStatus = newStatus,
                        Submitted = AssessmentBudgetStatuses.Submitted,
                        Approved = AssessmentBudgetStatuses.Approved,
                        Now = now,
                        UserId = _session.UserId,
                        rowVersion
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            EnsureConcurrentUpdate(updated);
            await InsertStatusHistoryAsync(
                connection,
                transaction,
                budgetId,
                assessmentId,
                expectedStatus,
                newStatus,
                historyRemarks,
                now,
                cancellationToken);
            transaction.Commit();
            _logger.LogInformation(
                "Changed budget {BudgetId} status from {OldStatus} to {NewStatus}.",
                budgetId,
                expectedStatus,
                newStatus);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return await GetRequiredAsync(assessmentId, cancellationToken);
    }

    private async Task<AssessmentBudgetWorkspace> GetRequiredAsync(
        long assessmentId,
        CancellationToken cancellationToken) =>
        await GetAsync(assessmentId, cancellationToken)
            ?? throw new InvalidOperationException("The assessment budget could not be loaded.");

    private async Task<bool> CanWriteAssessmentAsync(
        long assessmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _authorization.EnsureCanAccessAssessmentAsync(
                assessmentId,
                TenantRecordAccess.Write,
                cancellationToken);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private async Task<BudgetHeaderRow> GetHeaderForUpdateAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        long assessmentId,
        long budgetId,
        CancellationToken cancellationToken) =>
        await connection.QuerySingleOrDefaultAsync<BudgetHeaderRow>(
            new CommandDefinition(
                """
                SELECT *
                FROM tblAssessmentBudget WITH (UPDLOCK, ROWLOCK)
                WHERE BudgetId = @BudgetId
                  AND AssessmentId = @AssessmentId
                  AND TenantId = @TenantId
                  AND Active = 1;
                """,
                new
                {
                    budgetId,
                    assessmentId,
                    TenantId = _session.TenantId
                },
                transaction,
                cancellationToken: cancellationToken))
        ?? throw new InvalidOperationException("The assessment budget was not found.");

    private async Task<List<BudgetLineRow>> LoadLinesAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        long budgetId,
        long assessmentId,
        CancellationToken cancellationToken) =>
        (await connection.QueryAsync<BudgetLineRow>(
            new CommandDefinition(
                """
                SELECT *
                FROM tblAssessmentBudgetLine WITH (UPDLOCK)
                WHERE BudgetId = @BudgetId
                  AND AssessmentId = @AssessmentId
                  AND TenantId = @TenantId
                  AND Active = 1
                ORDER BY DisplayOrder, BudgetLineId;
                """,
                new
                {
                    budgetId,
                    assessmentId,
                    TenantId = _session.TenantId
                },
                transaction,
                cancellationToken: cancellationToken))).AsList();

    private async Task InsertLinesAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        long budgetId,
        long assessmentId,
        IReadOnlyList<BudgetLineRow> lines,
        DateTime now,
        CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            line.BudgetId = budgetId;
            line.AssessmentId = assessmentId;
            line.TenantId = _session.TenantId;
            await connection.ExecuteAsync(
                new CommandDefinition(
                    $"{InsertLineSql};",
                    BuildLineParameters(line, now),
                    transaction,
                    cancellationToken: cancellationToken));
        }
    }

    private async Task UpdateBudgetValuesAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        IReadOnlyList<BudgetLineRow> lines,
        CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    $"""
                    UPDATE tblAssessmentBudgetLine
                    SET {BudgetMonthAssignments},
                        ModifiedDate = @Now,
                        ModifiedBy = @UserId
                    WHERE BudgetLineId = @BudgetLineId
                      AND BudgetId = @BudgetId
                      AND AssessmentId = @AssessmentId
                      AND TenantId = @TenantId
                      AND Active = 1;
                    """,
                    BuildLineParameters(line, DateTime.UtcNow),
                    transaction,
                    cancellationToken: cancellationToken));
        }
    }

    private async Task UpdateAllLineValuesAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        IReadOnlyList<BudgetLineRow> lines,
        CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    $"""
                    UPDATE tblAssessmentBudgetLine
                    SET {ProjectionMonthAssignments},
                        {BudgetMonthAssignments},
                        ModifiedDate = @Now,
                        ModifiedBy = @UserId
                    WHERE BudgetLineId = @BudgetLineId
                      AND BudgetId = @BudgetId
                      AND AssessmentId = @AssessmentId
                      AND TenantId = @TenantId
                      AND Active = 1;
                    """,
                    BuildLineParameters(line, DateTime.UtcNow),
                    transaction,
                    cancellationToken: cancellationToken));
        }
    }

    private async Task InsertStatusHistoryAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        long budgetId,
        long assessmentId,
        string? previousStatus,
        string newStatus,
        string remarks,
        DateTime now,
        CancellationToken cancellationToken) =>
        await connection.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO tblAssessmentBudgetStatusHistory
                (
                    TenantId, AssessmentId, BudgetId, PreviousStatus, NewStatus,
                    Remarks, ChangedDate, ChangedBy
                )
                VALUES
                (
                    @TenantId, @AssessmentId, @BudgetId, @PreviousStatus, @NewStatus,
                    @Remarks, @ChangedDate, @ChangedBy
                );
                """,
                new
                {
                    TenantId = _session.TenantId,
                    assessmentId,
                    budgetId,
                    previousStatus,
                    newStatus,
                    remarks,
                    ChangedDate = now,
                    ChangedBy = _session.UserId
                },
                transaction,
                cancellationToken: cancellationToken));

    private static void ApplyEditableValues(
        IReadOnlyList<BudgetLineRow> lines,
        IReadOnlyList<SaveAssessmentBudgetLineRequest> requests)
    {
        var editable = lines
            .Where(line => !line.IsCalculated)
            .ToDictionary(line => line.BudgetLineId);
        foreach (var request in requests)
        {
            if (!editable.TryGetValue(request.BudgetLineId, out var line))
            {
                continue;
            }

            if (request.BudgetMonths.Length != MonthCount)
            {
                throw new InvalidOperationException(
                    $"Budget line {line.LineName} must contain exactly 12 monthly values.");
            }

            if (request.BudgetMonths.Any(value => value < 0m))
            {
                throw new InvalidOperationException(
                    $"Budget line {line.LineName} cannot contain negative monthly values.");
            }

            line.BudgetMonths = request.BudgetMonths.ToArray();
        }
    }

    private static void RecalculateBudget(
        IReadOnlyList<BudgetLineRow> lines,
        decimal openingBank)
    {
        var byCode = lines.ToDictionary(
            line => line.LineCode,
            StringComparer.OrdinalIgnoreCase);
        for (var month = 0; month < MonthCount; month++)
        {
            Set(byCode, "GROSS_PROFIT", month,
                Value(byCode, "REVENUE", month) - Value(byCode, "COGS", month));
            Set(byCode, "EBITDA", month,
                Value(byCode, "GROSS_PROFIT", month)
                + Value(byCode, "OTHER_INCOME", month)
                - Value(byCode, "OPERATING_EXPENSES", month));
            Set(byCode, "EBIT", month,
                Value(byCode, "EBITDA", month) - Value(byCode, "DEPRECIATION", month));
            Set(byCode, "PBT", month,
                Value(byCode, "EBIT", month) - Value(byCode, "INTEREST", month));
            Set(byCode, "TOTAL_INFLOWS", month,
                Value(byCode, "SALES_RECEIPTS", month)
                + Value(byCode, "OTHER_RECEIPTS", month));
            Set(byCode, "TOTAL_OUTFLOWS", month,
                Value(byCode, "SUPPLIER_PAYMENTS", month)
                + Value(byCode, "CASH_OPERATING_EXPENSES", month)
                + Value(byCode, "VAT_PAYMENT", month)
                + Value(byCode, "LOAN_PAYMENTS", month)
                + Value(byCode, "ASSET_PURCHASES", month));
            Set(byCode, "NET_CASH", month,
                Value(byCode, "TOTAL_INFLOWS", month)
                - Value(byCode, "TOTAL_OUTFLOWS", month));
            var monthOpeningBank = month == 0
                ? openingBank
                : Value(byCode, "CLOSING_BANK", month - 1);
            Set(byCode, "CLOSING_BANK", month,
                monthOpeningBank + Value(byCode, "NET_CASH", month));
        }
    }

    private static IReadOnlyList<BudgetLineRow> BuildProjectionLines(
        CashflowProjectionResult projection)
    {
        var months = Enumerable.Range(1, MonthCount)
            .Select(monthNumber =>
                projection.Months.FirstOrDefault(month => month.MonthNumber == monthNumber)
                ?? new CashflowProjectionMonth { MonthNumber = monthNumber })
            .ToArray();
        var lines = new List<BudgetLineRow>
        {
            Line(AssessmentBudgetSections.ProfitAndLoss, "Profit & loss", "REVENUE", "Revenue", 10, false, months.Select(x => x.Revenue)),
            Line(AssessmentBudgetSections.ProfitAndLoss, "Profit & loss", "OTHER_INCOME", "Other income", 20, false, months.Select(x => x.AdditionalIncome)),
            Line(AssessmentBudgetSections.ProfitAndLoss, "Profit & loss", "COGS", "Cost of goods sold", 30, false, months.Select(x => x.CostOfGoodsSold)),
            Line(AssessmentBudgetSections.ProfitAndLoss, "Profit & loss", "GROSS_PROFIT", "Gross profit", 40, true, months.Select(x => x.GrossProfit)),
            Line(AssessmentBudgetSections.ProfitAndLoss, "Profit & loss", "OPERATING_EXPENSES", "Operating expenses", 50, false, months.Select(x => x.OperatingExpenses)),
            Line(AssessmentBudgetSections.ProfitAndLoss, "Profit & loss", "EBITDA", "EBITDA", 60, true, months.Select(x => x.EBITDA)),
            Line(AssessmentBudgetSections.ProfitAndLoss, "Profit & loss", "DEPRECIATION", "Depreciation", 70, false, months.Select(x => x.Depreciation)),
            Line(AssessmentBudgetSections.ProfitAndLoss, "Profit & loss", "INTEREST", "Interest expense", 80, false, months.Select(x => x.InterestExpense)),
            Line(AssessmentBudgetSections.ProfitAndLoss, "Profit & loss", "EBIT", "Operating profit (EBIT)", 90, true, months.Select(x => x.EBIT)),
            Line(AssessmentBudgetSections.ProfitAndLoss, "Profit & loss", "PBT", "Profit before tax", 100, true, months.Select(x => x.ProfitBeforeTax)),
            Line(AssessmentBudgetSections.CashFlow, "Cash flow", "SALES_RECEIPTS", "Sales receipts", 110, false, months.Select(x => x.SalesReceipts)),
            Line(AssessmentBudgetSections.CashFlow, "Cash flow", "OTHER_RECEIPTS", "Other receipts", 120, false, months.Select(x => x.SundryIncomeReceipts + x.GrantsDonationsReceipts + x.AssetDisposalReceipts + x.OtherIncomeReceipts + x.VatRefund)),
            Line(AssessmentBudgetSections.CashFlow, "Cash flow", "TOTAL_INFLOWS", "Total cash inflows", 130, true, months.Select(x => x.TotalInflows)),
            Line(AssessmentBudgetSections.CashFlow, "Cash flow", "SUPPLIER_PAYMENTS", "Supplier payments", 140, false, months.Select(x => x.SupplierPayments)),
            Line(AssessmentBudgetSections.CashFlow, "Cash flow", "CASH_OPERATING_EXPENSES", "Cash operating expenses", 150, false, months.Select(x => x.CashOperatingExpenses)),
            Line(AssessmentBudgetSections.CashFlow, "Cash flow", "VAT_PAYMENT", "VAT payments", 160, false, months.Select(x => x.VatPayment)),
            Line(AssessmentBudgetSections.CashFlow, "Cash flow", "LOAN_PAYMENTS", "Loan payments", 170, false, months.Select(x => x.LoanCashPayment + x.OverdraftInterest)),
            Line(AssessmentBudgetSections.CashFlow, "Cash flow", "ASSET_PURCHASES", "Asset purchases", 180, false, months.Select(x => x.AssetPurchases)),
            Line(AssessmentBudgetSections.CashFlow, "Cash flow", "TOTAL_OUTFLOWS", "Total cash outflows", 190, true, months.Select(x => x.TotalOutflows)),
            Line(AssessmentBudgetSections.CashFlow, "Cash flow", "NET_CASH", "Net cash movement", 200, true, months.Select(x => x.NetCashflow)),
            Line(AssessmentBudgetSections.CashFlow, "Cash flow", "CLOSING_BANK", "Closing cash balance", 210, true, months.Select(x => x.ClosingBank))
        };
        RecalculateBudget(lines, projection.Summary.OpeningBank);
        return lines;
    }

    private static BudgetLineRow Line(
        string sectionCode,
        string sectionName,
        string lineCode,
        string lineName,
        int displayOrder,
        bool isCalculated,
        IEnumerable<decimal> projectionValues)
    {
        var projected = projectionValues.Take(MonthCount).ToArray();
        if (projected.Length != MonthCount)
        {
            Array.Resize(ref projected, MonthCount);
        }

        return new BudgetLineRow
        {
            SectionCode = sectionCode,
            SectionName = sectionName,
            LineCode = lineCode,
            LineName = lineName,
            DisplayOrder = displayOrder,
            IsCalculated = isCalculated,
            ProjectedMonths = projected,
            BudgetMonths = projected.ToArray()
        };
    }

    private static AssessmentBudgetWorkspace MapWorkspace(
        BudgetHeaderRow header,
        IReadOnlyList<BudgetLineRow> lines,
        bool canEdit,
        bool canApprove) =>
        new()
        {
            BudgetId = header.BudgetId,
            AssessmentId = header.AssessmentId,
            BudgetName = header.BudgetName,
            Status = header.Status,
            Remarks = header.Remarks,
            AssessmentStartDate = header.AssessmentStartDate,
            ProjectionCalculatedAtUtc = header.ProjectionCalculatedAtUtc,
            ProjectionImportedAtUtc = header.ProjectionImportedAtUtc,
            OpeningBank = header.OpeningBank,
            ModifiedDate = header.ModifiedDate,
            RowVersion = header.RowVersion,
            CanEdit = canEdit,
            CanApprove = canApprove,
            Lines = lines.Select(line => new AssessmentBudgetLine
            {
                BudgetLineId = line.BudgetLineId,
                SectionCode = line.SectionCode,
                SectionName = line.SectionName,
                LineCode = line.LineCode,
                LineName = line.LineName,
                DisplayOrder = line.DisplayOrder,
                IsCalculated = line.IsCalculated,
                ProjectedMonths = line.ProjectedMonths.ToArray(),
                BudgetMonths = line.BudgetMonths.ToArray()
            }).ToList()
        };

    private DynamicParameters BuildLineParameters(
        BudgetLineRow line,
        DateTime now)
    {
        var parameters = new DynamicParameters(new
        {
            line.BudgetLineId,
            line.TenantId,
            line.AssessmentId,
            line.BudgetId,
            line.SectionCode,
            line.SectionName,
            line.LineCode,
            line.LineName,
            line.DisplayOrder,
            line.IsCalculated,
            Now = now,
            UserId = _session.UserId
        });
        for (var month = 0; month < MonthCount; month++)
        {
            parameters.Add($"ProjectedMonth{month + 1}", line.ProjectedMonths[month]);
            parameters.Add($"BudgetMonth{month + 1}", line.BudgetMonths[month]);
        }

        return parameters;
    }

    private static decimal Value(
        IReadOnlyDictionary<string, BudgetLineRow> lines,
        string code,
        int month) =>
        lines.TryGetValue(code, out var line) ? line.BudgetMonths[month] : 0m;

    private static void Set(
        IReadOnlyDictionary<string, BudgetLineRow> lines,
        string code,
        int month,
        decimal value)
    {
        if (lines.TryGetValue(code, out var line))
        {
            line.BudgetMonths[month] = value;
        }
    }

    private static void EnsureDraft(BudgetHeaderRow header)
    {
        if (header.Status != AssessmentBudgetStatuses.Draft)
        {
            throw new InvalidOperationException(
                "Only a draft budget can be changed.");
        }
    }

    private static void EnsureConcurrentUpdate(int updated)
    {
        if (updated == 0)
        {
            throw new InvalidOperationException(
                "The budget changed after it was opened. Reload it before trying again.");
        }
    }

    private static void ValidateAssessmentId(long assessmentId)
    {
        if (assessmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(assessmentId),
                "A valid assessment is required.");
        }
    }

    private static void ValidateBudgetId(long budgetId)
    {
        if (budgetId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(budgetId),
                "A valid budget is required.");
        }
    }

    private static string ValidateBudgetName(string budgetName)
    {
        var value = budgetName?.Trim() ?? string.Empty;
        if (value.Length is < 3 or > 150)
        {
            throw new InvalidOperationException(
                "Budget name must contain between 3 and 150 characters.");
        }

        return value;
    }

    private static void ValidateRowVersion(byte[] rowVersion)
    {
        if (rowVersion is null || rowVersion.Length == 0)
        {
            throw new InvalidOperationException(
                "The budget version is missing. Reload it before trying again.");
        }
    }

    private static void Open(IDbConnection connection)
    {
        if (connection.State == ConnectionState.Closed)
        {
            connection.Open();
        }
    }

    private const string ProjectionMonthAssignments = """
        ProjectedMonth1 = @ProjectedMonth1,
        ProjectedMonth2 = @ProjectedMonth2,
        ProjectedMonth3 = @ProjectedMonth3,
        ProjectedMonth4 = @ProjectedMonth4,
        ProjectedMonth5 = @ProjectedMonth5,
        ProjectedMonth6 = @ProjectedMonth6,
        ProjectedMonth7 = @ProjectedMonth7,
        ProjectedMonth8 = @ProjectedMonth8,
        ProjectedMonth9 = @ProjectedMonth9,
        ProjectedMonth10 = @ProjectedMonth10,
        ProjectedMonth11 = @ProjectedMonth11,
        ProjectedMonth12 = @ProjectedMonth12
        """;

    private const string BudgetMonthAssignments = """
        BudgetMonth1 = @BudgetMonth1,
        BudgetMonth2 = @BudgetMonth2,
        BudgetMonth3 = @BudgetMonth3,
        BudgetMonth4 = @BudgetMonth4,
        BudgetMonth5 = @BudgetMonth5,
        BudgetMonth6 = @BudgetMonth6,
        BudgetMonth7 = @BudgetMonth7,
        BudgetMonth8 = @BudgetMonth8,
        BudgetMonth9 = @BudgetMonth9,
        BudgetMonth10 = @BudgetMonth10,
        BudgetMonth11 = @BudgetMonth11,
        BudgetMonth12 = @BudgetMonth12
        """;

    private const string InsertLineSql = $"""
        INSERT INTO tblAssessmentBudgetLine
        (
            TenantId, AssessmentId, BudgetId, SectionCode, SectionName,
            LineCode, LineName, DisplayOrder, IsCalculated,
            ProjectedMonth1, ProjectedMonth2, ProjectedMonth3, ProjectedMonth4,
            ProjectedMonth5, ProjectedMonth6, ProjectedMonth7, ProjectedMonth8,
            ProjectedMonth9, ProjectedMonth10, ProjectedMonth11, ProjectedMonth12,
            BudgetMonth1, BudgetMonth2, BudgetMonth3, BudgetMonth4,
            BudgetMonth5, BudgetMonth6, BudgetMonth7, BudgetMonth8,
            BudgetMonth9, BudgetMonth10, BudgetMonth11, BudgetMonth12,
            Active, CreatedDate, CreatedBy, ModifiedDate, ModifiedBy
        )
        VALUES
        (
            @TenantId, @AssessmentId, @BudgetId, @SectionCode, @SectionName,
            @LineCode, @LineName, @DisplayOrder, @IsCalculated,
            @ProjectedMonth1, @ProjectedMonth2, @ProjectedMonth3, @ProjectedMonth4,
            @ProjectedMonth5, @ProjectedMonth6, @ProjectedMonth7, @ProjectedMonth8,
            @ProjectedMonth9, @ProjectedMonth10, @ProjectedMonth11, @ProjectedMonth12,
            @BudgetMonth1, @BudgetMonth2, @BudgetMonth3, @BudgetMonth4,
            @BudgetMonth5, @BudgetMonth6, @BudgetMonth7, @BudgetMonth8,
            @BudgetMonth9, @BudgetMonth10, @BudgetMonth11, @BudgetMonth12,
            1, @Now, @UserId, @Now, @UserId
        )
        """;

    private sealed class BudgetHeaderRow
    {
        public long BudgetId { get; init; }
        public long AssessmentId { get; init; }
        public string BudgetName { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string? Remarks { get; init; }
        public DateTime? AssessmentStartDate { get; init; }
        public DateTime ProjectionCalculatedAtUtc { get; init; }
        public DateTime ProjectionImportedAtUtc { get; init; }
        public decimal OpeningBank { get; init; }
        public DateTime ModifiedDate { get; init; }
        public byte[] RowVersion { get; init; } = [];
    }

    private sealed class BudgetLineRow
    {
        public long BudgetLineId { get; set; }
        public long TenantId { get; set; }
        public long AssessmentId { get; set; }
        public long BudgetId { get; set; }
        public string SectionCode { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string LineCode { get; set; } = string.Empty;
        public string LineName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsCalculated { get; set; }
        public decimal[] ProjectedMonths
        {
            get => [
                ProjectedMonth1, ProjectedMonth2, ProjectedMonth3, ProjectedMonth4,
                ProjectedMonth5, ProjectedMonth6, ProjectedMonth7, ProjectedMonth8,
                ProjectedMonth9, ProjectedMonth10, ProjectedMonth11, ProjectedMonth12
            ];
            set
            {
                ProjectedMonth1 = value[0]; ProjectedMonth2 = value[1];
                ProjectedMonth3 = value[2]; ProjectedMonth4 = value[3];
                ProjectedMonth5 = value[4]; ProjectedMonth6 = value[5];
                ProjectedMonth7 = value[6]; ProjectedMonth8 = value[7];
                ProjectedMonth9 = value[8]; ProjectedMonth10 = value[9];
                ProjectedMonth11 = value[10]; ProjectedMonth12 = value[11];
            }
        }
        public decimal[] BudgetMonths
        {
            get => [
                BudgetMonth1, BudgetMonth2, BudgetMonth3, BudgetMonth4,
                BudgetMonth5, BudgetMonth6, BudgetMonth7, BudgetMonth8,
                BudgetMonth9, BudgetMonth10, BudgetMonth11, BudgetMonth12
            ];
            set
            {
                BudgetMonth1 = value[0]; BudgetMonth2 = value[1];
                BudgetMonth3 = value[2]; BudgetMonth4 = value[3];
                BudgetMonth5 = value[4]; BudgetMonth6 = value[5];
                BudgetMonth7 = value[6]; BudgetMonth8 = value[7];
                BudgetMonth9 = value[8]; BudgetMonth10 = value[9];
                BudgetMonth11 = value[10]; BudgetMonth12 = value[11];
            }
        }
        public long ModifiedBy { get; set; }
        public decimal ProjectedMonth1 { get; set; }
        public decimal ProjectedMonth2 { get; set; }
        public decimal ProjectedMonth3 { get; set; }
        public decimal ProjectedMonth4 { get; set; }
        public decimal ProjectedMonth5 { get; set; }
        public decimal ProjectedMonth6 { get; set; }
        public decimal ProjectedMonth7 { get; set; }
        public decimal ProjectedMonth8 { get; set; }
        public decimal ProjectedMonth9 { get; set; }
        public decimal ProjectedMonth10 { get; set; }
        public decimal ProjectedMonth11 { get; set; }
        public decimal ProjectedMonth12 { get; set; }
        public decimal BudgetMonth1 { get; set; }
        public decimal BudgetMonth2 { get; set; }
        public decimal BudgetMonth3 { get; set; }
        public decimal BudgetMonth4 { get; set; }
        public decimal BudgetMonth5 { get; set; }
        public decimal BudgetMonth6 { get; set; }
        public decimal BudgetMonth7 { get; set; }
        public decimal BudgetMonth8 { get; set; }
        public decimal BudgetMonth9 { get; set; }
        public decimal BudgetMonth10 { get; set; }
        public decimal BudgetMonth11 { get; set; }
        public decimal BudgetMonth12 { get; set; }
    }
}
