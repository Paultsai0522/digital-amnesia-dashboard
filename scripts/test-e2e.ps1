Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-FreePort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()

    try {
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Wait-ForHealthyApi {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url,

        [int]$TimeoutSeconds = 60
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-RestMethod -Method Get -Uri $Url -TimeoutSec 3
            if ($response.status -eq "ok") {
                return
            }
        }
        catch {
        }

        Start-Sleep -Milliseconds 250
    }

    throw "Timed out waiting for backend health check at $Url"
}

function Get-JobLogs {
    param(
        [Parameter(Mandatory = $true)]
        [System.Management.Automation.Job]$Job
    )

    try {
        return (Receive-Job -Job $Job -Keep -ErrorAction SilentlyContinue | Out-String).Trim()
    }
    catch {
        return ""
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$port = Get-FreePort
$apiBaseUrl = "http://localhost:$port"
$pollIntervalMs = 200
$stepDelayMs = 200

$backendJob = $null
$workerJob = $null

try {
    $backendJob = Start-Job -ScriptBlock {
        param($Path, $Port)

        Set-Location $Path
        $env:PORT = [string]$Port
        dotnet run --project backend/DigitalAmnesia.Backend.csproj
    } -ArgumentList $repoRoot, $port

    $workerJob = Start-Job -ScriptBlock {
        param($Path, $ApiBaseUrl, $PollIntervalMs, $StepDelayMs)

        Set-Location $Path
        $env:BACKEND_API_URL = $ApiBaseUrl
        $env:WORKER_POLL_INTERVAL_MS = [string]$PollIntervalMs
        $env:WORKER_STEP_DELAY_MS = [string]$StepDelayMs
        dotnet run --project worker/DigitalAmnesia.Worker.csproj
    } -ArgumentList $repoRoot, $apiBaseUrl, $pollIntervalMs, $stepDelayMs

    Wait-ForHealthyApi -Url "$apiBaseUrl/health"

    $payload = @{
        username = "e2e-worker"
        displayName = "E2E Worker"
        keywords = @("scan", "queue")
    } | ConvertTo-Json

    $created = Invoke-RestMethod -Method Post -Uri "$apiBaseUrl/api/jobs" -ContentType "application/json" -Body $payload
    $jobId = $created.jobId

    if ([string]::IsNullOrWhiteSpace($jobId)) {
        throw "The backend did not return a job id."
    }

    $deadline = (Get-Date).AddSeconds(45)
    $job = $null

    while ((Get-Date) -lt $deadline) {
        $job = Invoke-RestMethod -Method Get -Uri "$apiBaseUrl/api/jobs/$jobId"

        if ($job.status -eq "completed") {
            break
        }

        if ($job.status -eq "failed") {
            throw "Job $jobId failed: $($job.error)"
        }

        Start-Sleep -Milliseconds 300
    }

    if ($null -eq $job -or $job.status -ne "completed") {
        throw "Job $jobId did not complete before the timeout."
    }

    [pscustomobject]@{
        jobId = $job.id
        status = $job.status
        progress = $job.progress
        targetCount = $job.targets.Count
        resultCount = $job.results.Count
    } | ConvertTo-Json
}
catch {
    $backendLogs = if ($null -ne $backendJob) { Get-JobLogs -Job $backendJob } else { "" }
    $workerLogs = if ($null -ne $workerJob) { Get-JobLogs -Job $workerJob } else { "" }

    Write-Error @"
E2E test failed: $($_.Exception.Message)

Backend logs:
$backendLogs

Worker logs:
$workerLogs
"@

    exit 1
}
finally {
    if ($null -ne $workerJob) {
        Stop-Job -Job $workerJob -ErrorAction SilentlyContinue | Out-Null
        Remove-Job -Job $workerJob -Force -ErrorAction SilentlyContinue | Out-Null
    }

    if ($null -ne $backendJob) {
        Stop-Job -Job $backendJob -ErrorAction SilentlyContinue | Out-Null
        Remove-Job -Job $backendJob -Force -ErrorAction SilentlyContinue | Out-Null
    }
}
