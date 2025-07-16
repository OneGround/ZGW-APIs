param(
    [Parameter(Mandatory = $true)]
    [string]$path,
    [Parameter(Mandatory = $true)]
    [string]$rootDir = $null,
    [Parameter(Mandatory = $false)]
    [switch]$validateOnly = $false
)

$GEN_HEADER = "## GENERATED PATH LIST, DO NOT MODIFY CONTENT MANUALLY ##"
$GEN_FOOTER = "## END OF GENERATED PATH LIST ##"
$GEN_CMD_PREFIX = "## > "
$GEN_HELP = @"
--------------------------------------------------------------
... [YAML content] ...

on:
  push:
    paths:
$($GEN_HEADER)
$($GEN_CMD_PREFIX)<CSPROJ FILE PATH>
$($GEN_FOOTER)

... [YAML content] ...
--------------------------------------------------------------
"@
$GEN_PATH_LINE = "      - {0}"

function GetDotNetReferences([string]$csprojPath, [System.Collections.ArrayList]$skip = $null) {
    if ($null -ne $skip -and $skip -contains $csProjPath) {
        return $null
    }

    if ($null -eq $skip) {
        $skip = New-Object System.Collections.ArrayList
    }

    $stdout = dotnet list $csProjPath reference | Select-Object -Skip 2
    [void]$skip.Add($csProjPath)
    if ($stdout.Length -eq 0) {
        return $null
    }

    $parentPath = Split-Path -Parent $csProjPath
    $selfRefs = $stdout | ForEach-Object { Join-Path $parentPath $_ -Resolve }
    if ($selfRefs.Length -eq 0) {
        return $null
    }

    $projectRefs = New-Object System.Collections.ArrayList
    [void]$projectRefs.Add($csprojPath)
    foreach ($ref in $selfRefs) {
        [void]$projectRefs.Add($ref)

        $projectRefsDeep = GetDotNetReferences -csprojPath $ref -skip $skip
        if ($null -ne $projectRefsDeep) {
            foreach ($deepRef in $projectRefsDeep) {
                if (!$projectRefs.Contains($deepRef)) {
                    [void]$projectRefs.Add($deepRef)
                }
            }
        }
    }

    return $projectRefs | Sort-Object
}

function Search-ContentLines($content) {
    $lines = @{
        start = -1
        end   = -1
        cmd   = New-Object System.Collections.ArrayList
    }

    for ($i = 0; $i -lt $content.Length; $i++) {
        if ($content[$i] -eq $GEN_HEADER) {
            $lines.start = $i
            continue
        }

        if ($content[$i] -eq $GEN_FOOTER) {
            $lines.end = $i
            break
        }
    }

    if ($lines.start -ne -1 -and $lines.end -ne -1) {
        for ($i = $lines.start + 1; $i -lt $lines.end; $i++) {
            if (!$content[$i].StartsWith($GEN_CMD_PREFIX)) {
                break;
            }

            [void]$lines.cmd.Add($i)
        }
    }

    return $lines
}

function Export-CsProjFromCmdLine([string]$line) {
    if (!$line.StartsWith($GEN_CMD_PREFIX)) {
        return $null
    }

    return $line.Substring($GEN_CMD_PREFIX.Length)
}

function Resolve-RefsToPathList([array]$refs, [string]$root) {
    $paths = New-Object System.Collections.ArrayList

    $root = [System.IO.Path]::GetFullPath($root) -replace '\\', '/'
    foreach ($ref in $refs) {
        $refDir = Split-Path -Parent $ref
        $path = Get-RelativePath -rootPath $root -filePath $refDir

        $path = $path -replace '\\', '/'
        if (!$path.EndsWith("/")) {
            $path += "/"
        }
        $path += "**"
        [void]$paths.Add("'" + $path + "'")
    }

    return $paths;
}

function Format-OutputContent($content, $lines, [array]$paths) {
    $output = New-Object System.Collections.ArrayList
    $pointer = $lines.start + 1 + $lines.cmd.Count

    for ($i = 0; $i -lt $pointer; $i++) {
        [void]$output.Add($content[$i])
    }

    foreach ($path in $paths) {
        [void]$output.Add($GEN_PATH_LINE -f $path)
        $pointer++
    }

    for ($i = $lines.end; $i -lt $content.Length; $i++) {
        [void]$output.Add($content[$i])
    }

    return $output
}

function Get-RelativePath {
    param (
        [string]$rootPath,
        [string]$filePath
    )

    $normalizedRootPath = $rootPath -replace '\\', '/'
    $normalizedFilePath = $filePath -replace '\\', '/'

    if (-not $normalizedRootPath.EndsWith('/')) {
        $normalizedRootPath += '/'
    }

    return $normalizedFilePath.Substring($normalizedRootPath.Length)
}
function Compare-OutputContent($content, $lines, [array]$paths) {
    $start = $lines.start + 1 + $lines.cmd.Count

    $contentObj = @()
    if ($start -lt $lines.end) {
        $end = $lines.end - 1
        $contentObj = $content[$start..$end]
    }

    $pathObj = @()
    if ($paths.Count -gt 0) {
        $pathObj = $paths | ForEach-Object { $GEN_PATH_LINE -f $_ }
    }

    return Compare-Object -ReferenceObject $pathObj -DifferenceObject $contentObj
}

