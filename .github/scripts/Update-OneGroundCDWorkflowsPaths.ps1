$rootDir = Resolve-Path "$PSScriptRoot/../../";
$cfgFile = Join-Path $rootDir ".github/_pipelines.config.json"

Write-Host "Reading config from $cfgFile..."
$cfg = Get-Content -Path $cfgFile -ErrorAction Stop | ConvertFrom-Json
$pipelines = $cfg.AutoGeneratingIncludePipelines
Write-Host "Found $($pipelines.Length) auto-generating include pipelines."

for ($i = 0; $i -lt $pipelines.Length; $i++) {
    $logPrefix = "[$($i + 1)/$($pipelines.Length)]";
    Write-Host "$logPrefix Updating pipeline $($pipelines[$i])..."
    $path = Join-Path $rootDir $pipelines[$i] -Resolve
    & $PSScriptRoot/Update-OneGroundCDWorkflowsPathsFromCsprojRefs.ps1 -path $path -rootDir $rootDir
    if (-not $?) {
        exit 1
    }
    Write-Host "$logPrefix $($pipelines[$i]) completed."
}

Write-Host "All pipelines updated."
