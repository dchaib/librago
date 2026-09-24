$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot

[xml]$packages = Get-Content (Join-Path $repositoryRoot "Directory.Packages.props") -Raw
$playwrightPackages = @($packages.Project.ItemGroup.PackageVersion |
    Where-Object { $_.Include -eq "Microsoft.Playwright" })

if ($playwrightPackages.Count -ne 1) {
    throw "Expected exactly one Microsoft.Playwright version in Directory.Packages.props."
}

$packageVersion = $playwrightPackages[0].Version
$dockerfile = Get-Content (Join-Path $repositoryRoot "Dockerfile") -Raw
$imagePattern = '(?m)^FROM mcr\.microsoft\.com/playwright/dotnet:v(?<version>\d+\.\d+\.\d+)-noble AS runtime\r?$'
$images = [regex]::Matches($dockerfile, $imagePattern)

if ($images.Count -ne 1) {
    throw "Expected exactly one Playwright .NET runtime image in Dockerfile."
}

$imageVersion = $images[0].Groups["version"].Value
if ($packageVersion -ne $imageVersion) {
    throw "Playwright versions differ: NuGet $packageVersion, Docker image $imageVersion."
}

Write-Output "Playwright versions match: $packageVersion"
