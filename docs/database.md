# Phase 1 database rules

## Identity and engagements

`hr.Person.PersonId` is the person primary key. ID numbers and passports are text matching identifiers, with filtered uniqueness for populated values. A passport match includes issuing country. The importer requires one usable identifier, never merges people by name and rejects conflicting ID/passport matches.

`hr.Employment.EmploymentId` identifies one engagement. Re-employment receives a **new EmployeeCode**, linked to the existing PersonId. Hours, leave, snapshots and changes reference EmploymentId. Company + employee code + engagement date is an alternate engagement key, not the person key.

An old code may be allocated to another person only after the previous engagement has a non-overlapping termination date. A changed engagement date on an existing code/person is held for review rather than interpreted as re-employment. Entity Code remains optional; its stability has not been confirmed.

## Snapshots and changes

A snapshot has one row per EmploymentId + source SnapshotDate. It retains department, cost centre, job title, position, source status, company rule, remuneration definition, employment type (when supplied), reporting line and demographic values. Termination date and reason are additionally copied into each snapshot so historical list results use values known on that date.

The first import provides a baseline. Later source dates add snapshots, even when tracked values are unchanged. Differences create EmploymentChange records with old/new values, both snapshot references and source-batch lineage. Missing events between exports cannot be reconstructed.

A changed job title is **DetectedChange**, not automatically Promotion. A supplied valid transfer date can classify a department/cost-centre change as Transfer; otherwise the effective date remains unknown. The UI resolves reference IDs to friendly names. Unmatched/ambiguous manager codes remain as SourceReportsToCode with a null ManagerEmploymentId; manager resolution is scoped to company and snapshot date.

Person names and DOB are current personal attributes. Snapshot history preserves the listed employment/demographic values; it is not a complete audit of every person-field correction. DOB is nullable and age must be calculated at the report date when an age metric is implemented.

## Import publication

ImportBatch has a unique source type + as-of date + SHA-256 file hash. Staging retains the source row JSON and the normalized contract JSON. Invalid rows create ImportIssue records and block publication. Whole-file structural errors are rejected before database staging.

Publication uses a serializable SQL transaction, batch locks, indexed matching keys and XACT_ABORT. A conflict rolls back every person, engagement, lookup, snapshot and change in that publication. The staged batch remains available for review. An exact already-published batch is a no-op. Concurrent conflicting imports may deadlock; the transaction rolls back and the operator can retry.

The initial implementation intentionally rejects a different file that would overwrite an already-published engagement snapshot on the same date, and rejects older exports after newer snapshots. A reviewed correction/backfill workflow must precede enabling those actions. Missing employees never trigger automatic deletion or termination. Source corrections are made in Sage and re-exported, with the applicable correction workflow.

## Hours and leave foundation

HoursEntry stores integer MinutesQuantity, an optional WorkDate, a reporting period and a category. Filtered unique indexes separate dated rows from period totals. Confirm source format and quantity basis before ingestion; a value such as 6.62 is not valid H.MM. Do not infer worked time from premium-pay quantities.

LeaveBalance retains decimal(18,4) source precision, including values such as 5.2984. Its key is employment + leave type + period. UnitCode belongs to LeaveType; TakenBasis records Period/YTD/Cycle/Unknown. Do not sum balance snapshots or cumulative Taken values across periods. Monthly balance data cannot support dated leave-event reporting.

ReportingPeriod stores actual source boundaries and grain. ImportBatch.ReportingPeriodId is optional for employee snapshots. Hours/leave staging and fact tables have no ingestion procedures or UI yet.

## Security and deployment

Identity remains in the template's DefaultConnection database; HR uses the separate HrConnection database and `hr` schema. HRReader and HRAdmin can read HR pages; only HRAdmin can stage or publish imports. Database credentials must be limited to their intended environment. A database-level least-privilege execution role and production account provisioning remain deployment tasks.

No production migration or publication runs automatically at application startup. The SQL project includes the schema, all 24 tables, indexes and stored procedures explicitly.

Reference-code labels are current descriptions and may be corrected by later exports; they are not separately versioned. Employment history tracks the reference assignments, while the original source labels remain in each batch's raw rows. Code/label-only corrections do not automatically create promotions or transfers.
