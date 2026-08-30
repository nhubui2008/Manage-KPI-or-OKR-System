#!/usr/bin/env bash
# ==============================================================================
# Script nạp dữ liệu mẫu seeddata.sql vào SQL Server trong Docker container
# ==============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

CONTAINER_NAME="${MSSQL_CONTAINER_NAME:-kpi-sqlserver}"
DB_NAME="${MSSQL_DATABASE:-manasys}"
SA_PASSWORD="${MSSQL_SA_PASSWORD:-YourStrong@Password123}"
SEED_FILE="${REPO_ROOT}/seeddata.sql"

if [ ! -f "${SEED_FILE}" ]; then
  echo "[-] Không tìm thấy tệp seeddata.sql tại: ${SEED_FILE}"
  exit 1
fi

echo "[*] Kiểm tra container SQL Server (${CONTAINER_NAME})..."

if ! docker ps --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
  echo "[-] Container ${CONTAINER_NAME} chưa chạy. Vui lòng chạy 'docker compose up -d' trước."
  exit 1
fi

echo "[*] Đang chờ SQL Server sẵn sàng..."
for i in {1..30}; do
  if docker exec "${CONTAINER_NAME}" /bin/bash -c "
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${SA_PASSWORD}' -C -Q 'SELECT 1' >/dev/null 2>&1 || \
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P '${SA_PASSWORD}' -Q 'SELECT 1' >/dev/null 2>&1
  "; then
    echo "[+] SQL Server đã sẵn sàng!"
    break
  fi
  echo "  (Thử lại sau 2 giây... $i/30)"
  sleep 2
done

echo "[*] Đang sao chép seeddata.sql vào container..."
docker cp "${SEED_FILE}" "${CONTAINER_NAME}:/tmp/seeddata.sql"

echo "[*] Đang thực thi seeddata.sql trên database '${DB_NAME}'..."
docker exec -u 0 "${CONTAINER_NAME}" /bin/bash -c "
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '${SA_PASSWORD}' -C -I -d '${DB_NAME}' -i /tmp/seeddata.sql || \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P '${SA_PASSWORD}' -I -d '${DB_NAME}' -i /tmp/seeddata.sql
"

docker exec -u 0 "${CONTAINER_NAME}" rm -f /tmp/seeddata.sql

echo "[+] Nạp dữ liệu mẫu thành công!"
