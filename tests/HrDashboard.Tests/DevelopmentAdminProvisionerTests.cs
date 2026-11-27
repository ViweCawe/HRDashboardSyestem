using HrDashboard.Data;
using HrDashboard.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace HrDashboard.Tests;

public sealed class DevelopmentAdminProvisionerTests
{
    private const string Email = "local-admin@example.test";
    private const string Password = "LocalTest!42";

    [Fact] public async Task CreatesAConfirmedAdministratorWithTheChosenPassword()
    {
        await using var provider = Services("Development");
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DevelopmentAdminProvisioner>().ProvisionAsync(Email, Password);
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await manager.FindByEmailAsync(Email);
        Assert.NotNull(user); Assert.True(user.EmailConfirmed);
        Assert.True(await manager.CheckPasswordAsync(user, Password));
        Assert.True(await manager.IsInRoleAsync(user, "HRAdmin"));
    }
    [Fact] public async Task RepeatedSetupKeepsTheSameAccountAndRole()
    {
        await using var provider = Services("Development");
        await using var scope = provider.CreateAsyncScope();
        var setup = scope.ServiceProvider.GetRequiredService<DevelopmentAdminProvisioner>();
        await setup.ProvisionAsync(Email, Password); await setup.ProvisionAsync(Email, Password);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.Users.CountAsync()); Assert.Equal(1, await db.UserRoles.CountAsync());
    }
    [Theory] [InlineData("Production")] [InlineData("Staging")]
    public async Task CannotProvisionOutsideDevelopment(string environment)
    {
        await using var provider = Services(environment);
        await using var scope = provider.CreateAsyncScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<DevelopmentAdminProvisioner>().ProvisionAsync(Email, Password));
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.ToListAsync());
    }
    [Fact] public async Task ExistingAccountRequiresItsActualPasswordBeforeGrantingAccess()
    {
        await using var provider = Services("Development");
        await using var scope = provider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = Email, Email = Email };
        Assert.True((await manager.CreateAsync(user, Password)).Succeeded);
        await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<DevelopmentAdminProvisioner>().ProvisionAsync(Email, "WrongPassword!52"));
        Assert.False(await manager.IsInRoleAsync(user, "HRAdmin")); Assert.False(user.EmailConfirmed);
    }
    [Fact] public async Task RejectsAnInvalidEmailBeforeCreatingAnAccount()
    {
        await using var provider = Services("Development");
        await using var scope = provider.CreateAsyncScope();
        await Assert.ThrowsAsync<ArgumentException>(() => scope.ServiceProvider.GetRequiredService<DevelopmentAdminProvisioner>().ProvisionAsync("not-an-email", Password));
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.ToListAsync());
    }
    private static ServiceProvider Services(string environment)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestEnvironment { EnvironmentName = environment });
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<DevelopmentAdminProvisioner>();
        return services.BuildServiceProvider();
    }
    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
