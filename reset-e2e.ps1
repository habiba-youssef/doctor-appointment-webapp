
# Reset E2E test database (not development database)

Set-Location $PSScriptRoot

# Stop E2E containers and remove their database volume
docker compose -f compose.e2e.yaml down -v

if ($LASTEXITCODE -ne 0) {
    throw "Failed to remove E2E environment."
}

# Start a fresh E2E environment
docker compose -f compose.e2e.yaml up -d --wait

if ($LASTEXITCODE -ne 0) {
    throw "Failed to start E2E environment."
}

Write-Host "E2E database reset successfully!"
