using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Infrastructure.Repositories
{
    public class MasterDataService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ISessionService _sessionService;

        //private readonly ILogger<MasterDataService> _logger;
        //private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1); // Cache duration can be adjusted as needed
        private readonly string _cacheKey = "MasterData";
        private readonly object _cacheLock = new();
        private readonly object _lock = new();

        public MasterDataService(IDbConnectionFactory connectionFactory, ISessionService sessionService)
        {
            _dbConnectionFactory = connectionFactory;
            _sessionService = sessionService;
        }


        //-------BANK CRUD OPERATIONS-------
        public async Task<Bank?> GetBankByIdAsync(long bankId) => await _dbConnectionFactory.CreateConnection().GetAsync<Bank>(bankId);// Implement caching logic here if needed

        public async Task<IEnumerable<Bank>> GetAllBanksAsync()
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var banks = await connection.GetAllAsync<Bank>();
                return banks.OrderBy(b => b.BankName).ToList();
            }
            catch (Exception ex)
            {
                                // Log the exception (ex) as needed
                return null;
            }
            
        }

        public async Task<bool> SaveBankAsync(Bank bank)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            //var runtimeUser = _session.UserEmail ?? "System.Operator";

            if (bank.BankId == 0)
            {
                bank.CreatedDate = DateTime.UtcNow;             // Set metadata values automatically on creation
                bank.CreatedBy = _sessionService.UserId;
                bank.Active = true;
                
                var newId = await connection.InsertAsync(bank);     // InsertAsync automatically maps all properties and inserts them safely
                return newId > 0;
            }
            else
            {                
                bank.ModifiedDate = DateTime.UtcNow;       // Maintain audit trail details on modifications
                bank.ModifiedBy = _sessionService.UserId;                
                return await connection.UpdateAsync(bank);  // UpdateAsync automatically matches the [Key] property to modify the row
            }
        }

        public async Task<bool> DeleteBankAsync(Bank bank)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.DeleteAsync(bank); // Automatically runs: DELETE FROM Banks WHERE BankId = @id
        }


        //-------PRODUCT CATEGORY CRUD OPERATIONS-------

        //


    }
}
