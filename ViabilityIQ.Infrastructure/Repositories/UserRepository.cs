using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViabilityIQ.Application.Interfaces.IdentityInterfaces;
using ViabilityIQ.Infrastructure.DbFactory;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Infrastructure.Repositories
{
    public class UserRepository: IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IDbConnectionFactory connectionFactory, ILogger<UserRepository> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a user by email using Dapper
        /// </summary>
        public async Task<ApplicationUser> GetUserByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogDebug("GetUserByEmailAsync: Email is null or empty");
                    return null;
                }

                _logger.LogInformation("GetUserByEmailAsync: Querying user by email: {Email}", email);

                const string query = @"
                    SELECT 
                        UserId as Id,
                        UserName,
                        Email,
                        FirstName,
                        LastName,
                        PhoneNumber,
                        Department,
                        JobTitle,
                        Address,
                        City,
                        Country,
                        BranchId,
                        IsActive,
                        CreatedAt,
                        UpdatedAt,
                        LastLoginAt
                    FROM tblApplicationUsers
                    WHERE Email = @Email";

                using (var connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    var user = await connection.QueryFirstOrDefaultAsync<ApplicationUser>(
                        query,
                        new { Email = email });

                    if (user != null)
                    {
                        _logger.LogInformation("GetUserByEmailAsync: Found user {Email} with UserId {UserId}",
                            email, user.Id);
                    }
                    else
                    {
                        _logger.LogWarning("GetUserByEmailAsync: User not found for email: {Email}", email);
                    }

                    return user;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserByEmailAsync: Error getting user by email: {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Gets a user by ID using Dapper
        /// </summary>
        public async Task<ApplicationUser> GetUserByIdAsync(long userId)
        {
            try
            {
                if (userId <= 0)
                {
                    _logger.LogDebug("GetUserByIdAsync: UserId is invalid: {UserId}", userId);
                    return null;
                }

                _logger.LogInformation("GetUserByIdAsync: Querying user by ID: {UserId}", userId);

                const string query = @"
                    SELECT 
                        UserId as Id,
                        UserName,
                        Email,
                        FirstName,
                        LastName,
                        PhoneNumber,
                        Department,
                        JobTitle,
                        Address,
                        City,
                        Country,
                        BranchId,
                        IsActive,
                        CreatedAt,
                        UpdatedAt,
                        LastLoginAt
                    FROM tblApplicationUsers
                    WHERE UserId = @UserId";

                using (var connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    var user = await connection.QueryFirstOrDefaultAsync<ApplicationUser>(
                        query,
                        new { UserId = userId });

                    if (user != null)
                    {
                        _logger.LogInformation("GetUserByIdAsync: Found user {Email} with UserId {UserId}",
                            user.Email, userId);
                    }
                    else
                    {
                        _logger.LogWarning("GetUserByIdAsync: User not found for ID: {UserId}", userId);
                    }

                    return user;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserByIdAsync: Error getting user by ID: {UserId}", userId);
                throw;
            }
        }
    }
}