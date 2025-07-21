<#
.SYNOPSIS
    Calculates the next semantic version number based on existing Git tags.
.DESCRIPTION
    This script determines the next patch version for a given service and major/minor version.
    It searches for the highest existing Git tag matching the pattern "GitTagName@Major.Minor.*".

    If no tags are found, it starts the patch version from the specified PatchVersionStartsFrom number.
    If tags are found, it calculates the next patch version as the greater of either (latest patch + 1) or the PatchVersionStartsFrom number. This allows for starting a new, higher patch number series at any time.

    The calculated version is output to the console and to the GITHUB_OUTPUT environment file if it exists.
.PARAMETER GitTagName
    The Git tag name.
.PARAMETER MajorVersion
    The major version number.
.PARAMETER MinorVersion
    The minor version number.
.PARAMETER PatchVersionStartsFrom
    The starting number for a patch series. The calculated patch will be at least this number.
    Must be a positive integer that is a multiple of 100 (e.g., 100, 200, 1100).
.OUTPUTS
    - specific_version: The full version string, including the patch number (e.g., "1.2.101").
    - floating_version: The version string containing only the major and minor numbers (e.g., "1.2").
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$GitTagName,

    [Parameter(Mandatory=$true)]
    [string]$MajorVersion,

    [Parameter(Mandatory=$true)]
    [string]$MinorVersion,

    [Parameter(Mandatory=$true)]
    [ValidateScript({ Validate-PatchVersionStartsFrom($_) })]
    [int]$PatchVersionStartsFrom
)

$tagPrefix = "$($GitTagName)@$($MajorVersion).$($MinorVersion)."
Write-Host "Searching for Git tags with prefix: $($tagPrefix)*"

$latestTag = git tag --list "$($tagPrefix)*" --sort=-v:refname | Select-Object -First 1

if ($null -eq $latestTag) {
    Write-Host "No existing tags found. Starting patch version from $PatchVersionStartsFrom."
    $patchVersion = $PatchVersionStartsFrom
}
else {
    Write-Host "Latest overall tag found: $latestTag"
    $latestPatchNumber = [int]$latestTag.Split('.')[-1]
    $patchVersion = [Math]::Max($PatchVersionStartsFrom, ($latestPatchNumber + 1))
}

$specificVersion = "$($MajorVersion).$($MinorVersion).$($patchVersion)"
$floatingVersion = "$($MajorVersion).$($MinorVersion)"

Write-Host "Specific version: $specificVersion"
Write-Host "Floating version: $floatingVersion"

if ($env:GITHUB_OUTPUT) {
    Write-Host "Setting GitHub Actions outputs..."
    "specific_version=$specificVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    "floating_version=$floatingVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    Write-Host "Outputs set."
}

function Validate-PatchVersionStartsFrom {
    param(
        [int]$PatchVersion
    )

    if ($PatchVersion -ge 100 -and $PatchVersion % 100 -eq 0) {
        return $true
    }
    else {
        throw "PatchVersionStartsFrom must be a multiple of 100 and greater than or equal to 100 (e.g., 100, 200, 300)."
    }
}