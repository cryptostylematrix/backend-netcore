#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../../../.."
container="ton-connection-tests-$$"
cleanup() { docker rm -f "$container" >/dev/null 2>&1 || true; }
trap cleanup EXIT
# Disposable database only; trust authentication is confined to loopback and container lifetime.
docker run --rm -d --name "$container" -e POSTGRES_HOST_AUTH_METHOD=trust \
  -e POSTGRES_DB=ton_connection_tests -p 127.0.0.1::5432 postgres:17-alpine >/dev/null
for attempt in {1..60}; do
  if docker exec "$container" pg_isready -U postgres -d ton_connection_tests >/dev/null 2>&1; then break; fi
  sleep 1
done
docker exec "$container" pg_isready -U postgres -d ton_connection_tests >/dev/null
TON_CONNECTION_TEST_POSTGRES_PORT=$(docker port "$container" 5432/tcp | sed 's/.*://')
export TON_CONNECTION_TEST_POSTGRES_PORT
dotnet test tests/Modules/UI.Infrastructure.Tests/UI.Infrastructure.Tests.csproj \
  --filter FullyQualifiedName~TonConnectionPostgresTests "$@"
