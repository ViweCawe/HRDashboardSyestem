using System.Data;
using System.Text.Json;
using ClassLibrary.DataAccess;
using ClassLibrary.ImportAndStaging.Models;
using Dapper;

namespace ClassLibrary.ImportAndStaging.Data;

public sealed class EmployeeImportRepository(ISqlConnectionFactory connections) : IEmployeeImportRepository
{
    public async Task<ImportBatch> StageAsync(string fileName, string fileHash, DateTime asOfDate, string importedBy, EmployeeImportPreview preview, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<ImportBatch>(new CommandDefinition("hr.spEmployeeImport_Stage", new
        {
            FileName = fileName, FileHash = fileHash, AsOfDate = asOfDate.Date, ImportedBy = importedBy,
            RowsJson = JsonSerializer.Serialize(preview.Rows), IssuesJson = JsonSerializer.Serialize(preview.Issues)
        }, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
    }
    public async Task<IReadOnlyList<ImportBatch>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        return (await connection.QueryAsync<ImportBatch>(new CommandDefinition("hr.spEmployeeImport_List", commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken))).AsList();
    }
    public async Task<IReadOnlyList<ImportIssue>> GetIssuesAsync(long batchId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        return (await connection.QueryAsync<ImportIssue>(new CommandDefinition("hr.spEmployeeImport_Issues", new { ImportBatchId = batchId }, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken))).AsList();
    }
    public async Task PublishAsync(long batchId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("hr.spEmployeeImport_Publish", new { ImportBatchId = batchId },
            commandTimeout: 300, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
    }
}
