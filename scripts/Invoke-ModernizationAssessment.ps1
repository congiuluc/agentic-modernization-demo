#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$AppPath,
    [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$OutputPath,
    [string]$AssessmentConfig = '',
    [ValidatePattern('^\d+\.\d+\.\d+$')][string]$CliVersion = '1.0.76',
    [switch]$InstallCli
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$root = Split-Path $PSScriptRoot -Parent
$installation = $null

function Resolve-RepositoryPath([string]$Path) {
    $resolved = (Resolve-Path -LiteralPath ([IO.Path]::GetFullPath($Path, $root))).Path
    $comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    if (!$resolved.Equals($root, $comparison) -and !$resolved.StartsWith($root + [IO.Path]::DirectorySeparatorChar, $comparison)) {
        throw "Assessment inputs must be inside the checked-out repository: $Path"
    }
    $current = $resolved
    while (!$current.Equals($root, $comparison)) {
        $item = Get-Item -LiteralPath $current -Force
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "Linked assessment inputs are not allowed: $current"
        }
        $current = Split-Path $current -Parent
    }
    return $resolved
}

if ([string]::IsNullOrWhiteSpace($env:GH_TOKEN) -or $env:GH_TOKEN -match '^\$\(.+\)$') {
    throw 'Set GH_TOKEN to a Copilot-enabled GitHub token. The built-in CI job token is not sufficient.'
}
$application = Resolve-RepositoryPath $AppPath
if (!(Test-Path -LiteralPath $application -PathType Container)) {
    throw "AppPath must be an application directory: $AppPath"
}
$linked = Get-ChildItem -LiteralPath $application -Recurse -Force |
    Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint } |
    Select-Object -First 1
if ($linked) {
    throw "Linked assessment inputs are not allowed: $($linked.FullName)"
}
$config = if ($AssessmentConfig) { Resolve-RepositoryPath $AssessmentConfig } else { $null }
if ($config -and !(Test-Path -LiteralPath $config -PathType Leaf)) {
    throw "AssessmentConfig must be a YAML file: $AssessmentConfig"
}
$output = [IO.Path]::GetFullPath($OutputPath, $root)
if ((Test-Path -LiteralPath $output) -and (Get-ChildItem -LiteralPath $output -Recurse -File -Force | Select-Object -First 1)) {
    throw "OutputPath must be empty to prevent publishing stale reports: $output"
}
New-Item -ItemType Directory -Force -Path $output | Out-Null

$gh = Get-Command gh -ErrorAction Stop
$ghVersion = (& $gh --version | Out-String)
if ($LASTEXITCODE -ne 0 -or $ghVersion -notmatch 'gh version (\d+\.\d+\.\d+)') {
    throw 'Could not determine the GitHub CLI version.'
}
if ([version]$Matches[1] -lt [version]'2.45.0') {
    throw 'GitHub CLI 2.45.0 or later is required.'
}
& $gh auth status --active
if ($LASTEXITCODE -ne 0) {
    throw 'GitHub authentication failed. Verify the secret token, repository access, and organization authorization.'
}

try {
    $command = Get-Command modernize -ErrorAction SilentlyContinue
    $versionPattern = '(?m)^' + [regex]::Escape($CliVersion) + '(?:\+|\s|$)'
    $versionMatches = $false
    if ($command) {
        $versionText = (& $command --version | Out-String)
        $versionMatches = $LASTEXITCODE -eq 0 -and $versionText -match $versionPattern
    }
    if (!$versionMatches) {
        if (!$InstallCli) {
            throw "Modernize $CliVersion is required. Provision it first or pass -InstallCli."
        }
        $platform = if ($IsWindows) { 'windows' } elseif ($IsMacOS) { 'darwin' } elseif ($IsLinux) { 'linux' } else { throw 'Unsupported operating system.' }
        $architecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString().ToLowerInvariant()
        if ($architecture -notin @('x64', 'arm64')) {
            throw "Unsupported architecture: $architecture"
        }
        $extension = if ($IsWindows) { 'zip' } else { 'tar.gz' }
        $archiveName = "modernize_${CliVersion}_${platform}_${architecture}.$extension"
        $release = "https://github.com/microsoft/modernize-cli/releases/download/v$CliVersion"
        $installation = Join-Path ([IO.Path]::GetTempPath()) ('modernize-ci-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $installation | Out-Null
        $archive = Join-Path $installation $archiveName
        Invoke-WebRequest -Uri "$release/$archiveName" -OutFile $archive
        $checksums = (Invoke-WebRequest -Uri "$release/modernize_${CliVersion}_checksums.txt").Content
        if ($checksums -is [byte[]]) { $checksums = [Text.Encoding]::UTF8.GetString($checksums) }
        $checksumPattern = '(?m)^(?<hash>[a-fA-F0-9]{64})\s+\*?' + [regex]::Escape($archiveName) + '\r?$'
        $checksum = [regex]::Match($checksums, $checksumPattern)
        if (!$checksum.Success -or (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ine $checksum.Groups['hash'].Value) {
            throw "Release archive checksum verification failed: $archiveName"
        }
        if ($IsWindows) {
            Expand-Archive -LiteralPath $archive -DestinationPath $installation
        } else {
            & tar -xzf $archive -C $installation
            if ($LASTEXITCODE -ne 0) { throw 'Could not extract the Modernize release archive.' }
            & chmod +x (Join-Path $installation 'modernize')
            if ($LASTEXITCODE -ne 0) { throw 'Could not make the Modernize executable runnable.' }
        }
        $command = Join-Path $installation $(if ($IsWindows) { 'modernize.exe' } else { 'modernize' })
        $versionText = (& $command --version | Out-String)
        if ($LASTEXITCODE -ne 0 -or $versionText -notmatch $versionPattern) {
            throw "The installed Modernize CLI does not match $CliVersion."
        }
    }

    $arguments = @('assess', '--source', $application, '--output-path', $output, '--format', 'markdown', '--delegate', 'local', '--no-tty')
    if ($config) { $arguments += @('--assess-config', $config) }
    Write-Host "Assessing $AppPath with Modernize $CliVersion (assessment only)."
    Push-Location $application
    try {
        & $command @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Modernization assessment failed with exit code $LASTEXITCODE. See the CLI output above."
        }
    } finally {
        Pop-Location
    }
    $reports = @(Get-ChildItem -LiteralPath $output -Recurse -File -Filter '*.md' | Where-Object Length -gt 0)
    if ($reports.Count -eq 0) {
        throw "The CLI did not generate any nonempty Markdown reports in $output."
    }
    Write-Host "Assessment generated $($reports.Count) Markdown report(s) in $output."
} finally {
    if ($installation) { Remove-Item -LiteralPath $installation -Recurse -Force }
}
