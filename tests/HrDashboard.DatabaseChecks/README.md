# Opt-in database behaviour checks

These checks perform writes and deliberately leave synthetic rows for inspection. They refuse to run unless the database name starts with `HrDashboardTests_`. Use a fresh, isolated SQL Server 2025 / LocalDB database; never point them at HR development data, Identity or production.

1. Publish DashboardDatabase (or the portable DatabaseValidation build) to a new database such as `HrDashboardTests_Local`.
2. In PowerShell, set the test connection and run:

   ```powershell
   $env:HR_TEST_CONNECTION = 'Server=(localdb)\mssqllocaldb;Database=HrDashboardTests_Local;Trusted_Connection=True;TrustServerCertificate=True'
   dotnet run --project tests/HrDashboard.DatabaseChecks
   ```

3. Check the PASS output and inspect any failure. Drop this disposable database when finished.

Scenarios cover re-employment, recycled codes, import idempotency, rollback on conflicting identities/engagements, baseline and later snapshots, readable history, no inferred promotion, as-of queries, same-date correction rejection, historical-backfill rejection and validation errors blocking publication.

The harness uses the real Dapper repositories and stored procedures. A successful C# or SQL model build does not replace running these checks against a live SQL Server instance.
