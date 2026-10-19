#Requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('assessment-tests-' + [guid]::NewGuid().ToString('N'))
$originalToken = $env:GH_TOKEN
$originalDirectory = Get-Location
$fixture = Join-Path $root ('.local/assessment-scope-' + [guid]::NewGuid().ToString('N'))
$links = @()
$script:Checks = 0
$global:AssessmentTest = @{ Mode = 'success'; Calls = @(); Version = '1.0.76'; AuthExitCode = 0; Downloads = @(); Archive = '' }

function Assert-Test([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw "FAIL: $Message" }
    $script:Checks++
}

function global:gh {
    $global:LASTEXITCODE = $global:AssessmentTest.AuthExitCode
    if ($args[0] -eq '--version') {
        $global:LASTEXITCODE = 0
        'gh version 2.92.0'
    }
}

function global:modernize {
    $global:LASTEXITCODE = 0
    if ($args[0] -eq '--version') {
        $global:AssessmentTest.Version
        return
    }
    $global:AssessmentTest.Calls += ,@($args)
    if ($global:AssessmentTest.Mode -eq 'failure') {
        $global:LASTEXITCODE = 17
        return
    }
    $index = [array]::IndexOf($args, '--output-path')
    $output = $args[$index + 1]
    if ($global:AssessmentTest.Mode -ne 'missing-report') {
        $text = if ($global:AssessmentTest.Mode -eq 'empty-report') { '' } else { '# Assessment report' }
        Set-Content -LiteralPath (Join-Path $output 'report.md') -Value $text -NoNewline
    }
}

function global:Invoke-WebRequest {
    param([string]$Uri, [string]$OutFile)
    $global:AssessmentTest.Downloads += $Uri
    if ($OutFile) {
        $global:AssessmentTest.Archive = $OutFile
        Set-Content -LiteralPath $OutFile -Value 'corrupt archive' -NoNewline
    } else {
        $name = Split-Path $global:AssessmentTest.Archive -Leaf
        [pscustomobject]@{ Content = ('0' * 64) + " *$name" }
    }
}

function Invoke-TestCase([string]$Name, [hashtable]$Overrides = @{}, [string]$ExpectedError = '') {
    $parameters = @{
        AppPath = 'java'
        OutputPath = Join-Path $temporary $Name
    }
    foreach ($key in $Overrides.Keys) { $parameters[$key] = $Overrides[$key] }
    $errorMessage = ''
    try {
        & (Join-Path $PSScriptRoot 'Invoke-ModernizationAssessment.ps1') @parameters
    } catch {
        $errorMessage = $_.Exception.Message
    }
    if ($ExpectedError) {
        Assert-Test ($errorMessage -like "*$ExpectedError*") "$Name fails explicitly with the expected error (actual: $errorMessage)"
    } else {
        Assert-Test (!$errorMessage) "$Name succeeds (actual: $errorMessage)"
    }
}

