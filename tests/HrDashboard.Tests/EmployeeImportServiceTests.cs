using System.Text;
using ClassLibrary.ImportAndStaging.Data;
using ClassLibrary.ImportAndStaging.Models;
using ClassLibrary.ImportAndStaging.Services;
using Xunit;

namespace HrDashboard.Tests;

public sealed class EmployeeImportServiceTests
{
    private static readonly DateTime AsOf = new(2026, 7, 31);
    private const string Csv = "Employee Code,First Name,Last Name,Date Engaged,Company,Employee Status,ID Number\nC001,Alex,Example,01/02/2020,001 - Company,A - Active,TEST-ID-001";

    [Fact]
    public async Task ValidUploadPreservesActorSourceDateAndRows()
    {
        var repository = new StagingRepository();
        var service = new EmployeeImportService(new(), repository);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Csv));
        var batch = await service.StageAsync(stream, @"C:\Exports\employees.csv", AsOf, "hr-user");
        Assert.Equal(1, repository.Calls);
        Assert.Equal("employees.csv", repository.FileName);
        Assert.Equal("hr-user", repository.Actor);
        Assert.Equal(AsOf, repository.AsOfDate);
        Assert.Equal("Staged", batch.Status);
        var preview = Assert.IsType<EmployeeImportPreview>(repository.Preview);
        Assert.Single(preview.Rows);
        Assert.Empty(preview.Issues);
    }

    [Fact]
    public async Task ValidationFailuresReachStagingAsReviewIssues()
    {
        var repository = new StagingRepository();
        var service = new EmployeeImportService(new(), repository);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Csv.Replace("TEST-ID-001", "")));
        var batch = await service.StageAsync(stream, "employees.csv", AsOf, "hr-user");
        Assert.Equal("Review", batch.Status);
        Assert.Contains(repository.Preview!.Issues, i => i.RuleCode == "MissingIdentity");
        Assert.Equal("Invalid", Assert.Single(repository.Preview.Rows).ValidationStatus);
    }

    [Fact]
    public async Task OversizedUploadCannotReachTheDatabase()
    {
        var repository = new StagingRepository();
        var service = new EmployeeImportService(new(), repository);
        await using var stream = new MemoryStream(new byte[EmployeeImportService.MaxFileSize + 1]);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.StageAsync(stream, "employees.csv", AsOf, "hr-user"));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task InvalidUtf8CannotReachTheDatabase()
    {
        var repository = new StagingRepository();
        var service = new EmployeeImportService(new(), repository);
        await using var stream = new MemoryStream([0xFF, 0xFE, 0xC0]);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.StageAsync(stream, "employees.csv", AsOf, "hr-user"));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task UploadRequiresAnActorForAuditLineage()
    {
        var repository = new StagingRepository();
        var service = new EmployeeImportService(new(), repository);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Csv));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.StageAsync(stream, "employees.csv", AsOf, " "));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task CancellationCannotStageAnIncompleteFile()
    {
        var repository = new StagingRepository();
        var service = new EmployeeImportService(new(), repository);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Csv));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.StageAsync(stream, "employees.csv", AsOf, "hr-user", cancellationToken: cancellation.Token));
        Assert.Equal(0, repository.Calls);
    }

    private sealed class StagingRepository : IEmployeeImportRepository
    {
        public int Calls { get; private set; }
        public string? FileName { get; private set; }
        public string? Actor { get; private set; }
        public DateTime AsOfDate { get; private set; }
        public EmployeeImportPreview? Preview { get; private set; }
        public Task<ImportBatch> StageAsync(string fileName, string fileHash, DateTime asOfDate, string importedBy, EmployeeImportPreview preview, CancellationToken cancellationToken = default)
        {
            Calls++; FileName = fileName; Actor = importedBy; AsOfDate = asOfDate; Preview = preview;
            return Task.FromResult(new ImportBatch { Status = preview.Issues.Count == 0 ? "Staged" : "Review" });
        }
        public Task<IReadOnlyList<ImportBatch>> GetRecentAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ImportIssue>> GetIssuesAsync(long batchId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task PublishAsync(long batchId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
