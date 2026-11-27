using System.Security.Cryptography;
using System.Text;
using ClassLibrary.ImportAndStaging.Data;
using ClassLibrary.ImportAndStaging.Models;

namespace ClassLibrary.ImportAndStaging.Services;

public sealed class EmployeeImportService(EmployeeCsvReader reader, IEmployeeImportRepository imports)
{
    public const int MaxFileSize = 5 * 1024 * 1024;

    public async Task<ImportBatch> StageAsync(Stream stream, string fileName, DateTime asOfDate, string importedBy, string delimiter = ",", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(importedBy);
        fileName = Path.GetFileName(fileName.Replace('\\', '/'));
        if (!Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Select a CSV export.");
        if (fileName.Length > 260) throw new InvalidDataException("The filename exceeds 260 characters.");
        using var buffer = new MemoryStream();
        var block = new byte[81920];
        int count;
        while ((count = await stream.ReadAsync(block, cancellationToken)) > 0)
        {
            if (buffer.Length + count > MaxFileSize) throw new InvalidDataException("The CSV exceeds 5 MB.");
            await buffer.WriteAsync(block.AsMemory(0, count), cancellationToken);
        }
        var bytes = buffer.ToArray();
        string contents;
        try { contents = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException ex) { throw new InvalidDataException("Save the CSV as UTF-8 before uploading.", ex); }
        var preview = reader.Read(contents, asOfDate, delimiter);
        return await imports.StageAsync(fileName, Convert.ToHexString(SHA256.HashData(bytes)), asOfDate, importedBy, preview, cancellationToken);
    }
}
