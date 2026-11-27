# Employee CSV contract

Initial import support is **CSV only**, UTF-8, comma or semicolon delimiter, at most 5 MB and 20,000 employee rows. Select the delimiter and source as-of date explicitly. The date is the source's reporting date, not a date inferred from the upload time or filename. CSV can be exported from the HR workbook without manually splitting it into database tables.

The first row must contain column headings. Quoted delimiters and additional source columns are supported. Extra columns are retained in raw JSON but do not automatically become domain fields. A supplied value exceeding its schema limit is flagged rather than truncated.

| Column | Handling |
| --- | --- |
| Employee Code, First Name, Last Name, Date Engaged, Company, Employee Status | Required headings and required values. |
| ID Number | Person match identifier. Required unless a passport plus issuing country is supplied. |
| Passport No, Passport Country Code | Optional alternative identity; two-letter country required whenever a passport is supplied. Country may need to be added to the export contract. |
| Display Name, Date of Birth, Entity Code | Optional; missing DOB/type/entity values are not invented. |
| Job Title, Primary Position | Parsed as source code and label. |
| COST_CENTRE - Cost Centre, DEPARTMENT - Department | Parsed as source code and label. |
| Company Rule, Remuneration Definition Header | Stored separately from employment type. |
| Employment Type | Optional explicit source value. Until HR approves a rule mapping, no type is inferred from Company Rule. |
| Termination Date, Termination Reason | Optional, but contradictory active-status termination details require review. |
| Reports To Employee | Preserve source code; resolve the manager engagement by company and date only if unambiguous. |
| Gender, Racial Group, Disability | Snapshot attributes, when supplied. |
| Transfer Date or Transfare Date | Optional effective date for a department/cost-centre transfer. |

Code/label cells use `CODE - Description`; a standalone code is accepted as both code and provisional label. Dates accept explicit day/month/year (`dd/MM/yyyy` or `d/M/yyyy`) or ISO `yyyy-MM-dd`. US month/day dates are not guessed. National IDs and employee codes are text, preserving leading zeroes.

The export can contain active and terminated engagements together. Different leave codes or hours categories belong to the later, separate import contracts; do not upload those files on the employee screen.

`examples/employees.csv` uses invented names and non-production test identity values. It demonstrates one person returning under a new employee code, and an old code subsequently allocated to someone else. Replace only with approved synthetic identifiers when testing more specific national-ID validation later.

Known placeholder IDs (for example UNKNOWN or N/A) require review. The first slice does not validate national-ID checksums or obtain DOB from an ID; identity corrections and country-specific validation need an agreed HR contract. The review screen displays the first 200 issues while all issues remain stored and block publication.
