using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ClassLibrary.DataAccess;

public sealed class SqlConnectionFactory(string? connectionString) : ISqlConnectionFactory
{
    public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new HrConnectionConfigurationException("Configure ConnectionStrings:HrConnection before using HR data.");

        var connection = new SqlConnection();
        try
        {
            connection.ConnectionString = connectionString;
        }
        catch (ArgumentException ex)
        {
            await connection.DisposeAsync();
            throw new HrConnectionConfigurationException("ConnectionStrings:HrConnection is not a valid SQL Server connection string.", ex);
        }
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
