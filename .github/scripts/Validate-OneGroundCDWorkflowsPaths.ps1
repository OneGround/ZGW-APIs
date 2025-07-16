$rootDir = Resolve-Path "$PSScriptRoot/../../";
$cfgFile = Join-Path $rootDir ".github/_workflows.config.json"

Write-Host "Reading config from $cfgFile..."
$cfg = Get-Content -Path $cfgFile -ErrorAction Stop | ConvertFrom-Json
$workflows = $cfg.AutoGeneratingWorkflowPaths
Write-Host "Found $($workflows.Length) auto-generating path workflows."

for ($i = 0; $i -lt $workflows.Length; $i++) {
    $logPrefix = "[$($i + 1)/$($workflows.Length)]";
    Write-Host "$logPrefix Validating workflow $($workflows[$i])..."
    $path = Join-Path $rootDir $workflows[$i] -Resolve
    & $PSScriptRoot/Update-OneGroundCDWorkflowsPathsFromCsprojRefs.ps1 -path $path -rootDir $rootDir -validateOnly
    if (-not $?) {
        Write-Error "Content does not matched. Please run 'Update-OneGroundCDWorkflowsPaths.ps1' to update the workflows."
        exit 1
    }
    Write-Host "$logPrefix $($workflows[$i]) completed."
}

Write-Host "All workflows validated."
