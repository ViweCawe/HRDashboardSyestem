# Employee CSV contract

Initial import support is **CSV only**, UTF-8, comma or semicolon delimiter, at most 5 MB and 20,000 employee rows. Select the delimiter and source as-of date explicitly. The date is the source's reporting date, not a date inferred from the upload time or filename. CSV can be exported from the HR workbook without manually splitting it into database tables.

The first row must contain column headings. Quoted delimiters and additional source columns are supported. Extra columns are retained in raw JSON but do not automatically become domain fields. A supplied value exceeding its schema limit is flagged rather than truncated.

| Column | Handling |
| --- | --- |
| Employee Code, First Name, Last Name, Date Engaged, Company, Employee Status | Required headings and required values. |
| ID Number | Person match identifier. Required unless a passport plus issuing country is supplied. N/A means this field is unused. |
| Passport No, Passport Country Code | Optional alternative identity; two-letter country required whenever a passport is supplied. N/A in the unused passport/country field is normalized to null; country may need to be added for real passport-only records. |
| Display Name, Date of Birth, Entity Code | Optional; missing DOB/type/entity values are not invented. |
| Job Title, Primary Position | Parsed as source code and label. |
| COST_CENTRE - Cost Centre, DEPARTMENT - Department | Parsed as source code and label. |
| Company Rule, Remuneration Definition Header | Stored separately from employment type. |
| Employment Type | Optional explicit source value. Until HR approves a rule mapping, no type is inferred from Company Rule. |
| Termination Date, Termination Reason | For an active employee, the source end date is stored as PlannedEndDate. For a terminated employee, it remains TerminationDate. The Sage status is preserved. |
| Reports To Employee | Preserve source code; resolve the manager engagement by company and date only if unambiguous. |
| Gender, Racial Group, Disability | Snapshot attributes, when supplied. |
| Transfer Date or Transfare Date | Optional effective date for a department/cost-centre transfer. |

Code/label cells use `CODE - Description`; a standalone code is accepted as both code and provisional label. Dates accept explicit day/month/year (`dd/MM/yyyy` or `d/M/yyyy`) or year-first `yyyy/MM/dd` and ISO `yyyy-MM-dd`. For example, `2026/07/01` means 1 July 2026. US month/day dates are not guessed. National IDs and employee codes are text, preserving leading zeroes.

The export can contain active and terminated engagements together. Different leave codes or hours categories belong to the later, separate import contracts; do not upload those files on the employee screen.

`examples/employees.csv` uses invented names and non-production test identity values. It demonstrates one person returning under a new employee code, and an old code subsequently allocated to someone else. Replace only with approved synthetic identifiers when testing more specific national-ID validation later.

Known placeholder IDs (for example UNKNOWN or all-zero IDs) require review. N/A is treated as an unused field; another valid identity is still required. The first slice does not validate national-ID checksums or obtain DOB from an ID; identity corrections and country-specific validation need an agreed HR contract. The review screen displays the first 200 issues while all issues remain stored and block publication.

Re-uploading the same file and as-of date returns the same batch. An unpublished
Review batch is revalidated when the parser produces changed results: the original
source rows and staging IDs are retained, earlier issues are kept with a resolution
timestamp, and the review page shows current issues only. Published and already
validated Staged batches are returned unchanged. This does not replace published
snapshots or provide a same-date source correction workflow.

HR-confirmed end-date rules: a planned end does not automatically terminate an
employee, release a recycled code, infer employment type, or change payroll. An
active source record remains active even if its planned date has passed; follow-up
with HR/Sage is needed to establish the actual status. Planned and actual end dates
are stored separately on engagements and snapshots and shown in employee history.
Changes to either date remain detected changes, with the original source preserved.

Only literal N/A (case-insensitive, ignoring surrounding spaces) is treated as an
unused identity value. If both identity fields are blank/N/A, publication remains
blocked. A real passport still needs a two-letter issuing country. Other placeholder
IDs continue to require review. Original N/A text is retained in source JSON.
