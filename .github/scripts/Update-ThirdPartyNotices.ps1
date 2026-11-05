$ErrorActionPreference = 'Stop'

$nugetLicenseVersion = '4.0.18'
$allowedLicenses = 'MIT;Apache-2.0;BSD-3-Clause;PostgreSQL;LGPL-3.0-or-later;LGPL-3.0-only'
$rootDir = Resolve-Path "$PSScriptRoot/../../"
$solution = Join-Path $rootDir 'src/ZGW.all.sln'
$overrides = Join-Path $PSScriptRoot 'third-party-notices-overrides.json'
$noticesFile = Join-Path $rootDir 'THIRD-PARTY-NOTICES.md'
$packagesDir = Join-Path $rootDir 'third-party-licenses/nuget'
$upstreamDir = Join-Path $rootDir 'third-party-licenses/upstream'

if (-not (Get-Command nuget-license -ErrorAction SilentlyContinue)) {
    Write-Error "nuget-license is not installed. Run: dotnet tool install --global nuget-license --version $nugetLicenseVersion"
}
$installedVersion = ((nuget-license --version) -replace '\+.*$', '').Trim()
if ($installedVersion -ne $nugetLicenseVersion) {
    Write-Error "nuget-license $installedVersion is installed, but CI uses $nugetLicenseVersion. Run: dotnet tool update --global nuget-license --version $nugetLicenseVersion"
}

Write-Host "Restoring $solution..."
dotnet restore $solution
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Generating third-party notices...'
nuget-license -i $solution -t -exclude-projects '*Tests*' -ignore 'CSharpier.MsBuild' -override $overrides -a $allowedLicenses -o Markdown -fo $noticesFile
if ($LASTEXITCODE -ne 0) {
    Write-Error "nuget-license failed with exit code $LASTEXITCODE. A license outside '$allowedLicenses' is listed as a validation error in $noticesFile."
}

if (Test-Path $packagesDir) {
    Remove-Item $packagesDir -Recurse -Force
}
New-Item -ItemType Directory $packagesDir | Out-Null

Write-Host 'Copying license and notice files shipped inside packages...'
$globalPackages = ((dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', '').Trim()
foreach ($row in Get-Content $noticesFile | Select-Object -Skip 2) {
    $cells = $row.Split('|').Trim()
    $id = $cells[1]
    $version = $cells[2]
    $packageDir = Join-Path $globalPackages "$($id.ToLowerInvariant())/$($version.ToLowerInvariant())"
    $license = ([xml](Get-Content (Join-Path $packageDir "$($id.ToLowerInvariant()).nuspec") -Raw)).package.metadata.license
    $files = @(Get-ChildItem $packageDir -File -Depth 1 | Where-Object Name -Match '^(THIRDPARTY)?NOTICES?(\.|$)')
    if ($license.type -eq 'file') {
        $files += Get-Item (Join-Path $packageDir ($license.'#text' -replace '\\', '/'))
    }
    foreach ($file in $files) {
        Copy-Item $file.FullName (Join-Path $packagesDir "${id}__${version}.$($file.Name)")
    }
    if ($cells[4] -eq 'PostgreSQL') {
        $repoUrl = $cells[8].TrimEnd('/')
        $upstreamFile = Join-Path $upstreamDir "$(Split-Path $repoUrl -Leaf)__${version}.LICENSE"
        if (-not (Test-Path $upstreamFile)) {
            Write-Error "$id $version has a project-specific license text that its package does not ship. Save $($repoUrl -replace '^https://github.com/', 'https://raw.githubusercontent.com/')/v$version/LICENSE as $upstreamFile."
        }
    }
}

Write-Host "Updated $noticesFile and $packagesDir. Commit both."
