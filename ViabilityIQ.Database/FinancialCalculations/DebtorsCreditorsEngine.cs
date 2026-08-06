using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;
using ViabilityIQ.Shared.FinancialModels;

namespace ViabilityIQ.Application.FinancialCalculations
{
    public class DebtorsCreditorsEngine : IDebtorsCreditorsEngine
    {
        private readonly IDebtorsCreditorsRepository _repo;
        private readonly ILogger<DebtorsCreditorsEngine> _logger;

        public DebtorsCreditorsEngine(IDebtorsCreditorsRepository repo, ILogger<DebtorsCreditorsEngine> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        /// <summary>
        /// Set/Update debtors configuration
        /// </summary>
        public async Task SetConfigurationAsync(long assessmentId, DebtorsConfigurationDto dto)
        {
            try
            {
                _logger.LogInformation(
                    "Setting debtors configuration for assessment {AssessmentId}. Profile: {P0}-{P1}-{P2}-{P3}-{P4}",
                    assessmentId, dto.Debtors_30, dto.Debtors_60,
                    dto.Debtors_90, dto.Debtors_120, dto.Debtors_120Plus);

                var config = new DebtorsCreditorsProfile
                {
                    AssessmentId = assessmentId,
                    Debtors_30 = dto.Debtors_30,
                    Debtors_60 = dto.Debtors_60,
                    Debtors_90 = dto.Debtors_90,
                    Debtors_120 = dto.Debtors_120,
                    Debtors_120Plus = dto.Debtors_120Plus,
                    BadDebtPercentage = dto.BadDebtPercentage,
                    AveragePaymentDays = dto.AveragePaymentDays,
                    IncludeVAT = dto.IncludeTax
                };

                await _repo.SetConfigurationAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting debtors configuration");
                throw;
            }
        }

        /// <summary>
        /// Get debtors configuration
        /// </summary>
        public async Task<DebtorsConfigurationDto> GetConfigurationAsync(long assessmentId)
        {
            try
            {
                var config = await _repo.GetConfigurationAsync(assessmentId);

                return new DebtorsConfigurationDto
                {
                     
                    DebtorsCreditorsProfileId = config.DebtorsCreditorsProfileId,
                    AssessmentId = config.AssessmentId,
                    Debtors_30 = config.Debtors_30,
                    Debtors_60 = config.Debtors_60,
                    Debtors_90 = config.Debtors_90,
                    Debtors_120 = config.Debtors_120,
                    Debtors_120Plus = config.Debtors_120Plus,
                    BadDebtPercentage = config.BadDebtPercentage,
                    AveragePaymentDays = config.AveragePaymentDays,
                    IncludeTax = config.IncludeVAT
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting debtors configuration");
                throw;
            }
        }

        /// <summary>
        /// CORE LOGIC: Generate payment schedules from AssessmentSales data
        /// This reads sales and distributes across months based on collection profile
        /// </summary>
        public async Task GeneratePaymentSchedulesFromSalesAsync(long assessmentId)
        {
            try
            {
                _logger.LogInformation(
                    "Generating payment schedules from sales for assessment {AssessmentId}",
                    assessmentId);

                // Step 1: Delete existing schedules
                await _repo.DeletePaymentSchedulesAsync(assessmentId);

                // Step 2: Get configuration
                var config = await _repo.GetConfigurationAsync(assessmentId);

                // Step 3: Get all AssessmentSales records
                var salesRecords = await _repo.GetAssessmentSalesAsync(assessmentId);

                if (salesRecords == null || salesRecords.Count == 0)
                {
                    _logger.LogWarning("No sales data found for assessment {AssessmentId}", assessmentId);
                    return;
                }

                // Step 4: Generate payment schedules for each sales record
                var allSchedules = new List<DebtorPaymentSchedule>();

                foreach (var sales in salesRecords)
                {
                    var monthlyValues = sales.MonthlyValues;

                    // For each month in the sales record
                    for (int monthIndex = 0; monthIndex < 12; monthIndex++)
                    {
                        var monthSalesAmount = monthlyValues[monthIndex];

                        // Skip if no sales in this month
                        if (monthSalesAmount <= 0)
                            continue;

                        var salesMonth = monthIndex + 1; // 1-12

                        // Create payment schedule for this month's sales
                        // Distribution based on collection profile

                        // Collection in same month (0-30 days)
                        allSchedules.Add(new DebtorPaymentSchedule
                        {
                            AssessmentId = assessmentId,
                            AssessmentSalesId = sales.AssessmentSalesId,
                            SalesMonth = salesMonth,
                            CollectionMonth = salesMonth,
                            SalesAmount = monthSalesAmount,
                            CollectionAmount = monthSalesAmount * (config.Debtors_30 / 100),
                            PercentageOfSales = config.Debtors_30,
                            AgeCategory = "0-30 days",
                            DaysOutstanding = 15
                        });

                        // Collection next month (30-60 days)
                        if (salesMonth + 1 <= 12)
                        {
                            allSchedules.Add(new DebtorPaymentSchedule
                            {
                                AssessmentId = assessmentId,
                                AssessmentSalesId = sales.AssessmentSalesId,
                                SalesMonth = salesMonth,
                                CollectionMonth = salesMonth + 1,
                                SalesAmount = monthSalesAmount,
                                CollectionAmount = monthSalesAmount * (config.Debtors_60 / 100),
                                PercentageOfSales = config.Debtors_60,
                                AgeCategory = "30-60 days",
                                DaysOutstanding = 45
                            });
                        }

                        // Collection 2 months later (60-90 days)
                        if (salesMonth + 2 <= 12)
                        {
                            allSchedules.Add(new DebtorPaymentSchedule
                            {
                                AssessmentId = assessmentId,
                                AssessmentSalesId = sales.AssessmentSalesId,
                                SalesMonth = salesMonth,
                                CollectionMonth = salesMonth + 2,
                                SalesAmount = monthSalesAmount,
                                CollectionAmount = monthSalesAmount * (config.Debtors_90 / 100),
                                PercentageOfSales = config.Debtors_90,
                                AgeCategory = "60-90 days",
                                DaysOutstanding = 75
                            });
                        }

                        // Collection 3 months later (90-120 days)
                        if (salesMonth + 3 <= 12)
                        {
                            allSchedules.Add(new DebtorPaymentSchedule
                            {
                                AssessmentId = assessmentId,
                                AssessmentSalesId = sales.AssessmentSalesId,
                                SalesMonth = salesMonth,
                                CollectionMonth = salesMonth + 3,
                                SalesAmount = monthSalesAmount,
                                CollectionAmount = monthSalesAmount * (config.Debtors_120 / 100),
                                PercentageOfSales = config.Debtors_120,
                                AgeCategory = "90-120 days",
                                DaysOutstanding = 105
                            });
                        }

                        // Collection 4+ months later (120+ days)
                        if (salesMonth + 4 <= 12)
                        {
                            allSchedules.Add(new DebtorPaymentSchedule
                            {
                                AssessmentId = assessmentId,
                                AssessmentSalesId = sales.AssessmentSalesId,
                                SalesMonth = salesMonth,
                                CollectionMonth = salesMonth + 4,
                                SalesAmount = monthSalesAmount,
                                CollectionAmount = monthSalesAmount * (config.Debtors_120Plus / 100),
                                PercentageOfSales = config.Debtors_120Plus,
                                AgeCategory = "120+ days",
                                DaysOutstanding = 150
                            });
                        }
                    }
                }

                // Step 5: Add all schedules to database
                if (allSchedules.Count > 0)
                {
                    await _repo.AddPaymentSchedulesAsync(allSchedules);
                    _logger.LogInformation(
                        "Generated {Count} payment schedules for assessment {AssessmentId}",
                        allSchedules.Count, assessmentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payment schedules from sales");
                throw;
            }
        }

        /// <summary>
        /// Get complete collection table for UI display
        /// </summary>
        public async Task<DebtorsCollectionTableDto> GetCollectionTableAsync(long assessmentId)
        {
            try
            {
                var schedules = await _repo.GetPaymentSchedulesAsync(assessmentId);
                var salesRecords = await _repo.GetAssessmentSalesAsync(assessmentId);
                var config = await GetConfigurationAsync(assessmentId);

                // Build rows (one per AssessmentSales record)
                var rows = new List<DebtorSalesRowDto>();

                foreach (var sales in salesRecords)
                {
                    var row = new DebtorSalesRowDto
                    {
                        AssessmentSalesId = sales.AssessmentSalesId,
                        Description = sales.Description,
                        InvoiceMonth = GetMonthName(DateTime.UtcNow),  // First sales month
                        InvoicedAmount = sales.TotalNoVAT,  // Use TotalNoVAT from AssessmentSales
                        MonthlyCollections = new Dictionary<int, decimal>()
                    };

                    // Populate collections for each month (M1-M12)
                    for (int month = 1; month <= 12; month++)
                    {
                        var collection = schedules
                            .Where(s => s.AssessmentSalesId == sales.AssessmentSalesId &&
                                       s.CollectionMonth == month)
                            .Sum(s => s.CollectionAmount);

                        row.MonthlyCollections[month] = collection;
                    }

                    row.TotalCollected = row.MonthlyCollections.Values.Sum();
                    row.OutstandingBalance = row.InvoicedAmount - row.TotalCollected;
                    row.EstimatedBadDebt = row.InvoicedAmount * (config.BadDebtPercentage / 100);

                    rows.Add(row);
                }

                // Build table DTO
                var table = new DebtorsCollectionTableDto
                {
                    AssessmentId = assessmentId,
                    SalesRows = rows,
                    Configuration = config,
                    TotalInvoiced = rows.Sum(r => r.InvoicedAmount),
                    TotalCollected = rows.Sum(r => r.TotalCollected),
                    TotalOutstanding = rows.Sum(r => r.OutstandingBalance),
                    TotalEstimatedBadDebt = rows.Sum(r => r.EstimatedBadDebt),
                    DaysSalesOutstanding = await CalculateDaysSalesOutstandingAsync(assessmentId)
                };

                _logger.LogInformation(
                    "Collection table built. Total Invoiced: {Total}, Outstanding: {Outstanding}",
                    table.TotalInvoiced, table.TotalOutstanding);

                return table;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building collection table");
                throw;
            }
        }

        /// <summary>
        /// Get monthly debtors aging breakdown
        /// </summary>
        public async Task<List<DebtorsAgingSummaryDto>> GetAgingSummaryAsync(long assessmentId)
        {
            try
            {
                var schedules = await _repo.GetPaymentSchedulesAsync(assessmentId);
                var agingSummaries = new List<DebtorsAgingSummaryDto>();

                for (int month = 1; month <= 12; month++)
                {
                    var aging = new DebtorsAgingSummaryDto { Month = month };

                    // Get all collections for this month and categorize by age
                    var monthSchedules = schedules.Where(s => s.CollectionMonth == month).ToList();

                    aging.Current = monthSchedules
                        .Where(s => s.AgeCategory == "0-30 days")
                        .Sum(s => s.CollectionAmount);

                    aging.Debtors_30 = monthSchedules
                        .Where(s => s.AgeCategory == "0-30 days")
                        .Sum(s => s.CollectionAmount);

                    aging.Debtors_60 = monthSchedules
                        .Where(s => s.AgeCategory == "30-60 days")
                        .Sum(s => s.CollectionAmount);

                    aging.Debtors_90 = monthSchedules
                        .Where(s => s.AgeCategory == "60-90 days")
                        .Sum(s => s.CollectionAmount);

                    aging.Debtors_120 = monthSchedules
                        .Where(s => s.AgeCategory == "90-120 days")
                        .Sum(s => s.CollectionAmount);

                    aging.Debtors_120Plus = monthSchedules
                        .Where(s => s.AgeCategory == "120+ days")
                        .Sum(s => s.CollectionAmount);

                    aging.TotalOutstanding = aging.Current + aging.Debtors_60 +
                                           aging.Debtors_90 + aging.Debtors_120 + aging.Debtors_120Plus;

                    agingSummaries.Add(aging);
                }

                return agingSummaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating aging summary");
                throw;
            }
        }

        /// <summary>
        /// Calculate Days Sales Outstanding (DSO)
        /// </summary>
        public async Task<decimal> CalculateDaysSalesOutstandingAsync(long assessmentId)
        {
            try
            {
                var schedules = await _repo.GetPaymentSchedulesAsync(assessmentId);
                var salesRecords = await _repo.GetAssessmentSalesAsync(assessmentId);

                if (schedules.Count == 0 || salesRecords.Count == 0)
                    return 0m;

                // Total sales
                var totalSales = salesRecords.Sum(s => s.TotalNoVAT);

                // Average daily sales
                var avgDailySales = totalSales / 365;

                // Average accounts receivable (outstanding by age bucket average)
                var avgReceivables = schedules
                    .GroupBy(s => s.SalesMonth)
                    .Average(g => g.First().SalesAmount - g.Sum(s => s.CollectionAmount));

                // DSO = Average AR / Average Daily Sales
                return avgDailySales > 0 ? avgReceivables / avgDailySales : 0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating DSO");
                throw;
            }
        }

        /// <summary>
        /// Calculate total outstanding debtors
        /// </summary>
        public async Task<decimal> CalculateTotalOutstandingAsync(long assessmentId)
        {
            try
            {
                var schedules = await _repo.GetPaymentSchedulesAsync(assessmentId);
                var salesRecords = await _repo.GetAssessmentSalesAsync(assessmentId);

                var outstanding = salesRecords.Sum(s => s.TotalNoVAT) -
                                 schedules.Sum(s => s.CollectionAmount);

                return outstanding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total outstanding");
                throw;
            }
        }

        /// <summary>
        /// Calculate total estimated bad debt
        /// </summary>
        public async Task<decimal> CalculateTotalBadDebtAsync(long assessmentId)
        {
            try
            {
                var config = await GetConfigurationAsync(assessmentId);
                var salesRecords = await _repo.GetAssessmentSalesAsync(assessmentId);

                var badDebt = salesRecords.Sum(s => s.TotalNoVAT * (config.BadDebtPercentage / 100));

                return badDebt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total bad debt");
                throw;
            }
        }

        #region Helper Methods

        private string GetMonthName(DateTime date)
        {
            return $"{date:MMM}-{date:yy}";
        }

        #endregion
    }
}