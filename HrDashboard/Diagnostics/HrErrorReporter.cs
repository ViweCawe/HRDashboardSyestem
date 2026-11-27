using System.Globalization;
using ClassLibrary.DataAccess;
using Microsoft.Data.SqlClient;

namespace HrDashboard.Diagnostics;

/// <summary>Shows actionable categories and a log reference, never raw database exception text.</summary>
public sealed class HrErrorReporter(ILogger<HrErrorReporter> logger, IHostEnvironment environment)
{
    public string Report(Exception exception, string operation)
    {
        var reference = Guid.NewGuid().ToString("N")[..12];
        var sqlNumber = (exception as SqlException)?.Number;
        logger.LogError(exception, "HR request failed during {Operation}. Reference {ErrorReference}; SQL error {SqlErrorNumber}",
            operation, reference, sqlNumber);

        var code = exception is HrConnectionConfigurationException ? "HR-CONNECTION-CONFIG"
            : sqlNumber.HasValue ? $"HR-SQL-{sqlNumber.Value.ToString(CultureInfo.InvariantCulture)}"
            : exception is IOException ? "HR-FILE-READ" : "HR-REQUEST";
        var explanation = exception is HrConnectionConfigurationException
            ? "The HR database connection is missing or incorrectly configured."
            : sqlNumber switch
            {
                2812 or 208 or 207 => "The connected database is missing an HR procedure, table or column. Its schema may be out of date, or the app may be connected to the wrong database.",
                4060 or 4064 or 18456 => "The application cannot open the HR database or sign in to SQL Server. Check database availability and access permissions.",
                2 or 53 or 10060 or 11001 => "The application cannot reach the SQL Server instance. Check that the configured instance is available.",
                -2 => "The HR database request timed out. Check SQL Server availability before retrying.",
                229 => "The application's database account does not have permission to complete this request.",
                _ when exception is IOException => "The selected file could not be read. Select the file again and retry validation.",
                _ => "The request could not be completed. Share the error code and reference with your administrator."
            };

        var setupHint = environment.IsDevelopment()
            && (exception is HrConnectionConfigurationException || sqlNumber is 2812 or 208 or 207 or 4060 or 4064)
            ? " Local setup: stop the app, run bash scripts/setup-hr-database.sh from the repository root, then restart it."
            : string.Empty;
        return $"{operation}: {explanation}{setupHint} Error code: {code}. Reference: {reference}.";
    }
}