function Update-OneGroundCDWorkflowPathsFromCsprojRefs([string]$yamlFilePath, [string]$rootDir, [bool]$validateOnly) {
    Write-Host "Starting to update path list in $yamlFilePath YAML workflow file..."

    Write-Host "Reading content from $yamlFilePath..."
    $content = Get-Content -Path $yamlFilePath -ErrorAction Stop
    Write-Host "Searching for generated header and footer in $yamlFilePath..."
    $lines = Search-ContentLines -content $content

    if ($lines.start -eq -1) {
        Write-Error "Could not find generated header in $yamlFilePath. Please add header and footer to YAML workflow file. Exiting..."
        Write-Host "Please add the following lines to the YAML workflow file after on.push.paths line to enable automatic generation of the path list:"
        Write-Host $GEN_HELP
        exit 1
    }

    if ($lines.end -eq -1) {
        Write-Error "Could not find generated footer in $yamlFilePath. Please add footer to YAML workflow file.. Exiting..."
        Write-Host "Please add the following lines to the YAML workflow file after on.push.paths line to enable automatic generation of the path list:"
        Write-Host $GEN_HELP
        exit 1
    }

    if ($lines.cmd.Count -eq 0) {
        Write-Error "Could not find any command line in $yamlFilePath. Exiting..."
        Write-Host $GEN_HELP
        exit 1
    }

    $csProjPaths = New-Object System.Collections.ArrayList
    foreach ($cmdLine in $lines.cmd) {
        $csProjPath = Export-CsProjFromCmdLine -line $content[$cmdLine]
        if ($null -eq $csProjPath) {
            Write-Error "Could not find command line in $($yamlFilePath):$cmdLine. Exiting..."
            Write-Host $GEN_HELP
            exit 1C:\dev\ZGW_APIs\builds\builds\zgw-autorisaties.docker-build.yml
        }

        if (![System.IO.Path]::IsPathRooted($csProjPath)) {
            $csProjPath = Join-Path $rootDir $csProjPath -Resolve -ErrorAction Stop
        }

        if (-not (Test-Path $csProjPath)) {
            Write-Error "Could not find csproj file from $($yamlFilePath):$cmdLine at path: $csProjPath. Exiting..."
            Write-Host "Please ensure that the path is correct in the YAML workflow file comment line."
            Write-Host $GEN_HELP
            exit 1
        }

        [void]$csProjPaths.Add($csProjPath)
    }

    Write-Host "Found $($csProjPaths.Count) csproj files in $yamlFilePath."

    $refs = New-Object System.Collections.ArrayList
    foreach ($csProjPath in $csProjPaths) {
        Write-Host "Reading references from $csProjPath..."
        $projectRefs = GetDotNetReferences -csprojPath $csProjPath

        Write-Host "Found $($projectRefs.Count) references in $csProjPath."
        foreach ($projectRef in $projectRefs) {
            if (!$refs.Contains($projectRef)) {
                [void]$refs.Add($projectRef)
            }
        }
    }
    $refs = $refs | Sort-Object

    Write-Host "Found $($refs.Count) unique references in all csproj files."
    $paths = Resolve-RefsToPathList -refs $refs -root $rootDir

    if ($validateOnly) {
        Write-Host "Validating path list in $yamlFilePath..."
        $comparisonResult = Compare-OutputContent -content $content -lines $lines -paths $paths

        if ($comparisonResult.Length -eq 0) {
            Write-Host "Path list in $yamlFilePath is up-to-date."
        }
        else {
            Write-Host "Path list in $yamlFilePath is outdated. Please update the path list in the YAML workflow file."
            $comparisonResult | Format-Table -Property InputObject, SideIndicator -AutoSize
            exit 2
        }
    }
    else {
        Write-Host "Formatting output content..."
        $output = Format-OutputContent -content $content -lines $lines -paths $paths
        Write-Host "Writing updated content to $yamlFilePath..."
        Set-Content -Path $yamlFilePath -Value $output
    }
}

if ($rootDir -eq $null) {
    $rootDir = Resolve-Path "$PSScriptRoot/../../" -ErrorAction Stop
}

if (![System.IO.Path]::IsPathRooted($path)) {
    $path = Join-Path $rootDir $path -Resolve -ErrorAction Stop
}
else {
    Resolve-Path $path -ErrorAction Stop
}

Update-OneGroundCDWorkflowPathsFromCsprojRefs -yamlFilePath $path -rootDir $rootDir -validateOnly $validateOnly
