<#
.SYNOPSIS
    Calculates the next semantic version number based on existing Git tags.
.DESCRIPTION
    This script determines the next patch version for a given service and major/minor version.
    It searches for the highest existing Git tag matching the pattern "ServiceName@Major.Minor.*".

    If no tags are found, it starts the patch version from the specified PatchStart number.
    If tags are found, it calculates the next patch version as the greater of either (latest patch + 1) or the PatchStart number. This allows for starting a new, higher patch number series at any time.

    The calculated version is output to the console and to the GITHUB_OUTPUT environment file if it exists.
.PARAMETER ServiceName
    The name of the service or component. This is used as a prefix for the Git tag.
.PARAMETER MajorVersion
    The major version number.
.PARAMETER MinorVersion
    The minor version number.
.PARAMETER PatchStart
    The starting number for a patch series. The calculated patch will be at least this number.
    Must be a positive integer that is a multiple of 100 (e.g., 100, 200, 1100).
.OUTPUTS
    System.String - The calculated version string is written to the host.
    If the GITHUB_OUTPUT environment variable is set, the version is also appended to that file in the format "version=x.y.z".
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceName,

    [Parameter(Mandatory=$true)]
    [string]$MajorVersion,

    [Parameter(Mandatory=$true)]
    [string]$MinorVersion,

    [Parameter(Mandatory=$true)]
    [ValidateScript({
        if ($_ -ge 100 -and $_ % 100 -eq 0) {
            return $true
        } else {
            throw "PatchStart must be a multiple of 100 and greater than or equal to 100 (e.g., 100, 200, 300)."
        }
    })]
    [int]$PatchStart
)

$tagPrefix = "$($ServiceName)@$($MajorVersion).$($MinorVersion)."

Write-Host "Searching for Git tags with prefix: $($tagPrefix)*"

$latestTag = git tag --list "$($tagPrefix)*" --sort=-v:refname | Select-Object -First 1

if ($null -eq $latestTag) {
    Write-Host "No existing tags found. Starting patch version from $PatchStart."
    $patchVersion = $PatchStart
}
else {
    Write-Host "Latest overall tag found: $latestTag"
    $latestPatchNumber = [int]$latestTag.Split('.')[-1]
    $patchVersion = [Math]::Max($PatchStart, ($latestPatchNumber + 1))
}

$version = "$($MajorVersion).$($MinorVersion).$($patchVersion)"

Write-Host "Calculated version: $version"

if ($env:GITHUB_OUTPUT) {
    "version=$version" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
}
