[CmdletBinding()]
param(
    [ValidateSet('DotNet', 'Java', 'All')][string]$App = 'All',
    [string]$DotNetBaseUrl = 'http://localhost:5080',
    [string]$JavaBaseUrl = 'http://localhost:8080',
    [scriptblock]$RestartApplication
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$script:Checks = 0
$script:CleanupFailed = $false

function Assert-Blog([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw "FAIL: $Message" }
    $script:Checks++
    Write-Host "PASS: $Message"
}

function New-Browser {
    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.AllowAutoRedirect = $false
    $handler.UseCookies = $true
    $handler.CookieContainer = New-Object System.Net.CookieContainer
    $client = New-Object System.Net.Http.HttpClient($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(60)
    return $client
}

function Send-Blog($Client, [string]$Url, [hashtable]$Fields = $null) {
    if ($null -eq $Fields) {
        $response = $Client.GetAsync($Url).GetAwaiter().GetResult()
    } else {
        $pairs = New-Object 'System.Collections.Generic.List[System.Collections.Generic.KeyValuePair[string,string]]'
        foreach ($key in $Fields.Keys) {
            $pairs.Add([System.Collections.Generic.KeyValuePair[string,string]]::new($key, [string]$Fields[$key]))
        }
        $content = [System.Net.Http.FormUrlEncodedContent]::new($pairs)
        try { $response = $Client.PostAsync($Url, $content).GetAwaiter().GetResult() }
        finally { $content.Dispose() }
    }
    try {
        return [pscustomobject]@{
            Status = [int]$response.StatusCode
            Html = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            Location = [string]$response.Headers.Location
            Url = $Url
        }
    } finally { $response.Dispose() }
}

function Attribute([string]$Tag, [string]$Name) {
    $match = [regex]::Match($Tag, '(?is)(?:^|\s)' + [regex]::Escape($Name) + '\s*=\s*["'']([^"'']*)["'']')
    if ($match.Success) { return [System.Net.WebUtility]::HtmlDecode($match.Groups[1].Value) }
    return ''
}

function Get-BlogForm($Page, [string]$ActionPattern) {
    foreach ($match in [regex]::Matches($Page.Html, '(?is)<form\b([^>]*)>(.*?)</form>')) {
        $attributes = $match.Groups[1].Value
        if ((Attribute $attributes 'method') -ine 'post') { continue }
        $action = Attribute $attributes 'action'
        if (!$action) { $action = $Page.Url }
        $url = [uri]::new([uri]$Page.Url, $action).AbsoluteUri
        if ($url -notmatch $ActionPattern) { continue }
        $fields = @{}
        foreach ($input in [regex]::Matches($match.Groups[2].Value, '(?is)<input\b[^>]*>')) {
            $name = Attribute $input.Value 'name'
            if ($name) { $fields[$name] = Attribute $input.Value 'value' }
        }
        return [pscustomobject]@{ Url = $url; Fields = $fields }
    }
    throw "Expected POST form matching '$ActionPattern' was not found on $($Page.Url)."
}

function Submit-BlogForm($Client, $Page, [string]$Pattern, [hashtable]$Values = @{}) {
    $form = Get-BlogForm $Page $Pattern
    foreach ($name in $Values.Keys) { $form.Fields[$name] = $Values[$name] }
    return Send-Blog $Client $form.Url $form.Fields
}

function Assert-Redirect($Response, [string]$Message) {
    Assert-Blog ($Response.Status -in @(302, 303)) $Message
}

function Find-RecordId($Page, [string]$Kind, [string]$Marker) {
    foreach ($match in [regex]::Matches($Page.Html, '(?is)<article\b[^>]*data-' + $Kind + '-id=["''](\d+)["''][^>]*>(.*?)</article>')) {
        if ([System.Net.WebUtility]::HtmlDecode($match.Groups[2].Value).Contains($Marker)) { return $match.Groups[1].Value }
    }
    throw "Created $Kind record '$Marker' was not found."
}

function Find-Post($Client, [string]$Base, [string]$Marker) {
    for ($number = 1; $number -le 1000; $number++) {
        $page = Send-Blog $Client "$Base/admin/posts?page=$number"
        if ($page.Status -ne 200) { break }
        if ($page.Html.Contains($Marker)) { return $page }
        if ($page.Html -notmatch '>Next</a>') { break }
    }
    throw "Created post '$Marker' was not found in administrator pagination."
}

function Published-At($Page) {
    $match = [regex]::Match($Page.Html, '<time\b[^>]*datetime="([^"]+)"')
    if (!$match.Success -or !$match.Groups[1].Value.EndsWith('Z')) { throw 'Expected a machine-readable UTC publication timestamp.' }
    return [DateTimeOffset]::Parse($match.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture)
}

function Post-Ids($Client, [string]$Base) {
    $ids = @()
    for ($number = 1; $number -le 1000; $number++) {
        $page = Send-Blog $Client "$Base/admin/posts?page=$number"
        if ($page.Status -ne 200) { throw "Cannot enumerate posts: HTTP $($page.Status)." }
        $ids += @([regex]::Matches($page.Html, 'data-post-id="(\d+)"') | ForEach-Object { $_.Groups[1].Value })
        if ($page.Html -notmatch '>Next</a>') { return ($ids | Sort-Object) -join ',' }
    }
    throw 'Administrator pagination exceeded the regression-test limit.'
}

function Test-Engine([string]$Name, [string]$Base) {
    $Base = $Base.TrimEnd('/')
    $admin = New-Browser
    $guest = New-Browser
    $postId = $null
    $categoryId = $null
    $id = 'smoke-' + [Guid]::NewGuid().ToString('N')
    $slug = $id + '-post'
    $categorySlug = $id + '-category'
    $commentMarker = $id + '-comment'
    Write-Host "`n=== $Name : $Base ==="
    try {
        $homePage = Send-Blog $guest "$Base/"
        Assert-Blog ($homePage.Status -eq 200 -and $homePage.Html.Contains('Fieldnotes')) 'Public home responds with the expected application'
        Assert-Blog (([regex]::Matches($homePage.Html, 'class="card post-card"')).Count -le 10) 'Public pages contain at most ten posts'
        if ($homePage.Html -match '>Next</a>') {
            Assert-Blog (([regex]::Matches($homePage.Html, 'class="card post-card"')).Count -eq 10) 'A non-final page contains exactly ten posts'
            $secondPage = Send-Blog $guest "$Base/?page=2"
            Assert-Blog ($secondPage.Status -eq 200 -and $secondPage.Html -match 'Page 2') 'Public second page responds'
        }
        Assert-Blog ((Send-Blog $guest "$Base/?page=0").Status -eq 400) 'Invalid page number is rejected'
        Assert-Blog ((Send-Blog $guest "$Base/?page=abc").Status -eq 400) 'Non-numeric page is rejected'
        Assert-Blog ((Send-Blog $guest "$Base/?page=999999").Status -eq 404) 'Beyond-last page is rejected'
        Assert-Blog ((Send-Blog $guest "$Base/posts/no-such-$id").Status -eq 404) 'Missing post is 404'
        $protected = Send-Blog $guest "$Base/admin/posts"
        Assert-Blog ($protected.Status -in @(302, 303) -and $protected.Location -match '/account/login') 'Admin screens require authentication'
        $login = Send-Blog $admin "$Base/account/login"
        $withoutToken = Send-Blog $admin "$Base/account/login" @{ username = $env:BLOG_ADMIN_USER; password = 'wrong' }
        Assert-Blog ($withoutToken.Status -in @(400, 403)) 'Login requires a CSRF token'
        $badLogin = Submit-BlogForm $admin $login '/account/login' @{ username = $env:BLOG_ADMIN_USER; password = 'incorrect-password' }
        Assert-Blog ($badLogin.Status -eq 200 -or $badLogin.Location -match 'error') 'Incorrect password is rejected'
        Assert-Blog ((Send-Blog $admin "$Base/admin").Status -in @(302, 303)) 'Incorrect password does not establish an admin session'
        $login = Send-Blog $admin "$Base/account/login"
        if ($Name -eq 'Java 11') {
            $tokenFields = (Get-BlogForm $login '/account/login').Fields
            Assert-Blog ((Send-Blog $admin "$Base/" $tokenFields).Status -eq 405) 'Unsupported HTTP methods return 405 rather than 500'
        }
        $signedIn = Submit-BlogForm $admin $login '/account/login' @{ username = $env:BLOG_ADMIN_USER; password = $env:BLOG_ADMIN_PASSWORD; returnUrl = 'https://example.invalid/' }
        Assert-Redirect $signedIn 'Correct credentials sign in'
        Assert-Blog ($signedIn.Location -notmatch 'example\.invalid') 'Login cannot redirect off-site'
        Assert-Blog ((Send-Blog $admin "$Base/admin").Status -eq 200) 'Authenticated administrator can open dashboard'

        $categoryForm = Send-Blog $admin "$Base/admin/categories/create"
        $result = Submit-BlogForm $admin $categoryForm '/admin/categories/create' @{ name = $id; slug = $categorySlug }
        Assert-Redirect $result 'Administrator creates category'
        $categoryPage = Send-Blog $admin "$Base/admin/categories"
        $categoryId = Find-RecordId $categoryPage 'category' $categorySlug
        $empty = Send-Blog $guest "$Base/categories/$categorySlug"
        Assert-Blog ($empty.Status -eq 200 -and $empty.Html -match 'No published stories') 'Empty category has a friendly empty state'
        Assert-Blog ((Send-Blog $guest "$Base/categories/$categorySlug`?page=2").Status -eq 404) 'Empty category second page is 404'
        $result = Submit-BlogForm $admin (Send-Blog $admin "$Base/admin/categories/$categoryId/edit") "/categories/$categoryId/edit" @{ name = "$id edited"; slug = $categorySlug }
        Assert-Redirect $result 'Administrator edits category'
        $result = Submit-BlogForm $admin (Send-Blog $admin "$Base/admin/categories/create") '/categories/create' @{ name = $id; slug = $categorySlug }
        Assert-Blog ($result.Status -eq 200 -and $result.Html -match 'Slug already exists') 'Duplicate category slug is rejected'

        $editor = Send-Blog $admin "$Base/admin/posts/create"
        $result = Submit-BlogForm $admin $editor '/posts/create' @{ title = ' '; slug = $slug; body = ' '; categoryId = $categoryId }
        Assert-Blog ($result.Status -eq 200 -and $result.Html -match '(?i)(required|must not be blank)') 'Blank post fields are rejected'
        $result = Submit-BlogForm $admin $editor '/posts/create' @{ title = ('x' * 201); slug = $slug; body = 'text'; categoryId = $categoryId }
        Assert-Blog ($result.Status -eq 200 -and $result.Html -match '(?i)(maximum|size must be between)') 'Oversize post title is rejected'
        $result = Submit-BlogForm $admin $editor '/posts/create' @{
            title = $id; slug = $slug; body = '<script>alert("encoded")</script>'; categoryId = $categoryId
            published = 'true'; id = '1'
        }
        Assert-Redirect $result 'Administrator creates a draft'
        $postPage = Find-Post $admin $Base $slug
        $postId = Find-RecordId $postPage 'post' $slug
        Assert-Blog ((Send-Blog $guest "$Base/posts/$slug").Status -eq 404) 'Draft is private even with an overposted publication flag'
        Assert-Blog ((Send-Blog $guest "$Base/posts/$slug/comments" @{ author = $id; body = 'test' }).Status -in @(400, 403)) 'Comment POST requires CSRF'
        $result = Submit-BlogForm $admin (Send-Blog $admin "$Base/admin/posts/create") '/posts/create' @{ title = $id; slug = $slug; body = 'duplicate'; categoryId = $categoryId }
        Assert-Blog ($result.Status -eq 200 -and $result.Html -match 'Slug already exists') 'Duplicate post slug is rejected'
        $result = Submit-BlogForm $admin (Send-Blog $admin "$Base/admin/categories") "/categories/$categoryId/delete"
        Assert-Blog ($result.Status -eq 200 -and $result.Html -match 'cannot be deleted') 'In-use category cannot be deleted'
        $noToken = Send-Blog $admin "$Base/admin/posts/$postId/publish" @{}
        Assert-Blog ($noToken.Status -in @(400, 403)) 'Admin mutation requires CSRF'
        $guestForm = Get-BlogForm (Send-Blog $guest "$Base/account/login") '/account/login'
        $unauthorized = Send-Blog $guest "$Base/admin/posts/$postId/publish" $guestForm.Fields
        Assert-Blog ($unauthorized.Status -in @(302, 303, 403)) 'Anonymous users cannot execute admin mutations'
        Assert-Blog ((Send-Blog $guest "$Base/posts/$slug").Status -eq 404) 'Rejected mutation did not publish the draft'

        $result = Submit-BlogForm $admin (Find-Post $admin $Base $slug) "/posts/$postId/publish"
        Assert-Redirect $result 'Administrator publishes draft'
        $detail = Send-Blog $guest "$Base/posts/$slug"
        Assert-Blog ($detail.Status -eq 200) 'Published post is public'
        $publishedAt = Published-At $detail
        Assert-Blog ($publishedAt.Offset -eq [TimeSpan]::Zero) 'Publication timestamp is expressed in UTC'
        Assert-Blog ($detail.Html.Contains('&lt;script&gt;') -and !$detail.Html.Contains('<script>')) 'Plain-text HTML is encoded'
        Assert-Blog ((Send-Blog $guest "$Base/categories/$categorySlug").Html.Contains($slug)) 'Published post appears in category'
        $result = Submit-BlogForm $admin (Send-Blog $admin "$Base/admin/posts/$postId/edit") "/posts/$postId/edit" @{ title = "$id edited"; slug = $slug; body = 'Edited story'; categoryId = $categoryId }
        Assert-Redirect $result 'Administrator edits post'
        Assert-Blog ((Send-Blog $guest "$Base/posts/$slug").Html.Contains('Edited story')) 'Edited post persists and remains published'
        $detail = Send-Blog $guest "$Base/posts/$slug"
        Assert-Blog ((Published-At $detail) -eq $publishedAt) 'Editing a published post preserves its publication timestamp'
        $result = Submit-BlogForm $guest $detail "/posts/$slug/comments" @{ author = $id; body = ' ' }
        Assert-Blog ($result.Status -eq 200 -and $result.Html -match '(?i)(required|must not be blank)') 'Empty comment is rejected'
        $result = Submit-BlogForm $guest $detail "/posts/$slug/comments" @{ author = $id; body = $commentMarker; approved = 'true' }
        Assert-Redirect $result 'Guest submits comment'
        Assert-Blog (!(Send-Blog $guest "$Base/posts/$slug").Html.Contains($commentMarker)) 'New comment is pending despite overposted approval'
        $pending = Send-Blog $admin "$Base/admin/comments"
        $commentId = Find-RecordId $pending 'comment' $commentMarker
        $result = Submit-BlogForm $admin $pending "/comments/$commentId/approve"
        Assert-Redirect $result 'Administrator approves comment'
        Assert-Blog ((Send-Blog $guest "$Base/posts/$slug").Html.Contains($commentMarker)) 'Approved comment is public'
        if ($RestartApplication) {
            $beforeIds = Post-Ids $admin $Base
            $beforeCategories = (Send-Blog $admin "$Base/admin/categories").Html
            $beforeCategoryIds = @([regex]::Matches($beforeCategories, 'data-category-id="(\d+)"') | ForEach-Object { $_.Groups[1].Value }) -join ','
            & $RestartApplication | Out-Null
            $admin.Dispose(); $guest.Dispose()
            $admin = New-Browser
            $guest = New-Browser
            $restarted = Send-Blog $guest "$Base/posts/$slug"
            Assert-Blog ($restarted.Status -eq 200 -and $restarted.Html.Contains('Edited story')) 'Published content survives a complete application restart'
            Assert-Blog ($restarted.Html.Contains($commentMarker)) 'Approved comments survive a complete application restart'
            Assert-Blog ((Published-At $restarted) -eq $publishedAt) 'Restart preserves the publication timestamp'
            $login = Send-Blog $admin "$Base/account/login"
            $signedIn = Submit-BlogForm $admin $login '/account/login' @{ username = $env:BLOG_ADMIN_USER; password = $env:BLOG_ADMIN_PASSWORD }
            Assert-Redirect $signedIn 'Original administrator credentials still sign in after restart'
            Assert-Blog ((Send-Blog $admin "$Base/admin").Status -eq 200) 'A fresh administrator session works after restart'
            Assert-Blog ((Post-Ids $admin $Base) -eq $beforeIds) 'Restart neither duplicates nor replaces seed posts'
            $afterCategories = (Send-Blog $admin "$Base/admin/categories").Html
            $afterCategoryIds = @([regex]::Matches($afterCategories, 'data-category-id="(\d+)"') | ForEach-Object { $_.Groups[1].Value }) -join ','
            Assert-Blog ($afterCategoryIds -eq $beforeCategoryIds) 'Restart neither duplicates nor replaces seed categories'
            $detail = $restarted
        }
        $result = Submit-BlogForm $guest (Send-Blog $guest "$Base/posts/$slug") "/posts/$slug/comments" @{ author = $id; body = "$commentMarker-delete" }
        Assert-Redirect $result 'Guest submits second comment'
        $pending = Send-Blog $admin "$Base/admin/comments"
        $deleteCommentId = Find-RecordId $pending 'comment' "$commentMarker-delete"
        $result = Submit-BlogForm $admin $pending "/comments/$deleteCommentId/delete"
        Assert-Redirect $result 'Administrator deletes pending comment'
        Assert-Blog (!(Send-Blog $admin "$Base/admin/comments").Html.Contains("$commentMarker-delete")) 'Deleted comment leaves moderation queue'
        $result = Submit-BlogForm $admin (Find-Post $admin $Base $slug) "/posts/$postId/unpublish"
        Assert-Redirect $result 'Administrator unpublishes post'
        Assert-Blog ((Send-Blog $guest "$Base/posts/$slug").Status -eq 404) 'Unpublished post becomes private'
        $draftComment = Send-Blog $guest "$Base/posts/$slug/comments" (Get-BlogForm $detail "/posts/$slug/comments").Fields
        Assert-Blog ($draftComment.Status -eq 404) 'Comments cannot be added to a draft'
        $result = Submit-BlogForm $admin (Find-Post $admin $Base $slug) "/posts/$postId/publish"
        Assert-Redirect $result 'Administrator can republish an unpublished post'
        $republished = Send-Blog $guest "$Base/posts/$slug"
        Assert-Blog ((Published-At $republished) -gt $publishedAt) 'Republishing assigns a new publication timestamp'
        $result = Submit-BlogForm $guest $republished "/posts/$slug/comments" @{ author = $id; body = "$commentMarker-cascade" }
        Assert-Redirect $result 'A pending comment is created for cascade-deletion verification'
        Assert-Blog ((Send-Blog $admin "$Base/admin/comments").Html.Contains("$commentMarker-cascade")) 'The cascade-test comment exists before deleting its post'
        $result = Submit-BlogForm $admin (Send-Blog $admin "$Base/admin/posts/$postId/delete") "/posts/$postId/delete"
        Assert-Redirect $result 'Administrator deletes post'
        $postId = $null
        Assert-Blog (!(Send-Blog $admin "$Base/admin/comments").Html.Contains("$commentMarker-cascade")) 'Deleting a post removes its pending comments'
        $result = Submit-BlogForm $admin (Send-Blog $admin "$Base/admin/categories") "/categories/$categoryId/delete"
        Assert-Redirect $result 'Unused category can be deleted'
        $categoryId = $null
        Assert-Blog ((Send-Blog $guest "$Base/categories/$categorySlug").Status -eq 404) 'Deleted category is unavailable'
        $result = Submit-BlogForm $admin (Send-Blog $admin "$Base/admin") '/account/logout'
        Assert-Redirect $result 'Administrator signs out'
        Assert-Blog ((Send-Blog $admin "$Base/admin").Status -in @(302, 303)) 'Logout ends authenticated browser session'
    } finally {
        try {
            if ($postId) {
                $result = Submit-BlogForm $admin (Send-Blog $admin "$Base/admin/posts/$postId/delete") "/posts/$postId/delete"
                if ($result.Status -notin @(302, 303)) { throw "Post cleanup returned $($result.Status)." }
            }
            if ($categoryId) {
                $result = Submit-BlogForm $admin (Send-Blog $admin "$Base/admin/categories") "/categories/$categoryId/delete"
                if ($result.Status -notin @(302, 303)) { throw "Category cleanup returned $($result.Status)." }
            }
        } catch { $script:CleanupFailed = $true; Write-Warning "Smoke cleanup failed for records '$id': $($_.Exception.Message)" }
        $admin.Dispose(); $guest.Dispose()
    }
}

if ($RestartApplication -and $App -eq 'All') { throw 'A restart callback requires selecting one application.' }
if (!$env:BLOG_ADMIN_USER -or !$env:BLOG_ADMIN_PASSWORD) { throw 'Set BLOG_ADMIN_USER and BLOG_ADMIN_PASSWORD to the existing administrator credentials.' }
if ($App -in @('DotNet', 'All')) { Test-Engine '.NET blog' $DotNetBaseUrl }
if ($App -in @('Java', 'All')) { Test-Engine 'Java 11' $JavaBaseUrl }
if ($script:CleanupFailed) { throw 'One or more smoke test cleanup operations failed.' }
Write-Host "`nAll $script:Checks smoke assertions passed."
