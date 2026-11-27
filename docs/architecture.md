# Architecture and development conventions

## Projects and dependencies

- **HrDashboard**: Blazor pages, authentication, role policies, configuration and DI composition. Existing EF Core usage stays confined to ASP.NET Core Identity.
- **ClassLibrary**: domain/table models, import parsing and orchestration, repository interfaces and Dapper implementations. It does not reference Blazor or the web project.
- **DashboardDatabase**: declarative `hr` schema, tables, indexes and stored procedures. Application code calls named stored procedures rather than building SQL strings.
- **tests**: pure import-contract tests, a portable SQL model build, and opt-in checks against a disposable SQL database.

Dependencies are HrDashboard → ClassLibrary and database-check/test projects → ClassLibrary. The SQL project deploys independently. No additional API is required for this server-side UI.

## Class-library folders

Keep the user's existing feature grouping:

| Folder | Responsibility |
| --- | --- |
| PeoplAndEmployment | Person and engagement models, history queries and employee repositories. Existing folder spelling is retained. |
| ImportAndStaging | Staging/batch models, CSV parsing, validation, orchestration and import repositories. |
| HoursAndLeave | Period, hours, leave-balance and category models; ingestion follows later. |
| ReferenceData | Source-code lookup models. |
| DataAccess | SQL connection creation and its interface. |

Within a feature, use Models, Data and Services only when needed. Use one named table model per file. Avoid generic repository layers or additional solution projects until a concrete need appears. Open a short-lived SQL connection per repository operation; never share a live connection across Blazor circuits. Pass cancellation tokens through repository/service methods.

The original `HoursAndLeave` compile exclusion has been removed. The original SSDT project type and Sql170 target are preserved, with explicit SQL build items.

## Delivery approach

Implement small end-to-end slices. Keep changes on feature branches and inspect the SQL deployment diff before publishing. Build the web project, run contract tests and compile the SQL model. Run the database behaviour checks on an isolated database before accepting changes to identity matching, import publication or history.

## Next slices

1. Run the database behaviour checks and HR's real export contract against an authorised development database.
2. Add an explicit same-date correction/backfill workflow with audit and conflict review; the first slice blocks these actions.
3. Confirm permanent/contract/learnership mappings and add maintained reference mappings rather than guessing from company-rule text.
4. Implement hours import after confirming H.MM/decimal formatting and whether each category represents worked hours or paid premiums.
5. Implement leave-balance import after confirming units and the period/YTD/cycle meaning of Taken.
6. Add department/employment-type filters and charts, keeping report resolution within the source data's actual granularity.
7. Add the remaining profile/ER modules separately.

## SQL project visibility in Visual Studio

DashboardDatabase is an original-style SSDT project. Its project file lists each
SQL script with an explicit Build Include and lists the physical folders. Keep
those entries when adding tables or procedures so Solution Explorer displays them.
After pulling a project-file update, reload DashboardDatabase or close and reopen
HrDashboardApp.slnx from the same working folder. Tables, StoredProcedures and
Schemas should appear in the project. Reloading the project does not require
publishing or recreating the database.

## Blazor import workflow

The import centre selects a file and source date, then delegates validation and
staging to EmployeeImportService. A durable batch route queries its summary,
validation issues and paged preview via stored procedures. Publication is explicit
and remains protected by the HRAdmin policy and SQL transaction. The employee link
carries the batch's source date into the directory's as-of filter.

Shared Razor components and shared CSS keep the sidebar, headings, status badges,
metrics and empty/error states consistent. Identity's existing authentication
logic is retained. The optional administrator bootstrap is a local command gated
to Development, never a public route or automatic first-user grant.
