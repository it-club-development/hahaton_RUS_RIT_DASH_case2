#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "${BASH_SOURCE[0]}")"
[[ -f .env ]] || { echo 'Copy .env.example to .env and set the passwords.' >&2; exit 1; }
read_setting() {
  local line
  line=$(grep -m1 "^$1=" .env || true)
  line=${line#*=}
  printf '%s' "${line%$'\r'}"
}

db_password=$(read_setting WEBOTCHET_DB_PASSWORD)
admin_password=$(read_setting WEBOTCHET_ADMIN_PASSWORD)
port=$(read_setting WEBOTCHET_DB_PORT)
admin_user=$(read_setting WEBOTCHET_ADMIN_USER)
[[ $db_password =~ ^[a-zA-Z0-9_-]{12,}$ && ${#admin_password} -ge 12 ]] || {
  echo 'Set both passwords in .env (at least 12 characters).' >&2
  exit 1
}
port=${port:-3306}
[[ $port =~ ^[0-9]{1,5}$ ]] && (( 10#$port >= 1 && 10#$port <= 65535 )) || { echo 'Invalid database port.' >&2; exit 1; }
export ConnectionStrings__MariaDb="Server=127.0.0.1;Port=$port;Database=webotchet;User ID=webotchet;Password=$db_password;"
export Bootstrap__Initialize=true Bootstrap__Username="${admin_user:-admin}" Bootstrap__Password="$admin_password"

(cd frontend && npm ci && npm run build)
mkdir -p WebOtchet/wwwroot
cp -R frontend/dist/. WebOtchet/wwwroot/
docker compose up -d --wait db
exec dotnet run --project WebOtchet --launch-profile http

