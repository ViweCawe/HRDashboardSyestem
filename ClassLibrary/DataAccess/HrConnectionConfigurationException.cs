namespace ClassLibrary.DataAccess;

/// <summary>Identifies HR connection setup failures without exposing a connection string.</summary>
public sealed class HrConnectionConfigurationException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);
