using System.Data.Common;

namespace ClassLibrary.DataAccess;

public interface ISqlConnectionFactory
{
    Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default);
}
