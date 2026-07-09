param(
    [string] $Configuration = "Release",
    [string] $Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $repoRoot "src\ClickLockNotifier\ClickLockNotifier.csproj"
$output = Join-Path $repoRoot "dist\$Runtime"

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $output

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Copy-Item (Join-Path $repoRoot "LICENSE") (Join-Path $output "LICENSE") -Force
Copy-Item (Join-Path $repoRoot "THIRD_PARTY_NOTICES.md") (Join-Path $output "THIRD_PARTY_NOTICES.md") -Force

Write-Host "Published to $output"
