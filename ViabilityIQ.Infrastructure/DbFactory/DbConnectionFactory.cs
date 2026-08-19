using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace ViabilityIQ.Infrastructure.DbFactory;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration? _configuration;
    public DbConnectionFactory(IConfiguration configuration) => _configuration = configuration;   

    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_configuration!.GetConnectionString("ViabilityIQ_Connection"));       
        //var connection = new SqlConnection(_configuration!.GetConnectionString("Viability_SMARTNET"));      //This is the live connection on SmartNET     
        return connection;
    }
}

