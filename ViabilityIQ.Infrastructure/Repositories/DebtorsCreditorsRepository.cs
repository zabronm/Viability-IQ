using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Infrastructure.Repositories
{

    public class DebtorsCreditorsRepository : IDebtorsCreditorsRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILogger<DebtorsCreditorsRepository> _logger;

        public DebtorsCreditorsRepository(
            IDbConnectionFactory dbConnectionFactory,
            ILogger<DebtorsCreditorsRepository> logger)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
        }

        public async Task<DebtorsCreditorsProfile> GetConfigurationAsync(long assessmentId)
        {
            try
            {
                const string sql = @"
                    SELECT * FROM tblDebtorsConfiguration 
                    WHERE AssessmentId = @AssessmentId";

                using var connection = _dbConnectionFactory.CreateConnection();
                var config = await connection.QueryFirstOrDefaultAsync<DebtorsCreditorsProfile>(sql, new { AssessmentId = assessmentId });

                if (config == null)
                {
                    // Return default configuration
                    return new DebtorsCreditorsProfile
                    {
                        AssessmentId = assessmentId,
                        Debtors_30 = 50m,
                        Debtors_60 = 30m,
                        Debtors_90 = 10m,
                        Debtors_120 = 10m,
                        Debtors_120Plus = 10m,
                        AveragePaymentDays = 30,
                        BadDebtPercentage = 2m,
                        Active = true
                    };
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting debtors configuration for assessment {AssessmentId}", assessmentId);
                throw;
            }
        }

        public async Task<long> SetConfigurationAsync(DebtorsCreditorsProfile config)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var existing = await GetConfigurationAsync(config.AssessmentId);

                if (existing.DebtorsCreditorsProfileId == 0)
                {
                    // Insert new
                    config.CreatedDate = DateTime.UtcNow;
                    config.ModifiedDate = DateTime.UtcNow;

                    const string insertSql = @"
                        INSERT INTO tblDebtorsConfiguration 
                        (AssessmentId, Debtors0to30Days, Debtors30to60Days, Debtors60to90Days, 
                         Debtors90to120Days, Debtors120PlusDays, BadDebtPercentage, 
                         AveragePaymentDays, IncludeTax, Active, CreatedDate, CreatedBy, 
                         ModifiedDate, ModifiedBy)
                        VALUES (@AssessmentId, @Debtors0to30Days, @Debtors30to60Days, @Debtors60to90Days,
                                @Debtors90to120Days, @Debtors120PlusDays, @BadDebtPercentage,
                                @AveragePaymentDays, @IncludeTax, @Active, @CreatedDate, @CreatedBy,
                                @ModifiedDate, @ModifiedBy);
                        SELECT CAST(SCOPE_IDENTITY() as bigint);";


                    var id = await connection.ExecuteScalarAsync<long>(insertSql, config);
                    _logger.LogInformation("Created debtors configuration {ConfigId} for assessment {AssessmentId}", id, config.AssessmentId);
                    return id;
                }
                else
                {
                    // Update existing
                    config.DebtorsCreditorsProfileId = existing.DebtorsCreditorsProfileId;
                    config.ModifiedDate = DateTime.UtcNow;

                    const string updateSql = @"
                        UPDATE tblDebtorsConfiguration SET
                            Debtors0to30Days = @Debtors0to30Days,
                            Debtors30to60Days = @Debtors30to60Days,
                            Debtors60to90Days = @Debtors60to90Days,
                            Debtors90to120Days = @Debtors90to120Days,
                            Debtors120PlusDays = @Debtors120PlusDays,
                            BadDebtPercentage = @BadDebtPercentage,
                            AveragePaymentDays = @AveragePaymentDays,
                            IncludeTax = @IncludeTax,
                            ModifiedDate = @ModifiedDate,
                            ModifiedBy = @ModifiedBy
                        WHERE DebtorsCreditorsProfileId = @DebtorsCreditorsProfileId";

                    await connection.ExecuteAsync(updateSql, config);
                    _logger.LogInformation("Updated debtors configuration for assessment {AssessmentId}", config.AssessmentId);
                    return config.DebtorsCreditorsProfileId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting debtors configuration");
                throw;
            }
        }

        public async Task AddPaymentSchedulesAsync(List<DebtorPaymentSchedule> schedules)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                const string sql = @"
                    INSERT INTO tblDebtorPaymentSchedule 
                    (AssessmentId, AssessmentSalesId, SalesMonth, CollectionMonth, SalesAmount, 
                     CollectionAmount, PercentageOfSales, AgeCategory, DaysOutstanding, 
                     CreatedDate, CreatedBy, ModifiedDate, ModifiedBy)
                    VALUES (@AssessmentId, @AssessmentSalesId, @SalesMonth, @CollectionMonth, @SalesAmount,
                            @CollectionAmount, @PercentageOfSales, @AgeCategory, @DaysOutstanding,
                            @CreatedDate, @CreatedBy, @ModifiedDate, @ModifiedBy)";

                var schedulesToInsert = schedules.Select(s => new
                {
                    s.AssessmentId,
                    s.AssessmentSalesId,
                    s.SalesMonth,
                    s.CollectionMonth,
                    s.SalesAmount,
                    s.CollectionAmount,
                    s.PercentageOfSales,
                    s.AgeCategory,
                    s.DaysOutstanding,
                    CreatedDate = DateTime.UtcNow,
                    s.CreatedBy,
                    ModifiedDate = DateTime.UtcNow,
                    s.ModifiedBy
                }).ToList();

                foreach (var schedule in schedulesToInsert)
                {
                    await connection.ExecuteAsync(sql, schedule);
                }

                _logger.LogInformation("Added {Count} payment schedules", schedules.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding payment schedules");
                throw;
            }
        }

        public async Task<List<DebtorPaymentSchedule>> GetPaymentSchedulesAsync(long assessmentId)
        {
            try
            {
                const string sql = @"
                    SELECT * FROM tblDebtorPaymentSchedule 
                    WHERE AssessmentId = @AssessmentId
                    ORDER BY SalesMonth, CollectionMonth";

                using var connection = _dbConnectionFactory.CreateConnection();
                var schedules = await connection.QueryAsync<DebtorPaymentSchedule>(
                    sql, new { AssessmentId = assessmentId });

                return schedules.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment schedules");
                throw;
            }
        }

        public async Task DeletePaymentSchedulesAsync(long assessmentId)
        {
            try
            {
                const string sql = "DELETE FROM tblDebtorPaymentSchedule WHERE AssessmentId = @AssessmentId";
                using var connection = _dbConnectionFactory.CreateConnection();

                var deletedCount = await connection.ExecuteAsync(
                    sql, new { AssessmentId = assessmentId });

                _logger.LogInformation("Deleted {Count} payment schedules for assessment {AssessmentId}", deletedCount, assessmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting payment schedules");
                throw;
            }
        }

        public async Task<List<AssessmentSales>> GetAssessmentSalesAsync(long assessmentId)
        {
            try
            {
                const string sql = @"
                    SELECT * FROM tblAssessmentSales 
                    WHERE AssessmentId = @AssessmentId 
                    AND Active = 1
                    ORDER BY CreatedDate";

                using var connection = _dbConnectionFactory.CreateConnection();
                var sales = await connection.QueryAsync<AssessmentSales>(
                    sql, new { AssessmentId = assessmentId });

                return sales.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assessment sales");
                throw;
            }
        }
    }
}