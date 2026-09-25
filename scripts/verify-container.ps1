param(
    [Parameter(Mandatory = $true)]
    [string]$Image
)

$ErrorActionPreference = "Stop"
$taskRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("librago-container-" + [Guid]::NewGuid().ToString("N"))
$containerName = "librago-check-" + [Guid]::NewGuid().ToString("N")
$volumeName = "librago-check-" + [Guid]::NewGuid().ToString("N")
$configPath = Join-Path $taskRoot "appsettings.Local.json"
$port = 18080

function Invoke-Container {
    param([string[]]$Arguments)

    & docker @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Docker command failed: docker $($Arguments -join ' ')"
    }
}

function Wait-ForHealth {
    for ($attempt = 1; $attempt -le 30; $attempt++) {
        try {
            $response = Invoke-WebRequest "http://127.0.0.1:$port/health" -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 1
    }

    Invoke-Container @("logs", $containerName)
    throw "The container did not become healthy."
}

try {
    New-Item -ItemType Directory -Path $taskRoot | Out-Null
    @'
{
  "Librago": {
    "DatabasePath": "/data/librago.db",
    "Accounts": []
  }
}
'@ | Set-Content -Path $configPath -NoNewline

    Invoke-Container @("volume", "create", $volumeName)
    Invoke-Container @(
        "run", "--detach", "--init", "--name", $containerName,
        "--publish", "127.0.0.1:$port`:8080",
        "--read-only", "--tmpfs", "/tmp", "--shm-size", "1gb",
        "--security-opt", "no-new-privileges:true",
        "--security-opt", "seccomp=./seccomp_profile.json",
        "--volume", "$volumeName`:/data",
        "--mount", "type=bind,src=$configPath,dst=/config/appsettings.Local.json,readonly",
        $Image)
    Wait-ForHealth
    Invoke-Container @("exec", $containerName, "/bin/sh", "-c", 'test "$(id -u)" -ne 0')
    Invoke-Container @("cp", "$containerName`:/data/librago.db", (Join-Path $taskRoot "before-restart.db"))

    Invoke-Container @("restart", $containerName)
    Wait-ForHealth
    Invoke-Container @("cp", "$containerName`:/data/librago.db", (Join-Path $taskRoot "after-restart.db"))

    if ((Get-Item (Join-Path $taskRoot "before-restart.db")).Length -eq 0 -or
        (Get-Item (Join-Path $taskRoot "after-restart.db")).Length -eq 0) {
        throw "SQLite persistence was not verified."
    }

    $browserProbe = @'
const { chromium } = require('/app/.playwright/package');
(async () => {
  for (const headless of [true, false]) {
    const browser = await chromium.launch({ headless, channel: 'chromium', chromiumSandbox: true });
    await browser.close();
  }
})().catch(error => { console.error(error.name); process.exit(1); });
'@
    Invoke-Container @(
        "exec", "--env", "DISPLAY=:99", $containerName,
        "/app/.playwright/node/linux-x64/node", "-e", $browserProbe)
}
finally {
    & docker rm --force $containerName 2>$null | Out-Null
    & docker volume rm $volumeName 2>$null | Out-Null

    if ((Test-Path -LiteralPath $taskRoot) -and
        $taskRoot.StartsWith([System.IO.Path]::GetTempPath(), [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $taskRoot).StartsWith("librago-container-", [StringComparison]::Ordinal)) {
        Remove-Item -LiteralPath $taskRoot -Recurse -Force
    }
}
