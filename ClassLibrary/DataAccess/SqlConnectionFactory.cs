using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ClassLibrary.DataAccess;

public sealed class SqlConnectionFactory(string? connectionString) : ISqlConnectionFactory
{
    public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:HrConnection before using HR data.");

        var connection = new SqlConnection(connectionString);
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