try {
    New-Item -ItemType Directory -Path $temporary | Out-Null
    $env:GH_TOKEN = 'test-token-not-a-real-credential'
    Set-Location $root
    Invoke-TestCase 'success'
    $call = $global:AssessmentTest.Calls[-1]
    Assert-Test ($call[0] -eq 'assess') 'Only assessment is invoked.'
    Assert-Test ($call -contains '--no-tty') 'Headless execution is requested.'
    Assert-Test ($call[[array]::IndexOf($call, '--delegate') + 1] -eq 'local') 'No cloud agent is delegated.'
    Assert-Test ($call[[array]::IndexOf($call, '--format') + 1] -eq 'markdown') 'Markdown reports are requested.'
    Assert-Test ($call[[array]::IndexOf($call, '--source') + 1] -eq (Join-Path $root 'java')) 'Only the selected application is assessed.'
    Assert-Test ((Get-Location).Path -eq $root) 'Working directory is restored after success.'

    Invoke-TestCase 'config' @{ AssessmentConfig = 'docs/_config.yml' }
    Assert-Test ($global:AssessmentTest.Calls[-1] -contains '--assess-config') 'An explicit configuration is forwarded.'

    $global:AssessmentTest.Mode = 'failure'
    Invoke-TestCase 'cli-failure' @{} 'exit code 17'
    Assert-Test ((Get-Location).Path -eq $root) 'Working directory is restored after failure.'
    $global:AssessmentTest.Mode = 'missing-report'
    Invoke-TestCase 'missing-report' @{} 'nonempty Markdown'
    $global:AssessmentTest.Mode = 'empty-report'
    Invoke-TestCase 'empty-report' @{} 'nonempty Markdown'
    $global:AssessmentTest.Mode = 'success'

    $stale = Join-Path $temporary 'stale'
    New-Item -ItemType Directory -Path $stale | Out-Null
    Set-Content -LiteralPath (Join-Path $stale 'report.md') -Value 'old report'
    Invoke-TestCase 'stale' @{} 'stale reports'
    $global:AssessmentTest.Version = '0.0.1'
    Invoke-TestCase 'wrong-version' @{} 'Modernize 1.0.76 is required'
    Invoke-TestCase 'bad-download' @{ InstallCli = $true } 'checksum verification failed'
    Assert-Test ($global:AssessmentTest.Downloads.Count -eq 2) 'The installer retrieves an archive and its checksums.'
    Assert-Test ($global:AssessmentTest.Downloads[0].StartsWith('https://github.com/microsoft/modernize-cli/releases/download/v1.0.76/')) 'Only the pinned official release is downloaded.'
    Assert-Test (!(Test-Path -LiteralPath (Split-Path $global:AssessmentTest.Archive -Parent))) 'Failed installations are cleaned up.'
    $global:AssessmentTest.Version = '1.0.76'
    $global:AssessmentTest.AuthExitCode = 1
    Invoke-TestCase 'bad-auth' @{} 'authentication failed'
    $global:AssessmentTest.AuthExitCode = 0
    $env:GH_TOKEN = ''
    Invoke-TestCase 'missing-token' @{} 'Set GH_TOKEN'
    $env:GH_TOKEN = '$(MODERNIZE_GH_TOKEN)'
    Invoke-TestCase 'unresolved-token' @{} 'Set GH_TOKEN'
    $env:GH_TOKEN = 'test-token-not-a-real-credential'
    Invoke-TestCase 'outside-repository' @{ AppPath = $temporary } 'inside the checked-out repository'
    Invoke-TestCase 'missing-config' @{ AssessmentConfig = 'does-not-exist.yml' } 'does not exist'
    New-Item -ItemType Directory -Force -Path $fixture | Out-Null
    $linkType = if ($IsWindows) { 'Junction' } else { 'SymbolicLink' }
    $externalLink = Join-Path $fixture 'external'
    New-Item -ItemType $linkType -Path $externalLink -Target $temporary | Out-Null
    $links += $externalLink
    Invoke-TestCase 'linked-app' @{ AppPath = $externalLink } 'Linked assessment inputs are not allowed'
    Invoke-TestCase 'linked-parent' @{ AppPath = (Join-Path $externalLink 'success') } 'Linked assessment inputs are not allowed'
    Invoke-TestCase 'linked-descendant' @{ AppPath = $fixture } 'Linked assessment inputs are not allowed'
    Invoke-TestCase 'linked-config' @{ AssessmentConfig = (Join-Path $externalLink 'success/report.md') } 'Linked assessment inputs are not allowed'
    $internalLink = Join-Path $fixture 'internal'
    New-Item -ItemType $linkType -Path $internalLink -Target (Join-Path $root 'java') | Out-Null
    $links += $internalLink
    Invoke-TestCase 'internal-link' @{ AppPath = $internalLink } 'Linked assessment inputs are not allowed'
    Assert-Test (Test-Path -LiteralPath (Join-Path $temporary 'success/report.md')) 'Scope checks leave external targets unchanged.'
    Assert-Test ($global:AssessmentTest.Calls.Count -eq 5) 'Preflight failures do not run assessments.'
    Write-Host "Passed $script:Checks assessment automation checks without network calls or live assessments."
} finally {
    Set-Location $originalDirectory
    $env:GH_TOKEN = $originalToken
    Remove-Item Function:\gh, Function:\modernize, Function:\Invoke-WebRequest
    Remove-Variable AssessmentTest -Scope Global
    foreach ($link in $links) { [IO.Directory]::Delete($link) }
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
