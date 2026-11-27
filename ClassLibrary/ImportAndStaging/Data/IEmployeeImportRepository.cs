using ClassLibrary.ImportAndStaging.Models;

namespace ClassLibrary.ImportAndStaging.Data;

public interface IEmployeeImportRepository
{
    Task<ImportBatch> StageAsync(string fileName, string fileHash, DateTime asOfDate, string importedBy, EmployeeImportPreview preview, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportBatch>> GetRecentAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportIssue>> GetIssuesAsync(long batchId, CancellationToken cancellationToken = default);
    Task PublishAsync(long batchId, CancellationToken cancellationToken = default);
}
