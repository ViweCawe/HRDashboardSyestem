using ClassLibrary.DataAccess;
using HrDashboard.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HrDashboard.Tests;

public sealed class HrErrorReporterTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task MissingConnectionIsIdentifiedBeforeOpeningSql(string? connectionString)
    {
        await Assert.ThrowsAsync<HrConnectionConfigurationException>(() => new SqlConnectionFactory(connectionString).OpenAsync());
    }

    [Fact]
    public async Task MalformedConnectionIsHandledAsConfigurationInsteadOfAnUnhandledArgumentError()
    {
        var exception = await Assert.ThrowsAsync<HrConnectionConfigurationException>(
            () => new SqlConnectionFactory("Not a connection string").OpenAsync());
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public void ConfigurationFailureIsSafeAndCanBeCorrelatedWithItsServerLog(string environment, bool showLocalSetup)
    {
        var logger = new CaptureLogger();
        var reporter = new HrErrorReporter(logger, new TestHrEnvironment { EnvironmentName = environment });
        var exception = new HrConnectionConfigurationException("Password=private-credential; employee=private-person");
        var message = reporter.Report(exception, "Load recent imports");

        Assert.Contains("Load recent imports", message);
        Assert.Contains("HR-CONNECTION-CONFIG", message);
        Assert.DoesNotContain("private-credential", message);
        Assert.DoesNotContain("private-person", message);
        Assert.Equal(showLocalSetup, message.Contains("bash scripts/setup-hr-database.sh"));
        var reference = message.Split("Reference: ")[1].TrimEnd('.');
        Assert.Matches("^[a-f0-9]{12}$", reference);
        Assert.Contains(reference, logger.Message);
        Assert.Same(exception, logger.Exception);
    }

    [Fact]
    public void UnknownFailureHasAReferenceWithoutGuessingItsCauseOrExposingItsMessage()
    {
        var reporter = new HrErrorReporter(new CaptureLogger(), new TestHrEnvironment());
        var message = reporter.Report(new InvalidOperationException("Sensitive source contents"), "Validate employee file");
        Assert.Contains("HR-REQUEST", message);
        Assert.Contains("Reference:", message);
        Assert.DoesNotContain("Sensitive source contents", message);
        Assert.DoesNotContain("bash scripts/setup-hr-database.sh", message);
    }

    private sealed class CaptureLogger : ILogger<HrErrorReporter>
    {
        public string Message { get; private set; } = "";
        public Exception? Exception { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Message = formatter(state, exception);
            Exception = exception;
        }
    }
}

internal sealed class TestHrEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "HrDashboard.Tests";
    public string ContentRootPath { get; set; } = "/";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
