[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceName,

    [Parameter(Mandatory=$true)]
    [string]$MajorVersion,

    [Parameter(Mandatory=$true)]
    [string]$MinorVersion
)

$tagPrefix = "$($ServiceName)@$($MajorVersion).$($MinorVersion)."

$latestTag = git tag --list "$($tagPrefix)*" --sort=-v:refname | Select-Object -First 1

if ($null -eq $latestTag) {
    $patchVersion = 100
}
else {
    $latestPatch = $latestTag.Split('.')[-1]
    $patchVersion = [int]$latestPatch + 1
}

$version = "$($MajorVersion).$($MinorVersion).$($patchVersion)"

Write-Host "Calculated version: $version"

if ($env:GITHUB_OUTPUT) {
    "version=$version" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
}