#!/bin/sh
set -eu

KEYS_PATH="${MCP_TEST_SERVER_DATA_PROTECTION_KEYS_PATH:-/app/data-protection-keys}"
mkdir -p "$KEYS_PATH"
chown -R app:app "$KEYS_PATH"

if command -v setpriv >/dev/null 2>&1; then
  exec setpriv --reuid=app --regid=app --init-groups -- dotnet mcptestserver.api.dll
fi

if command -v runuser >/dev/null 2>&1; then
  exec runuser -u app -- dotnet mcptestserver.api.dll
fi

exec su app -s /bin/sh -c 'exec dotnet mcptestserver.api.dll'
