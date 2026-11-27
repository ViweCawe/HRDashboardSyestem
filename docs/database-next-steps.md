# Database project: SQL Server 2025 LocalDB setup

The development machine has SQL Server 2019 and 2025 LocalDB binaries installed,
but its automatic `MSSQLLocalDB` instance still uses SQL Server 2019. The project
already targets Sql170 (SQL Server 2025). Keep that target and use a separate named
instance, **HRDashboard2025**, created with version **17.0**.

## Git Bash setup

From the repository root:

```bash
git pull --ff-only
bash scripts/setup-hr-database.sh
```

The script:

1. Creates HRDashboard2025 with the installed 17.0 engine if it does not exist. If
   an instance with that name exists, it verifies version 17.x before continuing.
   Existing instances are not upgraded, stopped or deleted.
2. Starts the named instance and restores the pinned local SqlPackage tool.
3. Builds the same SQL sources through the portable validation project and builds
   the real database-check console. The original SSDT project is preserved.
4. Saves a deployment script and publishes the schema to HrDashboardTests_Local.
5. Runs the real Dapper/stored-procedure checks with synthetic records. A failure
   stops the script before development publication or connection configuration.
6. Saves another deployment script and publishes the schema to HrDashboardDev.
7. Sets ConnectionStrings:HrConnection in this application's local user secrets.

The server and database names are fixed to local development/test targets. Possible
data-loss blocking stays enabled, and objects outside the project are not dropped.
Deployment SQL files are written to ignored `artifacts/database/`. Test data remains
in HrDashboardTests_Local for inspection; the script does not copy it to development.
Do not repurpose these fixed-name databases for real HR records.

## Manual equivalent

Create and start the new instance once:

```bash
SqlLocalDB.exe create HRDashboard2025 17.0
SqlLocalDB.exe start HRDashboard2025
SqlLocalDB.exe info HRDashboard2025
```

An existing HRDashboard2025 instance can be started and inspected without running
create again. The reported version must start with 17.

Build the database sources:

```bash
dotnet tool restore --configfile NuGet.Config
dotnet build tests/DatabaseValidation/DatabaseValidation.sqlproj
```

The package is `tests/DatabaseValidation/bin/Debug/DatabaseValidation.dacpac`.
Publish that package or the original SSDT project's output to the named SQL 2025
instance. Run the disposable checks as described in
`tests/HrDashboard.DatabaseChecks/README.md` before publishing development.
The setup script includes SqlPackage invocations with Git Bash argument conversion
explicitly disabled so `/Action:Publish` is passed correctly on Windows.

## Connect the application

The HR connection after successful setup is:

```text
Server=(localdb)\HRDashboard2025;Database=HrDashboardDev;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True
```

DefaultConnection and Identity remain separate. Apply the existing Identity
migrations to its configured database if needed, create/confirm an account, then
use `scripts/Grant-HrRole.sql` in the **Identity database** to grant HRAdmin or
HRReader. The setup script does not grant privileges to an arbitrary account.

Start the application and use the HRAdmin employee-import screen with the synthetic
CSV example. Preserve source snapshot dates. Hours and leave ingestion are later slices.

## Verification boundary

`bash scripts/verify-phase-one.sh` runs the application tests and SQL model build.
Those checks cover contracts, service boundaries, route access, SQL syntax and
model relationships. They do not execute SQL Server.

The setup script's database-check console must print its PASS results and finish
successfully before the database workflow is treated as runtime-verified. Compilation,
shell-script tests and the existence of the setup script are not proof of live SQL
behaviour. No database on the user's computer is created until the user runs setup.
If setup fails, keep its output and fix that specific failure before proceeding.

## Instance-detection diagnostics

The setup lists instance names with `SqlLocalDB.exe info`, then inspects the named
instance. It does not treat a successful `info <name>` exit code alone as proof
that the instance exists. Version output is captured from both stdout and stderr.
If inspection fails or the engine cannot be confirmed as 17.x, the actual output
is displayed and setup stops before publication. An existing instance is never
deleted or automatically upgraded to bypass a version mismatch.
