# HR Dashboard

Internal HR reporting built with the existing Blazor Interactive Server application, SQL Database Project and .NET class library.

## Implemented in this first slice

- Phase 1 SQL tables and matching C# models for people, employment, dated employee snapshots, detected changes, hours, leave and import lineage.
- Employee CSV validation, staging, issue review and transactional publication using Dapper and stored procedures.
- Employee search by name/code and snapshot as-of date, engagement overview, snapshot history and readable changes.
- Reappointments link a new employee code and EmploymentId to the existing PersonId. A recycled code cannot merge different people.
- Exact file/date reimports reuse the original batch; republishing it is a no-op.
- HRReader/HRAdmin access policies. Registration alone grants no HR access.

Hours and leave ingestion, Excel workbook parsing, dashboard charts and the remaining employee-profile modules are **not implemented yet**. Their models and SQL tables are included so subsequent features share the same foundation.

## Run locally

1. Open `HrDashboardApp.slnx` in Visual Studio 2026 with .NET 10 and SQL Server Data Tools installed. The existing SQL project targets SQL Server 2025 (Sql170); publish to a compatible development instance. Review the publish script before deploying to an existing database.
2. Build and publish `DashboardDatabase` to an HR database, for example `HrDashboardDev`. Do not let EF migrations manage the `hr` schema.
3. Configure the connection strings with user secrets from the repository root:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:HrConnection" "Server=(localdb)\mssqllocaldb;Database=HrDashboardDev;Trusted_Connection=True;TrustServerCertificate=True" --project HrDashboard
   ```

   The template's `DefaultConnection` remains the separate Identity database. For a full SQL Server instance replace the server name accordingly.
4. Apply Identity migrations: `dotnet ef database update --project HrDashboard` (install a compatible EF CLI tool if needed). Register and confirm a development account using the template's confirmation flow.
5. In the **Identity database**, review and run `scripts/Grant-HrRole.sql` with that existing user's ID and either HRAdmin or HRReader. No first-user administrator is assigned automatically.
6. Run the `HrDashboard` project. HRAdmin sees **Employee imports**. Upload a UTF-8 CSV with the correct export as-of date, review issues, then publish. Use **Employees** to view engagements and history.

Use synthetic records in this public repository. Keep real employee exports and connection credentials outside source control.

## Validation

```powershell
dotnet build HrDashboard/HrDashboard.csproj
dotnet test tests/HrDashboard.Tests/HrDashboard.Tests.csproj
```

Build `DashboardDatabase` with SSDT in Visual Studio. A portable DacFx model build can also validate the exact SQL sources without converting the original SSDT project:

```powershell
dotnet build tests/DatabaseValidation/DatabaseValidation.sqlproj -p:UseTaskHostFactory=false
```

Database behaviour checks require a separately published, disposable SQL database. See `tests/HrDashboard.DatabaseChecks/README.md`. Successful model compilation does not establish runtime import behaviour.

Read [architecture](docs/architecture.md), [database rules](docs/database.md), and [the employee CSV contract](docs/employee-import.md) before extending the implementation.
