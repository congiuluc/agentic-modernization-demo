#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('DotNet', 'Java')][string]$App,
    [switch]$Restore
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$root = Split-Path $PSScriptRoot -Parent
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('blog-baseline-' + [guid]::NewGuid().ToString('N'))
$id = [guid]::NewGuid().ToString('N')
$state = @{ Process = $null; Container = ''; Volume = ''; Starts = 0; LocalDbCreated = $false }
$instance = "BlogBaseline_$id"
$username = "baseline-$($id.Substring(0, 12))"
$password = [guid]::NewGuid().ToString('N')
$environmentNames = @('BLOG_ADMIN_USER', 'BLOG_ADMIN_PASSWORD', 'BLOG_DOTNET_CONNECTION_STRING', 'BLOG_JAVA_DB_PATH')
$savedEnvironment = @{}
foreach ($name in $environmentNames) { $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name) }
$success = $false
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$baseUrl = "http://localhost:$port"
$sqlLocalDb = $null
$iis = $null

function Invoke-Checked([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE." }
}

function Stop-BaselineServer {
    if ($state.Process) {
        if (!$state.Process.HasExited) {
            Stop-Process -Id $state.Process.Id
            $state.Process.WaitForExit()
        }
        $state.Process.Dispose()
        $state.Process = $null
    }
    if ($state.Container) {
        Invoke-Checked docker @('rm', '--force', $state.Container)
        $state.Container = ''
    }
}

function Start-BaselineServer {
    if ($state.Starts -eq 0) {
        $env:BLOG_ADMIN_USER = $username
        $env:BLOG_ADMIN_PASSWORD = $password
    } else {
        Remove-Item Env:\BLOG_ADMIN_USER, Env:\BLOG_ADMIN_PASSWORD -ErrorAction SilentlyContinue
    }
    try {
        if ($App -eq 'DotNet') {
            $site = Join-Path $root 'dotnet/LegacyBlog'
            $state.Process = Start-Process -FilePath $iis -ArgumentList "/path:`"$site`" /port:$port /systray:false" -PassThru `
                -RedirectStandardOutput (Join-Path $temporary "iis-$($state.Starts).out.log") `
                -RedirectStandardError (Join-Path $temporary "iis-$($state.Starts).err.log")
        } else {
            $war = Join-Path $root 'java/target/ROOT.war'
            $container = & docker run --detach --name "blog-baseline-$id" -p "127.0.0.1:${port}:8080" `
                --mount "type=bind,source=$war,target=/usr/local/tomcat/webapps/ROOT.war,readonly" `
                --mount "type=volume,source=$($state.Volume),target=/var/lib/legacyblog" `
                -e BLOG_ADMIN_USER -e BLOG_ADMIN_PASSWORD -e BLOG_JAVA_DB_PATH=/var/lib/legacyblog/blog tomcat:9-jdk11-temurin
            if ($LASTEXITCODE -ne 0) { throw 'Could not start the disposable Tomcat container.' }
            $state.Container = ($container | Out-String).Trim()
        }
        $state.Starts++
    } finally {
        $env:BLOG_ADMIN_USER = $username
        $env:BLOG_ADMIN_PASSWORD = $password
    }

    $client = [Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds(3)
    $deadline = [DateTime]::UtcNow.AddSeconds(120)
    $lastResult = 'No HTTP response'
    try {
        while ([DateTime]::UtcNow -lt $deadline) {
            if ($state.Process -and $state.Process.HasExited) { throw 'IIS Express exited before the application became ready.' }
            try {
                $response = $client.GetAsync("$baseUrl/").GetAwaiter().GetResult()
                try {
                    $lastResult = "HTTP $([int]$response.StatusCode)"
                    if ($response.IsSuccessStatusCode) {
                        Write-Host "$App ready at $baseUrl (start $($state.Starts))."
                        return
                    }
                } finally { $response.Dispose() }
            } catch [Net.Http.HttpRequestException] {
                $lastResult = $_.Exception.Message
            } catch [Threading.Tasks.TaskCanceledException] {
                $lastResult = $_.Exception.Message
            }
            Start-Sleep -Milliseconds 500
        }
        throw "$App did not become ready within 120 seconds: $lastResult"
    } finally { $client.Dispose() }
}

try {
    New-Item -ItemType Directory -Path $temporary | Out-Null
    Push-Location $root
    try {
        if ($App -eq 'DotNet') {
            if (!$IsWindows) { throw 'The .NET Framework baseline requires Windows.' }
            $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
            $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
            if (!$msbuild) { throw 'Full Framework MSBuild is required.' }
            $arguments = @((Join-Path $root 'dotnet/LegacyBlog.sln'), '/p:Configuration=Release', '/v:minimal', '/nologo')
            if ($Restore) { $arguments += @('/restore', '/p:RestorePackagesConfig=true') }
            Invoke-Checked $msbuild $arguments
            $assembly = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes((Join-Path $root 'dotnet/LegacyBlog/bin/LegacyBlog.dll')))
            if ($assembly.Contains('LegacyBlog.TrainingOnly')) { throw 'The .NET build contains intentionally vulnerable training code.' }
            $iis = Join-Path $env:ProgramFiles 'IIS Express/iisexpress.exe'
            if (!(Test-Path -LiteralPath $iis)) { throw 'IIS Express is required.' }
            $sqlLocalDb = (Get-Command SqlLocalDB -ErrorAction SilentlyContinue).Source
            if (!$sqlLocalDb) {
                $sqlLocalDb = Get-ChildItem -Path (Join-Path $env:ProgramFiles 'Microsoft SQL Server/*/Tools/Binn/SqlLocalDB.exe') |
                    Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
            }
            if (!$sqlLocalDb) { throw 'SQL Server Express LocalDB is required.' }
            Invoke-Checked $sqlLocalDb @('create', $instance, '-s')
            $state.LocalDbCreated = $true
            $databaseFile = Join-Path $temporary 'baseline.mdf'
            $env:BLOG_DOTNET_CONNECTION_STRING = "Data Source=(LocalDB)\$instance;Initial Catalog=BlogBaseline;AttachDbFilename=$databaseFile;Integrated Security=True;MultipleActiveResultSets=True"
        } else {
            Invoke-Checked mvn @('-B', '-ntp', '-f', (Join-Path $root 'java/pom.xml'), 'package')
            $archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $root 'java/target/ROOT.war'))
            try {
                if ($archive.Entries.FullName -match 'trainingonly|security-examples') { throw 'The WAR contains intentionally vulnerable training code.' }
            } finally { $archive.Dispose() }
            $null = Get-Command docker -ErrorAction Stop
            $volume = & docker volume create "blog-baseline-$id"
            if ($LASTEXITCODE -ne 0) { throw 'Could not create the disposable database volume.' }
            $state.Volume = ($volume | Out-String).Trim()
        }
        Start-BaselineServer
        $parameters = @{ App = $App; RestartApplication = { Stop-BaselineServer; Start-BaselineServer } }
        if ($App -eq 'DotNet') { $parameters.DotNetBaseUrl = $baseUrl } else { $parameters.JavaBaseUrl = $baseUrl }
        & (Join-Path $PSScriptRoot 'Test-Blogs.ps1') @parameters
        if ($state.Starts -ne 2) { throw 'The persistence checks did not perform a complete application restart.' }
        $success = $true
        Write-Host "$App baseline, restart persistence, and training-fixture isolation passed."
    } finally { Pop-Location }
} finally {
    try {
        if (!$success) {
            if ($state.Container) { & docker logs --tail 100 $state.Container }
            Get-ChildItem -LiteralPath $temporary -Filter '*.log' -ErrorAction SilentlyContinue |
                ForEach-Object { Write-Host $_.Name; Get-Content -LiteralPath $_.FullName -Tail 50 }
        }
        Stop-BaselineServer
        if ($state.Volume) { Invoke-Checked docker @('volume', 'rm', $state.Volume) }
        if ($state.LocalDbCreated) {
            Invoke-Checked $sqlLocalDb @('stop', $instance, '-k')
            Invoke-Checked $sqlLocalDb @('delete', $instance)
        }
        if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
    } finally {
        foreach ($name in $environmentNames) { [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name]) }
    }
}
