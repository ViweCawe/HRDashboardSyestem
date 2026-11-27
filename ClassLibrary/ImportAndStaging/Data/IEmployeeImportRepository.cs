using ClassLibrary.ImportAndStaging.Models;

namespace ClassLibrary.ImportAndStaging.Data;

public interface IEmployeeImportRepository
{
    Task<ImportBatch> StageAsync(string fileName, string fileHash, DateTime asOfDate, string importedBy, EmployeeImportPreview preview, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportBatch>> GetRecentAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportIssue>> GetIssuesAsync(long batchId, CancellationToken cancellationToken = default);
    Task<EmployeeImportBatchSummary?> GetBatchAsync(long batchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeImportPreviewRow>> GetRowsAsync(long batchId, int offset = 0, int pageSize = 50, string? status = null, CancellationToken cancellationToken = default);
    Task<ImportDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task PublishAsync(long batchId, CancellationToken cancellationToken = default);
}
