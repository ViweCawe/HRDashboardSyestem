#!/usr/bin/env bash
set -euo pipefail

hr_script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
hr_repo_root="$(cd -- "$hr_script_dir/.." && pwd)"
cd "$hr_repo_root"

# Fixed local-only targets: never accept a production server or database here.
hr_instance="HRDashboard2025"
hr_test_database="HrDashboardTests_Local"
hr_dev_database="HrDashboardDev"
hr_dacpac="tests/DatabaseValidation/bin/Debug/DatabaseValidation.dacpac"
hr_artifacts="artifacts/database"

for hr_command in dotnet SqlLocalDB.exe; do
    if ! command -v "$hr_command" >/dev/null 2>&1; then
        echo "Required command not found: $hr_command. Run this from Git Bash with .NET 10 and SQL Server 2025 LocalDB installed." >&2
        exit 1
    fi
done

if SqlLocalDB.exe info "$hr_instance" >/dev/null 2>&1; then
    hr_instance_info="$(SqlLocalDB.exe info "$hr_instance")"
    if ! printf '%s\n' "$hr_instance_info" | tr -d '\r' | grep -Eq '^[[:space:]]*Version:[[:space:]]+17\.'; then
        echo "$hr_instance already exists but was not confirmed as version 17.x. Inspect it before proceeding; it will not be upgraded or deleted." >&2
        exit 1
    fi
else
    SqlLocalDB.exe create "$hr_instance" 17.0
fi
SqlLocalDB.exe start "$hr_instance"
SqlLocalDB.exe info "$hr_instance"

hr_test_connection="Server=(localdb)\\$hr_instance;Database=$hr_test_database;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
hr_dev_connection="Server=(localdb)\\$hr_instance;Database=$hr_dev_database;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"

# Keep Windows Git Bash from rewriting SqlPackage's /Action and /SourceFile options.
hr_sqlpackage() {
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' dotnet tool run sqlpackage -- "$@"
}

dotnet tool restore --configfile NuGet.Config
dotnet build tests/DatabaseValidation/DatabaseValidation.sqlproj -m:1
dotnet build tests/HrDashboard.DatabaseChecks/HrDashboard.DatabaseChecks.csproj -m:1
if [[ ! -f "$hr_dacpac" ]]; then
    echo "The database build did not produce $hr_dacpac." >&2
    exit 1
fi
mkdir -p "$hr_artifacts"

hr_publish() {
    local hr_connection="$1"
    local hr_script_name="$2"
    hr_sqlpackage /Action:Script "/SourceFile:$hr_dacpac" "/TargetConnectionString:$hr_connection" "/OutputPath:$hr_artifacts/$hr_script_name" "/p:BlockOnPossibleDataLoss=True" "/p:DropObjectsNotInSource=False"
    hr_sqlpackage /Action:Publish "/SourceFile:$hr_dacpac" "/TargetConnectionString:$hr_connection" "/p:BlockOnPossibleDataLoss=True" "/p:DropObjectsNotInSource=False"
}

# Only synthetic fixtures are written to the disposable database.
hr_publish "$hr_test_connection" "test-deployment.sql"
HR_TEST_CONNECTION="$hr_test_connection" dotnet run --project tests/HrDashboard.DatabaseChecks --no-build

# A failed SQL behaviour check exits above; it cannot reach development publication.
hr_publish "$hr_dev_connection" "dev-deployment.sql"
dotnet user-secrets set 'ConnectionStrings:HrConnection' "$hr_dev_connection" --project HrDashboard

echo "Database behaviour checks passed."
echo "Development HR database: (localdb)\\$hr_instance / $hr_dev_database"
echo "HrConnection is configured in this project's user secrets."
echo "DefaultConnection, existing LocalDB instances and Identity data were not changed."
echo "Deployment scripts: $hr_artifacts. Test fixtures remain only in $hr_test_database."
