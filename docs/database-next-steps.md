# Database project: next steps after Phase 1 verification

## Verification boundary

The application build and automated tests establish CSV-contract validation,
import-service boundaries and HTTP route access policies. The portable SQL build
checks SQL syntax and model relationships. The original SSDT project is preserved.
Neither compilation nor mocked HTTP tests establish stored-procedure behaviour
against a running database.

Run the repeatable checks from Git Bash in the repository:

```bash
bash scripts/verify-phase-one.sh
```

The separate database-check console must be run against a disposable database
before considering the import workflow database-verified. It is not part of
`dotnet test` and an unconfigured database is never counted as a passing SQL test.
There is no GitHub Actions workflow yet; the documented checks are local checks.

## 1. Identify the development SQL instance and version

If using LocalDB, run these **read-only** commands in Git Bash:

```bash
SqlLocalDB.exe versions
SqlLocalDB.exe info
SqlLocalDB.exe info MSSQLLocalDB
```

If the utility or named instance is unavailable, use the server/instance you
normally connect to in SSMS or Visual Studio instead. Obtain its SQL Server
version with `SELECT @@VERSION;` in an existing query connection.

The current SQL project targets Sql170 (SQL Server 2025). Align that target with
the confirmed engine before publishing. Visual Studio 2026 itself does not prove
which SQL Server engine or LocalDB version is installed. Do not enable an
incompatible-platform override to avoid doing that check.

## 2. Build and review the database model

The hr schema already contains 24 tables, primary/foreign keys, uniqueness rules,
filtered indexes and the employee import/query stored procedures. Review those
objects in DashboardDatabase before changing the schema. Start with Person,
Employment, EmployeeSnapshot and their import lineage; then hours and leave.

Build DashboardDatabase in Visual Studio with SSDT, or build the same sources
through `tests/DatabaseValidation/DatabaseValidation.sqlproj`. Review the generated
publish script against the selected target. Use a new disposable database named
`HrDashboardTests_<suffix>` for the first publication; keep Identity and real HR
records separate.

## 3. Publish the disposable database and run the behaviour checks

Follow `tests/HrDashboard.DatabaseChecks/README.md`. Required results include:

- One PersonId with separate EmploymentIds and new employee codes after reappointment.
- A recycled code never merges different people or overlaps employment intervals.
- An exact file/date reimport returns the original batch and cannot duplicate facts.
- A later snapshot preserves the earlier history and reports the correct as-of job title.
- A detected job-title change remains unconfirmed rather than automatically becoming a promotion.
- Failed publication rolls back changes, and source validation errors block publication.
- Same-date replacement and historical backfill remain blocked until reviewed workflows exist.

## 4. Connect the application to the development HR database

Only after the disposable checks pass, publish to the agreed development database,
set ConnectionStrings:HrConnection with user secrets, and test a synthetic CSV via
the HRAdmin import page. Existing DefaultConnection and Identity migrations remain
separate. Review database permissions and the deployment diff before using actual
HR exports.

The immediate information needed to proceed is the development SQL instance name
and engine version. No deployment has been performed by these verification steps.
