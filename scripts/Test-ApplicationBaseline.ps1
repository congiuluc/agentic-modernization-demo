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
$state = @{ Process = $null; Container = ''; Volume = ''; Starts = 0 }
$username = "baseline-$($id.Substring(0, 12))"
$password = [guid]::NewGuid().ToString('N')
$sqlEnvironmentNames = @('BLOG_DOTNET_CONNECTION_STRING', 'ConnectionStrings__Blog', 'SQLCONNSTR_Blog', 'SQLAZURECONNSTR_Blog', 'CUSTOMCONNSTR_Blog', 'MYSQLCONNSTR_Blog')
$environmentNames = @('BLOG_ADMIN_USER', 'BLOG_ADMIN_PASSWORD', 'BLOG_DOTNET_DB_PATH', 'BLOG_JAVA_DB_PATH') + $sqlEnvironmentNames
$savedEnvironment = @{}
foreach ($name in $environmentNames) { $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name) }
$success = $false
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$baseUrl = "http://localhost:$port"
$assemblyPath = ''

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
            $state.Process = Start-Process -FilePath (Get-Command dotnet -ErrorAction Stop).Source `
                -ArgumentList "`"$assemblyPath`" --urls $baseUrl --contentRoot `"$temporary`"" -WorkingDirectory $site -PassThru `
                -RedirectStandardOutput (Join-Path $temporary "dotnet-$($state.Starts).out.log") `
                -RedirectStandardError (Join-Path $temporary "dotnet-$($state.Starts).err.log")
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
            if ($state.Process -and $state.Process.HasExited) { throw 'The application process exited before becoming ready.' }
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
            $solution = Join-Path $root 'dotnet/LegacyBlog.sln'
            $project = Join-Path $root 'dotnet/LegacyBlog/LegacyBlog.csproj'
            if ($Restore) { Invoke-Checked dotnet @('restore', $solution) }
            Invoke-Checked dotnet @('build', $solution, '--configuration', 'Release', '--no-restore', '--nologo')
            Invoke-Checked dotnet @('test', (Join-Path $root 'dotnet/LegacyBlog.Tests/LegacyBlog.Tests.csproj'), '--configuration', 'Release', '--no-build', '--no-restore', '--nologo')
            $assemblyPath = (& dotnet msbuild $project '-getProperty:TargetPath' '-p:Configuration=Release' '-nologo' | Out-String).Trim()
            if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
                throw 'Could not locate the built .NET application assembly.'
            }
            $assembly = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($assemblyPath))
            if ($assembly.Contains('LegacyBlog.TrainingOnly')) { throw 'The .NET build contains intentionally vulnerable training code.' }
            Copy-Item -LiteralPath (Join-Path $root 'dotnet/LegacyBlog/Content') -Destination (Join-Path $temporary 'Content') -Recurse
            foreach ($name in $sqlEnvironmentNames) {
                if (Test-Path "Env:\$name") { Remove-Item "Env:\$name" }
            }
            $env:BLOG_DOTNET_DB_PATH = Join-Path $temporary 'blog.db'
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
        if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
    } finally {
        foreach ($name in $environmentNames) {
            if ($null -eq $savedEnvironment[$name]) {
                if (Test-Path "Env:\$name") { Remove-Item "Env:\$name" }
            } else {
                [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name])
            }
        }
    }
}
