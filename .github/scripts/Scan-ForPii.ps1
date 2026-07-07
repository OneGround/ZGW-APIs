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

# Resolve and validate before handing it to an external process — fail closed on a bad/missing path
# rather than passing an unvalidated string through to `git -C`.
try {
    $Root = (Resolve-Path -LiteralPath $Root -ErrorAction Stop).Path
}
catch {
    Write-Host "::error::Root path '$Root' does not exist or is not accessible: $($_.Exception.Message)"
    exit 1
}

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

# Build an explicit argument array (no shell interpolation) and validate that the resolved
# $Root is a git work tree before running against it. On PowerShell 7.4+ a native command
# that exits nonzero throws under $ErrorActionPreference='Stop' (via
# $PSNativeCommandUseErrorActionPreference), so catch that; the $LASTEXITCODE check remains
# for hosts where that preference is off.
$gitArgs = @('-C', $Root, 'ls-files', '--', 'src/**', 'docs/**')
try {
    $files = & git @gitArgs | Where-Object { $_ }
}
catch {
    Write-Host "::error::git ls-files failed in '$Root': $($_.Exception.Message) — not a git work tree?"
    exit 1
}
if ($LASTEXITCODE -ne 0) {
    Write-Host "::error::git ls-files failed in '$Root' (exit $LASTEXITCODE) — not a git work tree?"
    exit 1
}
$violations = [System.Collections.Generic.List[string]]::new()

foreach ($f in $files) {
    $full = Join-Path $Root $f
    if (-not (Test-Path -LiteralPath $full)) { continue }
    try {
        $text = Get-Content -LiteralPath $full -Raw -ErrorAction Stop
    }
    catch {
        # A CI gate that silently skips files it can't read can go green without actually
        # having scanned them — treat a read failure as a violation instead.
        $violations.Add(("{0}: could not be read for PII scan ({1})" -f $f, $_.Exception.Message))
        continue
    }
    if ([string]::IsNullOrEmpty($text)) { continue }

    $lineNo = 0
    foreach ($line in ($text -split "`n")) {
        $lineNo++
        # \b boundaries treat letters/underscore as part of the token, so a 9-digit run
        # embedded in a hex/GUID/UUID is NOT mistaken for a standalone BSN/RSIN. Genuine
        # leaks are quote/space/colon-delimited and still match.
        foreach ($m in [regex]::Matches($line, '\b\d{9}\b')) {
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
