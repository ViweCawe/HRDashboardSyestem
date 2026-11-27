#!/usr/bin/env bash
set -euo pipefail
hr_script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$(cd -- "$hr_script_dir/.." && pwd)"
if [[ ! -t 0 ]]; then
    echo "Run this interactively in Git Bash so you can choose local login credentials." >&2
    exit 1
fi
dotnet tool restore --configfile NuGet.Config
ASPNETCORE_ENVIRONMENT=Development dotnet tool run dotnet-ef -- database update --project HrDashboard
read -r -p "Local HRAdmin email: " hr_email
read -r -s -p "Password (existing password if this account already exists): " hr_password
printf '\n'
read -r -s -p "Confirm password: " hr_confirmation
printf '\n'
trap 'unset hr_password hr_confirmation HR_BOOTSTRAP_PASSWORD' EXIT
if [[ -z "$hr_email" || -z "$hr_password" || "$hr_password" != "$hr_confirmation" ]]; then
    echo "Supply an email and matching passwords. No administrator was created." >&2
    exit 1
fi
ASPNETCORE_ENVIRONMENT=Development HR_BOOTSTRAP_PASSWORD="$hr_password" dotnet run --project HrDashboard --no-launch-profile -- --bootstrap-hr-admin "$hr_email"
echo "Start the application with: dotnet run --project HrDashboard"
