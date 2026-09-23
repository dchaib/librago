param(
    [Parameter(Mandatory = $true)]
    [string]$Image
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$configurationPath = Join-Path $repositoryRoot "src/Librago/appsettings.Local.json"
$taskRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("librago-artifacts-" + [Guid]::NewGuid().ToString("N"))
$publishPath = Join-Path $taskRoot "publish"
$sentinel = "SYNTHETIC_ARTIFACT_SECRET_" + [Guid]::NewGuid().ToString("N")

if (Test-Path -LiteralPath $configurationPath) {
    throw "Refusing to replace an existing local configuration file. Run this check in an isolated checkout."
}

try {
    New-Item -ItemType Directory -Path $taskRoot | Out-Null
    "{ `"Librago`": { `"Sentinel`": `"$sentinel`" } }" |
        Set-Content -Path $configurationPath -NoNewline

    & dotnet restore "$repositoryRoot/src/Librago/Librago.csproj" --locked-mode
    if ($LASTEXITCODE -ne 0) {
        throw "Restoring the application failed."
    }

    & dotnet publish "$repositoryRoot/src/Librago/Librago.csproj" `
        --configuration Release --no-restore --output $publishPath
    if ($LASTEXITCODE -ne 0) {
        throw "Publishing the application failed."
    }

    if (Test-Path -LiteralPath (Join-Path $publishPath "appsettings.Local.json")) {
        throw "The local configuration was copied to the publish output."
    }

    & docker build --tag $Image $repositoryRoot
    if ($LASTEXITCODE -ne 0) {
        throw "Building the container image failed."
    }

    $checkScript = "test ! -e /app/appsettings.Local.json && ! grep -R -a -q '$sentinel' /app"
    & docker run --rm --entrypoint /bin/sh $Image -c $checkScript
    if ($LASTEXITCODE -ne 0) {
        throw "The local configuration or its sentinel was found in the image."
    }
}
finally {
    Remove-Item -LiteralPath $configurationPath -Force -ErrorAction SilentlyContinue

    if ((Test-Path -LiteralPath $taskRoot) -and
        $taskRoot.StartsWith([System.IO.Path]::GetTempPath(), [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $taskRoot).StartsWith("librago-artifacts-", [StringComparison]::Ordinal)) {
        Remove-Item -LiteralPath $taskRoot -Recurse -Force
    }
}
