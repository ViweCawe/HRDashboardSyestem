using System.Data;
using ClassLibrary.DataAccess;
using ClassLibrary.PeoplAndEmployment.Models;
using Dapper;

namespace ClassLibrary.PeoplAndEmployment.Data;

public sealed class EmployeeRepository(ISqlConnectionFactory connections) : IEmployeeRepository
{
    public async Task<IReadOnlyList<EmployeeListItem>> GetAsync(DateTime asOfDate, string? search = null, int offset = 0, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);
        await using var connection = await connections.OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync<EmployeeListItem>(new CommandDefinition(
            "hr.spEmployee_List", new { AsOfDate = asOfDate.Date, Search = search, Offset = offset, PageSize = pageSize },
            commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
        return rows.AsList();
    }

    public async Task<EmployeeOverview?> GetOverviewAsync(long employmentId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        using var result = await connection.QueryMultipleAsync(new CommandDefinition(
            "hr.spEmployee_Overview", new { EmploymentId = employmentId },
            commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
        var employee = await result.ReadSingleOrDefaultAsync<EmployeeListItem>();
        var engagements = (await result.ReadAsync<EmployeeListItem>()).AsList();
        var history = (await result.ReadAsync<EmployeeHistoryItem>()).AsList();
        var changes = (await result.ReadAsync<EmployeeChangeItem>()).AsList();
        return employee is null ? null : new(employee, engagements, history, changes);
    }
}
