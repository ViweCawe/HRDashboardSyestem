# Test employee imports through the Blazor app

## Update and start

Run in Git Bash from the repository root:

```bash
git pull --ff-only
bash scripts/setup-hr-database.sh
bash scripts/setup-hr-access.sh
dotnet run --project HrDashboard
```

The database setup adds the batch-review and overview query procedures, checks them
in the disposable database, then updates development. Run it after this UI update
even if the previous database setup already succeeded.

The access setup restores the pinned EF CLI tool and applies the existing Identity
migrations to DefaultConnection. It prompts locally for an email and password,
then creates or grants HRAdmin to that development account. An existing account
requires its current password. Credentials are not committed, printed or placed
in command-line arguments; the password is passed only to the child setup process.
This explicit account command works only in Development and is not an HTTP endpoint.
It confirms the chosen development account so testing does not require an email server.

Open the address printed by dotnet, normally `http://localhost:5175`. Sign in with
the account you just configured. If you were already signed in, sign out and back
in so your role claims refresh. Ordinary registration alone grants no HR access.

## Pages

| Page | Route | Access |
| --- | --- | --- |
| Workspace overview | `/` | Public welcome; HR metrics only for HRReader/HRAdmin. |
| Sign in / create account | `/Account/Login`, `/Account/Register` | Public account forms; registration still needs confirmation and role provisioning. |
| Import centre | `/imports/employees` | HRAdmin. |
| Batch review and preview | `/imports/employees/{batchId}` | HRAdmin. |
| Employee directory | `/employees` | HRReader or HRAdmin. |
| Employee overview and history | `/employees/{employmentId}` | HRReader or HRAdmin. |

The layout is responsive, with reusable page headings, metrics, status badges,
messages and empty states. Import/employee services remain in ClassLibrary;
Razor pages coordinate the UI rather than containing SQL. The existing three
solution projects and feature-folder grouping are preserved.

## First successful import

1. Open **Import centre** and download **Example CSV** from the upload guidance.
   The same synthetic sample is in `examples/employees.csv` in the repository.
2. Select **31/07/2026** as the export as-of date, choose **Comma**, then choose the CSV.
3. Click **Validate file**. This stages the file and opens its batch-review page.
4. Check the record preview, source date and valid/invalid counts.
5. Click **Publish records**. Publication checks existing identities and history
   inside the SQL transaction before saving anything.
6. Click **View employees**. It opens the directory at the source date, so the new
   snapshot is visible even when it is not today's date.
7. Open Alex Example. The page links both engagements with their separate employee
   codes. Jordan Sample remains a different person despite the recycled code.

The sample contains three engagements for two people. Workspace **Employee records**
counts engagements, not active headcount. The overview reports actual database
records/import status, with no fabricated workforce metrics.

## Negative and history checks

- Repeat the same file and source date: it returns the existing batch and cannot
  create duplicate records.
- In a copied CSV, clear one ID with no passport alternative. Validate it: the row
  and issue appear in batch review, and publication is unavailable. Upload a
  corrected file instead of editing authoritative Sage facts in the dashboard.
- Copy the sample and change Alex's current job-title code/label to a new value.
  Import it at **31/08/2026**. Open employee history: both snapshots should remain,
  and the job-title change should be marked as awaiting HR confirmation rather
  than automatically classified as a promotion.
- Use a different file for an already-published snapshot date: the reviewed
  correction requirement should block replacement. Older exports are also blocked
  after newer snapshots until a backfill workflow exists.
- Preview filters and paging apply to source rows; issues display the first 200
  with their original row numbers. National IDs and passports stay out of the preview.

Hours and leave have a table/model foundation, but their import pages remain marked
**Coming next**. This test slice handles employee CSV files only; it does not parse
Excel workbooks or automatically classify promotions.

## If an import fails

The error banner identifies the failed step, a safe error code and a reference.
The same reference is logged with the underlying exception in the server console.
Share the code and reference when reporting a problem; do not upload the employee
export or share connection strings/passwords for diagnosis.

- `HR-CONNECTION-CONFIG`: the HR connection setting is missing or malformed.
- `HR-SQL-2812`, `HR-SQL-208`, `HR-SQL-207`: a required procedure, table or column
  could not be found in the connected database. Check the connection target and
  deploy the current schema.
- `HR-SQL-4060`, `HR-SQL-4064`, `HR-SQL-18456`: SQL Server could not open the
  requested/default database or authenticate the application account.
- `HR-SQL--2`: a connection or command timed out.
- Other `HR-SQL-*` codes retain the SQL error number for investigation.
- `HR-FILE-READ`: reselect the CSV and retry.
- `HR-REQUEST`: consult the console entry with the matching reference.

For local database setup errors, stop the app, run
`bash scripts/setup-hr-database.sh`, and restart it. This restores the HR connection
setting for this project and deploys the current procedures after the disposable
checks pass. Production banners omit local setup commands. Infrastructure exception messages,
connection strings and stack traces are never copied into these banners.

## Automated and local verification

The automated suite covers source validation, upload service boundaries, input-file
selection/validation, publish interaction, HTTP access policies, batch-review states,
untrusted text encoding and development-only administrator provisioning. SQL model
compilation and the disposable database harness cover the new query procedures.
Component/HTTP tests use synthetic repositories and are not a substitute for this
full browser test against your own LocalDB instance. Browser layout inspection was
not completed in the build workspace; confirm the desktop/mobile views during
this local test.
