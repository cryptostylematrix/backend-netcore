#!/usr/bin/env bash
set -euo pipefail
repo_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
container_id="$(docker run --detach --rm --publish 127.0.0.1::5432 \
  --label cryptostyle.test=activity \
  --env POSTGRES_PASSWORD=activity-test-only --env POSTGRES_DB=activity_test postgres:17-alpine)"
cleanup() { docker stop "$container_id" >/dev/null; }
trap cleanup EXIT
ready=false
for attempt in {1..30}; do
  if docker exec "$container_id" pg_isready -U postgres >/dev/null 2>&1; then
    ready=true
    break
  fi
  sleep 1
done
if [[ "$ready" != true ]]; then
  echo 'Temporary PostgreSQL did not become ready.' >&2
  exit 1
fi
port_binding="$(docker port "$container_id" 5432/tcp)"
export ACTIVITY_TEST_POSTGRES_PORT="${port_binding##*:}"
cd "$repo_dir"
dotnet test tests/Modules/ReferalProgram.Application.Tests/ReferalProgram.Application.Tests.csproj \
  --filter FullyQualifiedName~ActivityPostgresTests "$@"
