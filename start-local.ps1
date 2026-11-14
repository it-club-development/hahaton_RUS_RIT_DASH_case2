$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (!(Test-Path -LiteralPath '.env')) { throw 'Copy .env.example to .env and set the passwords.' }

$settings = ConvertFrom-StringData -StringData (Get-Content -Raw -LiteralPath '.env')
if ($settings.WEBOTCHET_DB_PASSWORD -notmatch '^[a-zA-Z0-9_-]{12,}$') {
    throw 'Set WEBOTCHET_DB_PASSWORD to at least 12 letters, digits, underscores or hyphens.'
}
if ($settings.WEBOTCHET_ADMIN_PASSWORD.Length -lt 12) { throw 'Set WEBOTCHET_ADMIN_PASSWORD to at least 12 characters.' }
$port = if ($settings.WEBOTCHET_DB_PORT) { $settings.WEBOTCHET_DB_PORT } else { '3306' }
if ($port -notmatch '^\d{1,5}$' -or [int]$port -notin (1..65535)) { throw 'Invalid WEBOTCHET_DB_PORT.' }
$env:ConnectionStrings__MariaDb = "Server=127.0.0.1;Port=$port;Database=webotchet;User ID=webotchet;Password=$($settings.WEBOTCHET_DB_PASSWORD);"
$env:Bootstrap__Username = $settings.WEBOTCHET_ADMIN_USER
$env:Bootstrap__Password = $settings.WEBOTCHET_ADMIN_PASSWORD
$env:Bootstrap__Initialize = 'true'

Push-Location frontend
try {
    npm.cmd install
    if ($LASTEXITCODE -ne 0) { throw 'npm install failed.' }
    npm.cmd run build
    if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
} finally { Pop-Location }

New-Item -ItemType Directory -Force WebOtchet/wwwroot | Out-Null
Copy-Item -Path frontend/dist/* -Destination WebOtchet/wwwroot -Recurse -Force

docker compose up -d --wait db
if ($LASTEXITCODE -ne 0) { throw 'MariaDB did not start. Check Docker Desktop and docker compose logs db.' }

dotnet run --project WebOtchet --launch-profile http
if ($LASTEXITCODE -ne 0) { throw 'WebOtchet failed to start.' }

