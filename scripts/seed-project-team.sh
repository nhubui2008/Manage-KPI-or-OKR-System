#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
env_file=${1:-"$repo_root/.env"}
sql_container=${SQLCMD_CONTAINER:-sqlserver2025}
seed_file="$repo_root/scripts/seed-project-team.sql"

if [[ ! -f "$env_file" ]]; then
    echo "Environment file not found: $env_file" >&2
    exit 1
fi

connection=$(sed -n 's/^ConnectionStrings__DefaultConnection=//p' "$env_file" | head -1)
if [[ -z "$connection" ]]; then
    echo "ConnectionStrings__DefaultConnection is missing in $env_file" >&2
    exit 1
fi

db_server=''
db_name=''
db_user=''
db_password=''
IFS=';' read -r -a connection_parts <<< "$connection"
for connection_part in "${connection_parts[@]}"; do
    connection_key=${connection_part%%=*}
    connection_value=${connection_part#*=}
    connection_key=$(printf '%s' "$connection_key" | tr '[:upper:]' '[:lower:]' | tr -d ' ')
    case "$connection_key" in
        server|datasource) db_server=$connection_value ;;
        database|initialcatalog) db_name=$connection_value ;;
        userid|uid) db_user=$connection_value ;;
        password|pwd) db_password=$connection_value ;;
    esac
done

if [[ -z "$db_server" || -z "$db_name" || -z "$db_user" || -z "$db_password" ]]; then
    echo "The configured SQL connection must include server, database, user id, and password." >&2
    exit 1
fi

if ! docker inspect "$sql_container" >/dev/null 2>&1; then
    echo "SQL command container '$sql_container' is not available." >&2
    exit 1
fi

echo "Seeding project team into server=$db_server database=$db_name user=$db_user password=<redacted>"
docker exec -i \
    -e SQLCMDPASSWORD="$db_password" \
    "$sql_container" \
    /opt/mssql-tools18/bin/sqlcmd \
    -C -b -l 15 -S "$db_server" -U "$db_user" -d "$db_name" -i /dev/stdin \
    < "$seed_file"

echo "Project team seed completed."
echo "Demo password for newly created accounts: NextGen@2026"
