#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][uri]$RepositoryUrl,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{40}$')][string]$Revision,
    [string]$BasePath = '',
    [string]$SiteUrl = ''
)

$ErrorActionPreference = 'Stop'
if (!$RepositoryUrl.IsAbsoluteUri -or $RepositoryUrl.Scheme -ne 'https') {
    throw 'RepositoryUrl must be an absolute HTTPS repository URL.'
}

$root = Split-Path $PSScriptRoot -Parent
$destination = Join-Path $root '.pages-source'
if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Recurse -Force }
$repository = $RepositoryUrl.AbsoluteUri.TrimEnd('/')
$pages = [ordered]@{
    'README.md' = 'index.md'
    'dotnet/README.md' = 'dotnet/index.md'
    'java/README.md' = 'java/index.md'
}
$aliases = @{
    'dotnet' = 'dotnet/README.md'
    'java' = 'java/README.md'
}
$metadata = @{
    'README.md' = @{ Title = 'Modernization field guide'; Key = 'home'; Section = 'The field guide'; Description = 'A hands-on guide to assess, plan, and modernize legacy .NET and Java applications with the GitHub Copilot modernization agent.' }
    'dotnet/README.md' = @{ Title = '.NET Framework setup'; Key = 'dotnet'; Section = 'Application setup'; Description = 'Build, run, and verify the ASP.NET MVC 5 baseline. Windows, full Framework MSBuild, IIS Express, and SQL Server LocalDB are required.' }
    'java/README.md' = @{ Title = 'Java application setup'; Key = 'java'; Section = 'Application setup'; Description = 'Build, run, and verify the Java 11 Spring MVC baseline with Maven, Tomcat 9, and persistent H2 storage.' }
}

foreach ($page in $pages.GetEnumerator()) {
    $source = Join-Path $root $page.Key
    $markdown = Get-Content -Raw -LiteralPath $source
    $markdown = [regex]::Replace($markdown, '(?s)<!-- repository-header:start -->.*?<!-- repository-header:end -->', '')
    $markdown = [regex]::Replace($markdown, '\A# [^\r\n]+\r?\n', '')
    $markdown = [regex]::Replace($markdown, '\{\{|\{%', { param($match) '{{ "' + $match.Value + '" }}' })
    $sourceUri = [uri]::new("https://documentation.invalid/$($page.Key)")
    $content = [regex]::Replace($markdown, '(?<!!)\[(?<label>[^\]\r\n]+)\]\((?<target>[^)\s]+)\)', {
        param($match)
        $target = $match.Groups['target'].Value
        if ($target -match '^(?:[a-zA-Z][a-zA-Z0-9+.-]*:|//|#)') {
            return $match.Value
        }

        $resolved = [uri]::new($sourceUri, $target)
        $path = [uri]::UnescapeDataString($resolved.AbsolutePath.Trim('/'))
        if ($aliases.ContainsKey($path)) {
            $path = $aliases[$path]
        }
        if (!(Test-Path -LiteralPath (Join-Path $root $path))) {
            throw "Broken documentation link in $($page.Key): $target"
        }

        $suffix = $resolved.Query + $resolved.Fragment
        if ($path -eq 'docs/presentation.html') {
            $url = '{{ "/presentation/" | relative_url }}' + $suffix
        } elseif ($pages.Contains($path)) {
            $sitePath = '/' + ($pages[$path] -replace 'index\.md$', '')
            $url = '{{ "' + $sitePath + '" | relative_url }}' + $suffix
        } else {
            $url = "$repository/blob/$Revision$($resolved.AbsolutePath)$suffix"
        }
        return "[$($match.Groups['label'].Value)]($url)"
    })

    $output = Join-Path $destination $page.Value
    New-Item -ItemType Directory -Force -Path (Split-Path $output -Parent) | Out-Null
    $details = $metadata[$page.Key]
    $frontMatter = @(
        'layout: docs'
        "title: $($details.Title | ConvertTo-Json -Compress)"
        "description: $($details.Description | ConvertTo-Json -Compress)"
        "section: $($details.Section | ConvertTo-Json -Compress)"
        "page_key: $($details.Key)"
        "source_url: $("$repository/blob/$Revision/$($page.Key)" | ConvertTo-Json -Compress)"
    ) -join "`n"
    Set-Content -LiteralPath $output -Encoding utf8NoBOM -Value "---`n$frontMatter`n---`n`n$content"
}

$presentation = Join-Path $destination 'presentation'
New-Item -ItemType Directory -Force -Path $presentation | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'docs/presentation.html') -Destination (Join-Path $presentation 'index.html')
foreach ($file in @('_layouts/docs.html', 'assets/site.css', 'assets/site.js')) {
    $output = Join-Path $destination $file
    New-Item -ItemType Directory -Force -Path (Split-Path $output -Parent) | Out-Null
    Copy-Item -LiteralPath (Join-Path $root "docs/$file") -Destination $output
}

$config = Get-Content -Raw -LiteralPath (Join-Path $root 'docs\_config.yml')
$config += "`nbaseurl: $($BasePath | ConvertTo-Json -Compress)`nurl: $($SiteUrl | ConvertTo-Json -Compress)`n"
Set-Content -LiteralPath (Join-Path $destination '_config.yml') -Encoding utf8NoBOM -Value $config
Write-Host "Generated $($pages.Count) documentation pages and the HTML presentation in $destination"
