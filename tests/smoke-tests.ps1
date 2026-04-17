$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$appPath = Join-Path $projectRoot 'bin\Debug\net8.0\WeatherService.dll'
$runDir = Join-Path $projectRoot '.codex-run'

if (-not (Test-Path $appPath)) {
    throw "Build the project first: dotnet build WeatherService.sln"
}

New-Item -ItemType Directory -Force -Path $runDir | Out-Null

$oldDatabase = $env:ConnectionStrings__WeatherDatabase
$smokeDb = Join-Path $runDir "weather-smoke-$PID.db"
$env:ConnectionStrings__WeatherDatabase = "Data Source=$smokeDb"

$stdoutLog = Join-Path $runDir "weather-smoke-$PID.out.log"
$stderrLog = Join-Path $runDir "weather-smoke-$PID.err.log"
$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = "dotnet"
$processInfo.Arguments = "`"$appPath`" --urls http://127.0.0.1:5090"
$processInfo.WorkingDirectory = $projectRoot
$processInfo.UseShellExecute = $false
$processInfo.RedirectStandardOutput = $true
$processInfo.RedirectStandardError = $true

$process = [System.Diagnostics.Process]::Start($processInfo)

try {
    $ready = $false
    for ($i = 0; $i -lt 15; $i++) {
        Start-Sleep -Seconds 1
        try {
            $icon = Invoke-WebRequest -Uri 'http://127.0.0.1:5090/static/icons/clear.png' -UseBasicParsing
            if ($icon.StatusCode -eq 200) {
                $ready = $true
                break
            }
        }
        catch {
            if ($process.HasExited) {
                throw "WeatherService exited before smoke tests started."
            }
        }
    }

    if (-not $ready) {
        throw "WeatherService did not become ready in time. See logs in $runDir."
    }

    $day = Invoke-WebRequest -Uri 'http://127.0.0.1:5090/api/weather/Malaga?date=2025-09-19' -UseBasicParsing
    $dayCached = Invoke-WebRequest -Uri 'http://127.0.0.1:5090/api/weather/Malaga?date=2025-09-19' -UseBasicParsing
    $week = Invoke-WebRequest -Uri 'http://127.0.0.1:5090/api/weather/Malaga/week' -UseBasicParsing
    $top = Invoke-WebRequest -Uri 'http://127.0.0.1:5090/api/stats/top-cities?from=2026-04-01&to=2026-04-30&limit=5' -UseBasicParsing
    $requests = Invoke-WebRequest -Uri 'http://127.0.0.1:5090/api/stats/requests?from=2026-04-01&to=2026-04-30&page=1&pageSize=10' -UseBasicParsing

    if ($day.StatusCode -ne 200) { throw "Day endpoint failed." }
    if ($dayCached.StatusCode -ne 200) { throw "Cached day endpoint failed." }
    if ($week.StatusCode -ne 200) { throw "Week endpoint failed." }
    if ($top.StatusCode -ne 200) { throw "Top cities endpoint failed." }
    if ($requests.StatusCode -ne 200) { throw "Requests endpoint failed." }

    $requestsJson = $requests.Content | ConvertFrom-Json
    $hasCacheHit = $requestsJson.items | Where-Object { $_.endpoint -eq 'day' -and $_.cacheHit -eq $true } | Select-Object -First 1
    if ($null -eq $hasCacheHit) {
        throw "Cache hit was not found in request statistics."
    }

    Write-Host "Smoke tests passed."
}
finally {
    if ($process -and -not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit()
    }

    if ($process) {
        Set-Content -Path $stdoutLog -Value $process.StandardOutput.ReadToEnd()
        Set-Content -Path $stderrLog -Value $process.StandardError.ReadToEnd()
    }

    if ($null -eq $oldDatabase) {
        Remove-Item Env:ConnectionStrings__WeatherDatabase -ErrorAction SilentlyContinue
    }
    else {
        $env:ConnectionStrings__WeatherDatabase = $oldDatabase
    }
}
