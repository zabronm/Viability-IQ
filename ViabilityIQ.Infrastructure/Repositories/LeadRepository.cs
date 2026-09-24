using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Infrastructure.Repositories
{
    public interface ILeadRepository
    {
        Task InsertLeadAsync(LeadSubmission lead);
    }

    public class LeadRepository : ILeadRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public LeadRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task InsertLeadAsync(LeadSubmission lead)
        {

            try
            {

                var sql = @"INSERT INTO LeadSubmissions (FullName, Email, CompanyName, AssetVolumeScale, SubmittedAt) 
                        VALUES (@FullName, @Email, @CompanyName, @AssetVolumeScale, @SubmittedAt)";

                var parameters = new
                {
                    lead.FullName,
                    lead.Email,
                    lead.CompanyName,
                    lead.AssetVolumeScale,
                    SubmittedAt = DateTime.UtcNow
                };

                using var connection = _dbConnectionFactory.CreateConnection();
                await connection.ExecuteAsync(sql, parameters);

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}