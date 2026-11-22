param(
    [string]$OutputDir = (Join-Path $PSScriptRoot 'dist')
)

$ErrorActionPreference = 'Stop'
$cscCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$csc = $null
foreach ($candidate in $cscCandidates) {
    if (Test-Path $candidate) { $csc = $candidate; break }
}
if (-not $csc) { throw '找不到 csc.exe，需要 .NET Framework 4.x。' }

$sources = Get-ChildItem (Join-Path $PSScriptRoot 'src') -Filter '*.cs' |
    ForEach-Object { $_.FullName } | Sort-Object
$manifest = Join-Path $PSScriptRoot 'app.manifest'
$xray = Join-Path $PSScriptRoot 'assets\xray.exe'
$wintun = Join-Path $PSScriptRoot 'assets\wintun.dll'
if (-not (Test-Path $xray)) { throw "缺少 $xray" }
if (-not (Test-Path $wintun)) { throw "缺少 $wintun" }

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$arguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/langversion:5',
    ('/win32manifest:' + $manifest),
    ('/out:' + (Join-Path $OutputDir 'XrayLite.exe')),
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Web.Extensions.dll',
    '/reference:System.Windows.Forms.dll'
) + $sources

& $csc $arguments
if ($LASTEXITCODE -ne 0) { throw "编译失败（csc 退出码 $LASTEXITCODE）" }

Copy-Item $xray (Join-Path $OutputDir 'xray.exe') -Force
Copy-Item $wintun (Join-Path $OutputDir 'wintun.dll') -Force
Copy-Item (Join-Path $PSScriptRoot 'assets\Xray-LICENSE') (Join-Path $OutputDir 'Xray-LICENSE') -Force
Copy-Item (Join-Path $PSScriptRoot 'assets\Wintun-LICENSE') (Join-Path $OutputDir 'Wintun-LICENSE') -Force

Write-Host ''
Write-Host '构建成功：' (Join-Path $OutputDir 'XrayLite.exe')
