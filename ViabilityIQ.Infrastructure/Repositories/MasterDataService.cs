using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Dtos;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels;
using System.Text.RegularExpressions;


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


        #region SQL Identifier Validation  and SafeIdentified methods      
        /// Validates a SQL identifier (table name, field name, schema name).
        /// Only letters, numbers and underscores are permitted.          
        private static void ValidateSqlIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("SQL identifier cannot be empty.");

            if (!Regex.IsMatch(identifier, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                throw new ArgumentException(
                    $"Invalid SQL identifier '{identifier}'.");
        }

        private static string SafeIdentifier(string identifier)
        {
            ValidateSqlIdentifier(identifier);

            return $"[{identifier}]";
        }
        #endregion


        #region Bank CRUD methods original
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

        #endregion

        #region LoanType CRUD methods Original ==========

        //-------LOAN TYPE CRUD OPERATIONS-------
        public async Task<LoanType?> GetLoanTypeByIdAsync(long loanTypeId) => await _dbConnectionFactory.CreateConnection().GetAsync<LoanType>(loanTypeId);

        public async Task<IEnumerable<LoanType>> GetAllLoanTypesAsync()
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var loanTypes = await connection.GetAllAsync<LoanType>();
                return loanTypes.OrderBy(l => l.LoanTypeName).ToList();
            }
            catch (Exception ex)
            {
                // Log the exception (ex) as needed
                return null;
            }
        }

        public async Task<bool> SaveLoanTypeAsync(LoanType loanType)
        {
            using var connection = _dbConnectionFactory.CreateConnection();

            if (loanType.LoanTypeId == 0)
            {
                loanType.CreatedDate = DateTime.UtcNow;             // Set metadata values automatically on creation
                loanType.CreatedBy = _sessionService.UserId;
                loanType.Active = true;

                var newId = await connection.InsertAsync(loanType);     // InsertAsync automatically maps all properties and inserts them safely
                return newId > 0;
            }
            else
            {
                loanType.ModifiedDate = DateTime.UtcNow;       // Maintain audit trail details on modifications
                loanType.ModifiedBy = _sessionService.UserId;
                return await connection.UpdateAsync(loanType);  // UpdateAsync automatically matches the [Key] property to modify the row
            }
        }

        public async Task<bool> DeleteLoanTypeAsync(LoanType loanType)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.DeleteAsync(loanType); // Automatically runs: DELETE FROM LoanTypes WHERE LoanTypeId = @id
        }


        //================= ASSESSMENT LOANS CRUD OPERATIONS ==============================

        public async Task<AssessmentLoanDto?> GetAssessmentLoanByIdAsync(long assessmentLoanId)
            => await _dbConnectionFactory.CreateConnection().GetAsync<AssessmentLoanDto>(assessmentLoanId);


        public async Task<IEnumerable<AssessmentLoanDto>> GetAssessmentLoansByIdAsync(long assessmentId)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                const string sql = @"SELECT  AssessmentLoanId, 
                     AssessmentId, 
                     LoanDate, 
                     LoanTypeName, 
                     BankName, 
                     LoanAmount, 
                     LoanBalanceAtAssessmentDate, 
                     InterestRatePerAnnum, 
                     RepaymentPeriodMonths, 
                     MinimumRepaymentAmount, 
                     ActualRepaymentAmount, 
                     Active
                     FROM vw_assessment_loans_list
                     WHERE AssessmentId = @AssessmentId";

                return await connection.QueryAsync<AssessmentLoanDto>(
                    sql, new
                    {
                        AssessmentId = assessmentId
                    });

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion 




        #region generic methods to get 1 annd LIST record/s passing a formatted SQL statement with 1 field parameter

        //================= GENERIC DTO READING METHODS, (1)READ 1 RECORD AND  (2) READ LIST F RECORDS ==============================
        //================= suitable for reading views into dtos  ===================================================================
        public async Task<T?> GetByIdAsync<T>(long id, string? sql = null, string paramName = "Id") where T : class
        {
            using var connection = _dbConnectionFactory.CreateConnection();

            if (string.IsNullOrWhiteSpace(sql))      // If no custom SQL is supplied, default to the ORM's direct primary key mapping method
            {
                return await connection.GetAsync<T>(id);
            }

            var parameters = new Dictionary<string, object> { { paramName, id } };  // Dynamic parameter mapping for explicit query structures
            return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
        }

        public async Task<IEnumerable<T>> GetListByIdAsync<T>(long id, string sql, string paramName = "Id") where T : class
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                var parameters = new Dictionary<string, object> { { paramName, id } };   // Dynamically assigns the tracking value to your specific query parameter name

                return await connection.QueryAsync<T>(sql, parameters);
            }
            catch (Exception)
            {
                // Maintains your current exception bubble up strategy
                throw;
            }
        }

        #endregion

        #region ===== 2 methods to get a single record based on one or multiple fied conditions ======================

        //===== usage examples =================================================================
        //--- var assessment = await ViqCrudService.GetSingleAsync<AssessmentDto>("tblAssessment", new{AssessmentId}); ---single parameter
        //--- var assessment = await ViqCrudService.GetSingleAsync<AssessmentDto>("tblAssessment",new {AssessmentId, Active = true}); ---- multiple parameters

        public async Task<T?> GetSingleAsync<T>(string tableName, object conditions)
        {

            ValidateSqlIdentifier(tableName);

            string whereClause = BuildWhereClause(conditions);

            string sql = $@"SELECT * FROM {SafeIdentifier(tableName)} WHERE {whereClause};";


            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                return await connection.QuerySingleOrDefaultAsync<T>(sql, conditions);
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        //---- GETS LIST OF RECORDS USING SINGLE OR MULTIPLE CONDITIONS, WITH ORDERING OPTIONS
        // ------USAGE ------ multiple conditions with ordering
        //  var businesses =  await ViqCrudService.GetListAsync<BusinessDto>("tblBusiness", 
        //                                                    new
        //                                                    {
        //                                                        ProvinceId,
        //                                                        Active = true
        //                                                    },
        //                                                    "BusinessName");
        //


        public async Task<IEnumerable<T>> GetListAsync<T>(string tableName,
                                                          object conditions,
                                                          string? orderBy = null,
                                                          bool ascending = true)
        {
            ValidateSqlIdentifier(tableName);
            string whereClause = BuildWhereClause(conditions);

            string sql = $@"SELECT * FROM {SafeIdentifier(tableName)} WHERE {whereClause}";
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                ValidateSqlIdentifier(orderBy);
                sql += $@" ORDER BY {SafeIdentifier(orderBy)} {(ascending ? "ASC" : "DESC")}";
            }

            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                return await connection.QueryAsync<T>(sql, conditions);
            }
            catch (Exception)
            {
                throw;
            }
        }


        private static string BuildWhereClause(object conditions)
        {
            if (conditions == null)
                throw new ArgumentNullException(nameof(conditions));

            var properties = conditions.GetType().GetProperties();

            if (properties.Length == 0)
                throw new ArgumentException(
                    "At least one condition must be supplied.");

            return string.Join(
                " AND ",
                properties.Select(property =>
                {
                    ValidateSqlIdentifier(property.Name);

                    return $"{SafeIdentifier(property.Name)} = @{property.Name}";
                }));
        }
        #endregion 





        // GENERIC RUN SQL METHODS 
        // Example usage: var count = await repo.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Farmers");
        public async Task<T> ExecuteScalarAsync<T>(string sql, object parameters = null)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                return await connection.ExecuteScalarAsync<T>(sql, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        // Generic method to execute commands (INSERT/UPDATE/DELETE)
        // Example usage: await repo.ExecuteCommandAsync("UPDATE Farmers SET Name = @Name WHERE Id = @Id", new { Name = "John", Id = 1 });
        public async Task<int> ExecuteCommandAsync(string sql, object parameters = null)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            if (connection.State != ConnectionState.Open) connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                var rowsAffected = await connection.ExecuteAsync(sql, parameters, transaction);
                transaction.Commit();
                return rowsAffected;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }


        //================================ GENERIC HELPER METHODS SECTION - RECORDEXISTS/COUNT RECORDS/GET FIELD/ ETC


        //1. ===================  check if record exists ==================
        public async Task<bool> RecordExistsAsync(string tableName, string keyFieldName, object keyValue)
        {
            ValidateSqlIdentifier(tableName);
            ValidateSqlIdentifier(keyFieldName);


            string sql = $@"SELECT CASE
                        WHEN EXISTS
                        (
                            SELECT 1 FROM {tableName} WHERE {keyFieldName}=@parKeyValue
                        )
                        THEN CAST(1 AS bit)
                        ELSE CAST(0 AS bit)
                        END";

            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<bool>(sql,
                new
                {
                    parKeyValue = keyValue
                });
        }


        //2. ======================== DLOOKUP ======================================================================
        // usage examples
        //string? clientName =  await ViqCrudService.LookupAsync<string>( "tblClient", "ClientName", "ClientId",clientId);
        //decimal vatRate = await ViqCrudService.LookupAsync<decimal>( "tblAssessment", "VATRate", "AssessmentId", AssessmentId);
        public async Task<T?> LookAsync<T>(string tableName, string returnField, string keyField, object keyValue)
        {
            ValidateSqlIdentifier(tableName);
            ValidateSqlIdentifier(returnField);
            ValidateSqlIdentifier(keyField);


            string sql = $@"SELECT TOP(1) {returnField} FROM {tableName} WHERE {keyField}= @parKeyValue";

            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<T>(
                sql,
                new { parKeyValue = keyValue }
                );
        }



        //3. ===================== Count records meeting a key criteria ===================================== CountAsync
        public async Task<int> CountAsync(string tableName, string? keyField = null, object? keyValue = null)
        {
            ValidateSqlIdentifier(tableName);

            string sql;
            object? parameters = null;

            if (string.IsNullOrWhiteSpace(keyField))
            {
                sql = $"SELECT COUNT(*) FROM {tableName}";
            }
            else
            {
                sql = $@" SELECT COUNT(*) FROM {tableName} WHERE {keyField}= @parKeyValue";
                parameters = new
                {
                    parKeyValue = keyValue
                };
            }

            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, parameters);
        }


        //4. ===================== Sum records meeting a key criteria ===================================== SumAsync
        public async Task<decimal> SumAsync(string tableName,
                                            string fieldName,
                                            string? keyField = null,
                                            object? keyValue = null)
        {
            ValidateSqlIdentifier(tableName);
            ValidateSqlIdentifier(fieldName);

            string sql;
            object? parameters = null;

            if (string.IsNullOrWhiteSpace(keyField))
            {
                sql = $@"SELECT ISNULL(SUM({fieldName}),0) FROM {tableName}";
            }
            else
            {
                sql = $@"SELECT ISNULL(SUM({fieldName}),0) FROM {tableName} WHERE {keyField}= @parKeyValue";
                parameters = new
                {
                    KeyValue = keyValue
                };
            }

            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<decimal>(sql, parameters);
        }


        //5. ===================== Min records meeting a key criteria ===================================== MinAsync
        public async Task<T?> MinAsync<T>(string tableName, string fieldName)
        {
            ValidateSqlIdentifier(tableName);
            ValidateSqlIdentifier(fieldName);

            string sql = $"SELECT MIN({fieldName}) FROM {tableName}";

            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<T>(sql);
        }


        //6. ===================== Max records meeting a key criteria ===================================== MaxAsync
        public async Task<T?> MaxAsync<T>(string tableName, string fieldName)
        {
            ValidateSqlIdentifier(tableName);
            ValidateSqlIdentifier(fieldName);

            string sql = $"SELECT MAX({fieldName}) FROM {tableName}";

            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<T>(sql);
        }


        //7. ===================== Average records meeting a key criteria ===================================== AverageAsync
        public async Task<decimal> AverageAsync(string tableName, string fieldName)
        {
            ValidateSqlIdentifier(tableName);
            ValidateSqlIdentifier(fieldName);

            string sql = $"SELECT ISNULL(AVG({fieldName}),0) FROM {tableName}";

            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<decimal>(sql);
        }


        //8. ===================== Update 1 field meeting a key criteria ===================================== UpdateFieldAsync
        public async Task<int> UpdateFieldAsync(string tableName,
                                                string updateField,
                                                object updateValue,
                                                string keyField,
                                                object keyValue)
        {
            ValidateSqlIdentifier(tableName);
            ValidateSqlIdentifier(updateField);
            ValidateSqlIdentifier(keyField);

            string sql = $@"UPDATE {tableName} SET {updateField}= @parUpdateValue WHERE {keyField}= @parKeyValue";

            using var connection = _dbConnectionFactory.CreateConnection();

            return await connection.ExecuteAsync(
                sql,
                new
                {
                    parUpdateValue = updateValue,
                    parKeyValue = keyValue
                });
        }

        //8. ===================== Execute sql ===================================== UpdateFieldAsync
        public async Task<int> ExecuteAsync(string sql, object? parameters = null)
        {
            using var connection = _dbConnectionFactory.CreateConnection();
            return await connection.ExecuteAsync(sql, parameters);
        }



    }
}
