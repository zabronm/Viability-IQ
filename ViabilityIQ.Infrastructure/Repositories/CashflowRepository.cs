using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ViabilityIQ.Infrastructure.Repositories
{
    public class CashflowRepository : ICashflowRepository
    {
        #region Private Fields

        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<CashflowRepository> _logger;

        #endregion

        #region Constructor

        public CashflowRepository(
            IDbConnectionFactory dbConnectionFactory,
            ILogger<CashflowRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Methods


        /// Saves monthly cashflow records to database

        public async Task SaveMonthlyCashflowAsync(List<AssessmentCashflow> cashflows)
        {
            if (cashflows == null || cashflows.Count == 0)
            {
                _logger.LogWarning("No cashflow records to save");
                return;
            }

            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();

                foreach (var cashflow in cashflows)
                {
                    // Clear navigation property to prevent Dapper serialization error
                    cashflow.Assessment = null;

                    // Check if record exists
                    var existingQuery = @"
                        SELECT TOP 1 AssessmentCashflowId 
                        FROM tblAssessmentCashflow 
                        WHERE AssessmentId = @AssessmentId 
                        AND MonthNumber = @MonthNumber
                    ";

                    var existingId = await connection.QuerySingleOrDefaultAsync<long?>(
                        existingQuery,
                        new { cashflow.AssessmentId, cashflow.MonthNumber }
                    );

                    if (existingId.HasValue)
                    {
                        // Update existing record
                        cashflow.AssessmentCashflowId = existingId.Value;
                        await connection.UpdateAsync(cashflow);
                        _logger.LogDebug("Updated cashflow for assessment {AssessmentId}, month {Month}", cashflow.AssessmentId, cashflow.MonthNumber);
                    }
                    else
                    {
                        // Insert new record
                        await connection.InsertAsync(cashflow);
                        _logger.LogDebug("Inserted new cashflow for assessment {AssessmentId}, month {Month}", cashflow.AssessmentId, cashflow.MonthNumber);
                    }
                }

                _logger.LogInformation("Saved {Count} monthly cashflow records for assessment {AssessmentId}", cashflows.Count, cashflows.First().AssessmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving monthly cashflow records");
                throw;
            }
        }


        /// Saves cashflow summary to database        
        public async Task SaveCashflowSummaryAsync(CashflowSummary summary)
        {
            if (summary == null)
            {
                _logger.LogWarning("No cashflow summary to save");
                return;
            }

            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                
                // Clear navigation property to prevent Dapper serialization error
                summary.Assessment = null;

                // Check if summary exists
                var existingQuery = @"
                    SELECT TOP 1 CashflowSummaryId  
                    FROM tblCashflowSummary 
                    WHERE AssessmentId = @AssessmentId
                ";

                var existingId = await connection.QuerySingleOrDefaultAsync<long?>(existingQuery, new { summary.AssessmentId });

                if (existingId.HasValue)
                {
                    // Update existing summary
                    summary.CashflowSummaryId = existingId.Value;
                    summary.UpdatedAt = DateTime.UtcNow;
                    await connection.UpdateAsync(summary);
                    _logger.LogDebug("Updated cashflow summary for assessment {AssessmentId}", summary.AssessmentId);
                }
                else
                {
                    // Insert new summary
                    await connection.InsertAsync(summary);
                    _logger.LogDebug("Inserted new cashflow summary for assessment {AssessmentId}", summary.AssessmentId);
                }

                _logger.LogInformation("Saved cashflow summary for assessment {AssessmentId}", summary.AssessmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving cashflow summary for assessment {AssessmentId}", summary.AssessmentId);
                throw;
            }
        }


        /// Gets all monthly cashflow records for an assessment
        public async Task<List<AssessmentCashflow>> GetMonthlyCashflowAsync(long assessmentId)
        {
            try
            {
                const string query = @"
                    SELECT 
                        AssessmentCashflowId,
                        AssessmentId,
                        MonthNumber,
                        Year,
                        SalesRevenue,
                        OtherIncome,
                        TotalIncome,
                        COGS,
                        SalaryExpense,
                        RentExpense,
                        UtilityExpense,
                        MarketingExpense,
                        LoanRepayment,
                        OtherExpense,
                        TotalExpense,
                        NetCashflow,
                        OpeningBalance,
                        ClosingBalance,
                        CumulativeCashflow,
                        HasNegativeCashflow,
                        IsCritical,
                        CreatedAt,
                        UpdatedAt,
                        IsActive,
                        Notes
                    FROM tblAssessmentCashflow
                    WHERE AssessmentId = @AssessmentId
                    AND IsActive = 1
                    ORDER BY MonthNumber ASC
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var cashflows = (await connection.QueryAsync<AssessmentCashflow>(query, new { AssessmentId = assessmentId })).ToList();

                _logger.LogDebug("Retrieved {Count} monthly cashflow records for assessment {AssessmentId}", cashflows.Count, assessmentId);

                return cashflows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly cashflow for assessment {AssessmentId}",
                    assessmentId);
                throw;
            }
        }


        /// Gets cashflow summary for an assessment

        public async Task<CashflowSummary> GetCashflowSummaryAsync(long assessmentId)
        {
            try
            {
                const string query = @"
                    SELECT 
                        CashflowSummaryId ,
                        AssessmentId,
                        TotalAnnualIncome,
                        TotalAnnualExpense,
                        TotalAnnualNetCashflow,
                        MinimumCashBalance,
                        MaximumCashBalance,
                        AverageMonthlyNetCashflow,
                        MonthsWithNegativeCashflow,
                        CriticalMonthsCount,
                        CashflowRunway,
                        OperatingMarginRatio,
                        ExpenseRatio,
                        HealthStatus,
                        IsSustainable,
                        CreatedAt,
                        UpdatedAt,
                        IsActive
                    FROM tblCashflowSummary
                    WHERE AssessmentId = @AssessmentId
                    AND IsActive = 1
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var summary = await connection.QuerySingleOrDefaultAsync<CashflowSummary>(query, new { AssessmentId = assessmentId });

                if (summary != null)
                {
                    _logger.LogDebug("Retrieved cashflow summary for assessment {AssessmentId}", assessmentId);
                }
                else
                {
                    _logger.LogWarning("Cashflow summary not found for assessment {AssessmentId}", assessmentId);
                }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cashflow summary for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }


        /// Gets a single month's cashflow data

        public async Task<AssessmentCashflow> GetMonthCashflowAsync(long assessmentId, int month)
        {
            try
            {
                const string query = @"
                    SELECT TOP 1
                        AssessmentCashflowId,
                        AssessmentId,
                        MonthNumber,
                        Year,
                        SalesRevenue,
                        OtherIncome,
                        TotalIncome,
                        COGS,
                        SalaryExpense,
                        RentExpense,
                        UtilityExpense,
                        MarketingExpense,
                        LoanRepayment,
                        OtherExpense,
                        TotalExpense,
                        NetCashflow,
                        OpeningBalance,
                        ClosingBalance,
                        CumulativeCashflow,
                        HasNegativeCashflow,
                        IsCritical,
                        CreatedAt,
                        UpdatedAt,
                        IsActive,
                        Notes
                    FROM tblAssessmentCashflow
                    WHERE AssessmentId = @AssessmentId
                    AND MonthNumber = @MonthNumber
                    AND IsActive = 1
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var cashflow = await connection.QuerySingleOrDefaultAsync<AssessmentCashflow>(query, new { AssessmentId = assessmentId, MonthNumber = month });

                if (cashflow != null)
                {
                    _logger.LogDebug("Retrieved cashflow for assessment {AssessmentId}, month {Month}", assessmentId, month);
                }

                return cashflow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cashflow for assessment {AssessmentId}, month {Month}", assessmentId, month);
                throw;
            }
        }


        /// Clears all cashflow data for an assessment

        public async Task ClearCashflowAsync(long assessmentId)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();

                // Soft delete monthly cashflow records
                const string monthlyDeleteQuery = @"
                    UPDATE tblAssessmentCashflow
                    SET IsActive = 0
                    WHERE AssessmentId = @AssessmentId
                ";

                await connection.ExecuteAsync(monthlyDeleteQuery, new { AssessmentId = assessmentId });

                // Soft delete summary
                const string summaryDeleteQuery = @"
                    UPDATE tblCashflowSummary
                    SET IsActive = 0
                    WHERE AssessmentId = @AssessmentId
                ";

                await connection.ExecuteAsync(summaryDeleteQuery, new { AssessmentId = assessmentId });
                _logger.LogInformation("Cleared cashflow data for assessment {AssessmentId}", assessmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cashflow data for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }


        /// Checks if cashflow data exists for an assessment

        public async Task<bool> CashflowExistsAsync(long assessmentId)
        {
            try
            {
                const string query = @"
                    SELECT COUNT(1)
                    FROM tblAssessmentCashflow
                    WHERE AssessmentId = @AssessmentId
                    AND IsActive = 1
                ";

                using var connection = _dbConnectionFactory.CreateConnection();
                var count = await connection.ExecuteScalarAsync<int>(query, new { AssessmentId = assessmentId });

                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if cashflow exists for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        #endregion
    }
}