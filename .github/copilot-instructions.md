# HR Dashboard repository instructions

- Preserve the existing three projects and feature folders. Read docs/architecture.md and the relevant import/database contract before changing behaviour.
- Use Dapper and named SQL stored procedures for HR data. Existing EF Core is for Identity only.
- Keep business rules and import orchestration in ClassLibrary, not Razor components. Use feature-specific interfaces and constructor injection.
- PersonId identifies a person; EmploymentId identifies an engagement. Re-employment gets a new EmployeeCode. Employee codes may also be recycled for a different person and are never person primary keys.
- Never match people by names alone, fabricate change dates, infer promotion from job-title change, or discard changed existing rows as duplicates.
- Keep source snapshots, raw staging data, validation issues and batch lineage. An exact file/date reimport must not duplicate data.
- Do not infer employment type, hours format, leave units or Taken basis without the agreed source mapping.
- Authorise HR pages by the existing policies. Never commit actual HR records, IDs, credentials or exports.
- Build HrDashboard, run the tests and compile SQL after relevant changes. Database behaviour checks require an explicitly configured disposable database.
- Update docs to distinguish implemented behaviour from planned work. Report checks actually run and any remaining validation limits.
