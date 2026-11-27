# Packages and restore troubleshooting

Package references are already committed in each project. Run `dotnet restore`
to install their specified versions and transitive dependencies; do not add the
same packages manually to every project.

## Direct references

| Project | Package | Version | Purpose |
| --- | --- | --- | --- |
| ClassLibrary | Dapper | 2.1.100 | Map stored-procedure results to C# models. |
| ClassLibrary | Microsoft.Data.SqlClient | 7.1.2 | SQL Server connections. |
| ClassLibrary | Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.0 | Register the library's services and repositories. |
| HrDashboard | Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore | 10.0.0 | Existing development diagnostics for Identity's EF database. |
| HrDashboard | Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.15 | Existing Identity EF stores. |
| HrDashboard | Microsoft.EntityFrameworkCore.SqlServer | 10.0.15 | Existing Identity SQL Server provider. |
| HrDashboard | Microsoft.EntityFrameworkCore.Design | 10.0.15 | Identity migration design tooling. |
| HrDashboard | Microsoft.EntityFrameworkCore.Tools | 10.0.15 | Identity migration tooling in Visual Studio. |
| tests/HrDashboard.Tests | Microsoft.AspNetCore.Mvc.Testing | 10.0.15 | HTTP access-policy tests. |
| tests/HrDashboard.Tests | Microsoft.NET.Test.Sdk | 17.14.1 | Test host. |
| tests/HrDashboard.Tests | xunit | 2.9.3 | Test framework. |
| tests/HrDashboard.Tests | xunit.runner.visualstudio | 3.1.4 | Test discovery and execution. |

The optional portable SQL validation project resolves **Microsoft.Build.Sql
2.4.0** as a project SDK. The original DashboardDatabase project instead uses
SQL Server Data Tools installed with Visual Studio. The database-check console
inherits its dependencies through its ClassLibrary project reference.

`System.Threading.RateLimiting` and other transitive packages are installed by
restore automatically. They do not need extra direct package references to work
around package-source errors. The Blazor application also uses the ASP.NET Core
shared framework provided by the .NET 10 SDK/runtime.

## NU1100 with PackageSourceMapping

If NuGet says a package cannot be resolved and **all available sources were not
considered**, its source mapping is excluding that package. This is different
from a compiler error or proof that a package version does not exist.

The root `NuGet.Config` explicitly maps this repository's public packages and
their transitive dependencies to nuget.org. It resets inherited package sources,
disabled-source settings and source mappings for this repository only; it does
not modify the user's global configuration. If private packages are added later,
declare the private feed and its matching package patterns explicitly.

From Git Bash in the repository root:

```bash
git pull --ff-only
dotnet restore HrDashboard/HrDashboard.csproj --configfile NuGet.Config
dotnet restore tests/HrDashboard.Tests/HrDashboard.Tests.csproj --configfile NuGet.Config
dotnet build HrDashboard/HrDashboard.csproj --no-restore
dotnet test tests/HrDashboard.Tests/HrDashboard.Tests.csproj --no-restore
```

The explicit configuration selects the repository's settings for restore. Normal
Visual Studio and CLI restores also discover this file from the solution root.
Do not clear all global package caches or change package versions to fix an
unmatched source mapping. If restore still fails, inspect the new error before
choosing the next fix.
