#!/usr/bin/env bash
set -euo pipefail

hr_script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
hr_repo_root="$(cd -- "$hr_script_dir/.." && pwd)"
cd "$hr_repo_root"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "Install the .NET 10 SDK and reopen Git Bash before running this check." >&2
    exit 1
fi

dotnet restore tests/HrDashboard.Tests/HrDashboard.Tests.csproj --configfile NuGet.Config
dotnet build HrDashboard/HrDashboard.csproj --no-restore
dotnet test tests/HrDashboard.Tests/HrDashboard.Tests.csproj --no-restore
dotnet build tests/DatabaseValidation/DatabaseValidation.sqlproj
dotnet build tests/HrDashboard.DatabaseChecks/HrDashboard.DatabaseChecks.csproj

echo "Phase 1 build, automated tests and SQL model compilation passed."
echo "Live SQL Server behaviour checks are a separate step; see docs/database-next-steps.md."
