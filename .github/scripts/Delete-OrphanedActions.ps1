<#
.SYNOPSIS
    A script to delete GitHub Actions workflow runs from a repository.
.DESCRIPTION
    This script retrieves all workflow runs from a specified GitHub repository,
    sorts them by name, allows the user to select runs to delete,
    and then deletes the selected runs.
.PARAMETER Owner
    The owner of the GitHub repository.
.PARAMETER Repo
    The name of the GitHub repository.
.EXAMPLE
    .\Delete-WorkflowRuns.ps1 -Owner "my-username" -Repo "my-awesome-project"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Owner,

    [Parameter(Mandatory = $true)]
    [string]$Repo
)

# Check if GitHub CLI is installed
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI ('gh') is not installed. Please install it from https://cli.github.com/ and authenticate with 'gh auth login'."
    return
}

# Get all workflow runs, including the workflow filename
Write-Host "Fetching workflow runs for '$Owner/$Repo'..."
$workflowRuns = gh api "repos/$Owner/$Repo/actions/runs" --paginate --jq '.workflow_runs[] | {id: .id, name: .name, workflow_file: .path, status: .status, conclusion: .conclusion, created_at: .created_at}' | ConvertFrom-Json

if (-not $workflowRuns) {
    Write-Host "No workflow runs found for '$Owner/$Repo'."
    return
}

# --- User Selection Step ---
# The list is now sorted by the 'name' property before being displayed.
# The -PassThru parameter allows the script to receive the items you select.
#
# HOW TO SELECT:
# - To select multiple runs, hold down the 'Ctrl' key and click each one.
# - To select a range of runs, click the first one, then hold 'Shift' and click the last one.
#
# Click 'OK' to confirm your selection or 'Cancel' to exit.
$runsToDelete = $workflowRuns | Sort-Object -Property name | Out-GridView -Title "Select workflow runs to delete (use Ctrl+Click for multiple)" -PassThru

if (-not $runsToDelete) {
    Write-Host "No workflow runs selected. Exiting."
    return
}

# Delete the selected workflow runs
foreach ($run in $runsToDelete) {
    Write-Host "Deleting workflow run ID $($run.id) ($($run.name))..."
    # The '-X DELETE' flag tells the GitHub API to delete the specified resource.
    gh api "repos/$Owner/$Repo/actions/runs/$($run.id)" -X DELETE
}

Write-Host "All selected workflow runs have been deleted."