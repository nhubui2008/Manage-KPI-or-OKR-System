#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)
env_file=${1:-"$repo_root/.env"}
mc_image='minio/mc@sha256:a7fe349ef4bd8521fb8497f55c6042871b2ae640607cf99d9bede5e9bdf11727'

read_env() {
  local key=$1
  sed -n "s/^${key}=//p" "$env_file" | tail -n 1
}

qdrant_endpoint=$(read_env Qdrant__Endpoint)
qdrant_collection=$(read_env Qdrant__CollectionName)
qdrant_key=$(read_env Qdrant__ApiKey)
minio_endpoint=$(read_env Minio__Endpoint)
minio_access=$(read_env Minio__AccessKey)
minio_secret=$(read_env Minio__SecretKey)
minio_bucket=$(read_env Minio__BucketName)

for value in "$qdrant_endpoint" "$qdrant_collection" "$qdrant_key" \
  "$minio_endpoint" "$minio_access" "$minio_secret" "$minio_bucket"; do
  if [[ -z "$value" ]]; then
    echo "Qdrant/MinIO configuration is incomplete in $env_file" >&2
    exit 1
  fi
done

docker compose --env-file "$env_file" \
  -f "$repo_root/deploy/rag/compose.local.yml" up -d qdrant minio

for _ in {1..30}; do
  if curl -fsS "${qdrant_endpoint%/}/healthz" >/dev/null 2>&1 &&
    curl -fsS "${minio_endpoint%/}/minio/health/live" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done
curl -fsS "${qdrant_endpoint%/}/healthz" >/dev/null
curl -fsS "${minio_endpoint%/}/minio/health/live" >/dev/null

mc_env=$(mktemp)
trap 'rm -f "$mc_env"' EXIT
chmod 600 "$mc_env"
printf 'KPI_MINIO_URL=%s\nKPI_MINIO_ACCESS=%s\nKPI_MINIO_SECRET=%s\nKPI_MINIO_BUCKET=%s\n' \
  "$minio_endpoint" "$minio_access" "$minio_secret" "$minio_bucket" >"$mc_env"

docker run --rm --network host --entrypoint /bin/sh --env-file "$mc_env" \
  "$mc_image" -c '
    mc alias set local "$KPI_MINIO_URL" "$KPI_MINIO_ACCESS" "$KPI_MINIO_SECRET" >/dev/null
    mc mb --ignore-existing "local/$KPI_MINIO_BUCKET" >/dev/null
    mc anonymous set none "local/$KPI_MINIO_BUCKET" >/dev/null
    mc stat "local/$KPI_MINIO_BUCKET" >/dev/null
  '

collection_url="${qdrant_endpoint%/}/collections/$qdrant_collection"
status=$(curl -sS -o /dev/null -w '%{http_code}' \
  -H "api-key: $qdrant_key" "$collection_url")
if [[ "$status" == 404 ]]; then
  curl -fsS -X PUT "$collection_url" \
    -H "api-key: $qdrant_key" -H 'Content-Type: application/json' \
    --data '{"vectors":{"size":1024,"distance":"Cosine"},"on_disk_payload":true}' >/dev/null
elif [[ "$status" != 200 ]]; then
  echo "Qdrant collection check failed with HTTP $status" >&2
  exit 1
fi

for spec in \
  'TenantId:integer' \
  'AllowedPrincipalIds:keyword' \
  'IsCurrent:bool' \
  'ChunkId:keyword' \
  'DocumentId:keyword' \
  'VersionId:keyword'; do
  field=${spec%%:*}
  schema=${spec#*:}
  curl -fsS -X PUT "$collection_url/index?wait=true" \
    -H "api-key: $qdrant_key" -H 'Content-Type: application/json' \
    --data "{\"field_name\":\"$field\",\"field_schema\":\"$schema\"}" >/dev/null
done

collection_state=$(curl -fsS -H "api-key: $qdrant_key" "$collection_url")
COLLECTION_STATE="$collection_state" python3 - <<'PY'
import json
import os

result = json.loads(os.environ["COLLECTION_STATE"])["result"]
vectors = result["config"]["params"]["vectors"]
required = {"TenantId", "AllowedPrincipalIds", "IsCurrent", "ChunkId", "DocumentId", "VersionId"}
actual = set(result["payload_schema"])
if vectors.get("size") != 1024 or vectors.get("distance") != "Cosine" or not required <= actual:
    raise SystemExit("Qdrant collection schema does not match the application contract.")
PY

curl -fsS "${qdrant_endpoint%/}/healthz" >/dev/null
curl -fsS "${minio_endpoint%/}/minio/health/live" >/dev/null
echo "Qdrant collection '$qdrant_collection' and private MinIO bucket '$minio_bucket' are ready."
