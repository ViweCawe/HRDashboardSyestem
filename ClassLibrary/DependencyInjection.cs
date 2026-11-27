using ClassLibrary.DataAccess;
using ClassLibrary.ImportAndStaging.Data;
using ClassLibrary.ImportAndStaging.Services;
using ClassLibrary.PeoplAndEmployment.Data;
using Microsoft.Extensions.DependencyInjection;

namespace ClassLibrary;

public static class DependencyInjection
{
    public static IServiceCollection AddHrData(this IServiceCollection services, string? connectionString)
    {
        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory(connectionString));
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeImportRepository, EmployeeImportRepository>();
        services.AddSingleton<EmployeeCsvReader>();
        services.AddScoped<EmployeeImportService>();
        return services;
    }
}
