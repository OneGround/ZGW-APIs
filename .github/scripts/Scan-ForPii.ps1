#requires -Version 7.0
<#
.SYNOPSIS
    Fail if any tracked source/doc file contains a real-looking Dutch BSN/RSIN.

.DESCRIPTION
    Public-repository hygiene gate: a standalone 9-digit number that passes the elfproef
    (11-test) and is NOT an officially designated public test value is treated as a potential
    real citizen/organization identifier and fails the build.

    This check is pattern-only and contains no internal secrets, so it is safe to live in this
    public repository. (Internal code-name checks are intentionally NOT here — publishing that
    blocklist would leak it; those are enforced privately upstream of this repo.)

.PARAMETER Root
    Repository root. Defaults to two levels up from this script (.github/scripts).
#>
param(
    [string]$Root = (Resolve-Path "$PSScriptRoot/../..").Path
)
$ErrorActionPreference = 'Stop'

# Officially designated PUBLIC test values (RvIG "Testset persoonslijsten" / BRP-API proefomgeving).
# These are government-published and safe to appear anywhere. Keep in sync with the RvIG dataset.
$safe = @(
    '999993653', '999990482', '999993586', '999990561', '999993847', '999990639',
    '999999990', '999999989', '999999928', '999999916', '999999904',
    '000001407', '000001419'
)
$safeSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$safe)

function Test-Elfproef([string]$n) {
    if ($n.Length -ne 9) { return $false }
    $sum = 0
    for ($i = 0; $i -lt 8; $i++) { $sum += (9 - $i) * [int][string]$n[$i] }
    $sum -= [int][string]$n[8]
    return ($sum % 11 -eq 0 -and $sum -ne 0)
}

$files = & git -C $Root ls-files -- 'src/**' 'docs/**' | Where-Object { $_ }
$violations = [System.Collections.Generic.List[string]]::new()

foreach ($f in $files) {
    $full = Join-Path $Root $f
    if (-not (Test-Path -LiteralPath $full)) { continue }
    $text = Get-Content -LiteralPath $full -Raw -ErrorAction SilentlyContinue
    if ([string]::IsNullOrEmpty($text)) { continue }

    $lineNo = 0
    foreach ($line in ($text -split "`n")) {
        $lineNo++
        foreach ($m in [regex]::Matches($line, '(?<!\d)\d{9}(?!\d)')) {
            $n = $m.Value
            if ((Test-Elfproef $n) -and -not $safeSet.Contains($n)) {
                $violations.Add(("{0}:{1}: {2}" -f $f, $lineNo, $n))
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "::error::Potential real Dutch BSN/RSIN values found (9-digit, pass the elfproef, not official test values). Replace with an official test value or move the logic to the private layer:"
    foreach ($v in $violations) { Write-Host "::error::$v" }
    exit 1
}

Write-Host "PII scan passed: no real-looking BSN/RSIN found in tracked src/ or docs/ files."
exit 0
