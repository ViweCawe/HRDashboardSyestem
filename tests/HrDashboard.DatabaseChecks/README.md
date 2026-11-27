# Opt-in database behaviour checks

These checks perform writes and deliberately leave synthetic rows for inspection. They refuse to run unless the database name starts with `HrDashboardTests_`. Use a fresh, isolated SQL Server 2025 / LocalDB database; never point them at HR development data, Identity or production.

1. Publish DashboardDatabase (or the portable DatabaseValidation build) to a new database such as `HrDashboardTests_Local`.
2. In **Git Bash**, set the test connection and run:

   ```bash
   export HR_TEST_CONNECTION='Server=(localdb)\mssqllocaldb;Database=HrDashboardTests_Local;Trusted_Connection=True;TrustServerCertificate=True'
   dotnet run --project tests/HrDashboard.DatabaseChecks
   ```

   For a full SQL Server instance, use its actual server name instead of LocalDB.
   In PowerShell, set the same value with `$env:HR_TEST_CONNECTION = '...'` instead.

3. Check the PASS output and inspect any failure. Drop this disposable database when finished.

Scenarios cover re-employment, recycled codes, import idempotency, rollback on conflicting identities/engagements, baseline and later snapshots, readable history, no inferred promotion, as-of queries, same-date correction rejection, historical-backfill rejection and validation errors blocking publication.

The harness uses the real Dapper repositories and stored procedures. A successful C# or SQL model build does not replace running these checks against a live SQL Server instance.

Rejection checks verify the expected rule message as well as SQL error 50001. Historical-query checks require the expected nonempty engagement set; empty results cannot produce a pass. Fixture names and identifiers are unique per run, and rejected corrections are checked for rollback of both personal details and snapshots.
