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
        Task<long> InsertLeadAsync(
            LeadSubmission lead,
            CancellationToken cancellationToken = default);
        Task MarkEmailSentAsync(
            long leadSubmissionId,
            DateTime sentAtUtc,
            CancellationToken cancellationToken = default);
    }

    public class LeadRepository : ILeadRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public LeadRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<long> InsertLeadAsync(
            LeadSubmission lead,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
                INSERT INTO dbo.LeadSubmissions
                    (FullName, Email, PhoneNumber, CompanyName, Subject, Message,
                     Status, SubmittedAtUtc)
                OUTPUT INSERTED.LeadSubmissionId
                VALUES
                    (@FullName, @Email, @PhoneNumber, @CompanyName, @Subject,
                     @Message, @Status, @SubmittedAtUtc);
                """;

            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    sql,
                    lead,
                    cancellationToken: cancellationToken));
        }

        public async Task MarkEmailSentAsync(
            long leadSubmissionId,
            DateTime sentAtUtc,
            CancellationToken cancellationToken = default)
        {
            const string sql = """
                UPDATE dbo.LeadSubmissions
                SET EmailSentAtUtc = @SentAtUtc,
                    Status = N'Notified'
                WHERE LeadSubmissionId = @LeadSubmissionId;
                """;

            using var connection = _dbConnectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        LeadSubmissionId = leadSubmissionId,
                        SentAtUtc = sentAtUtc
                    },
                    cancellationToken: cancellationToken));
        }
    }
}