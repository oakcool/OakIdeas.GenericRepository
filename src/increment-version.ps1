#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automatically increments the version in Directory.Build.props
    
.DESCRIPTION
    This script reads the current version from Directory.Build.props,
    increments the patch version, and updates the file.
    
.PARAMETER IncrementType
    The type of version increment: Major, Minor, or Patch (default)
    
.PARAMETER Suffix
    The version suffix (e.g., alpha, beta, rc1). If not specified, keeps current suffix.
    
.PARAMETER RemoveSuffix
    Remove the version suffix to create a stable release
    
.EXAMPLE
    ./increment-version.ps1
    Increments patch version: 0.0.6-alpha -> 0.0.7-alpha
    
.EXAMPLE
    ./increment-version.ps1 -IncrementType Minor
    Increments minor version: 0.0.6-alpha -> 0.1.0-alpha
    
.EXAMPLE
    ./increment-version.ps1 -Suffix beta
    Changes suffix: 0.0.6-alpha -> 0.0.7-beta
    
.EXAMPLE
    ./increment-version.ps1 -RemoveSuffix
    Creates stable version: 0.0.6-alpha -> 0.0.7
#>

param(
    [Parameter()]
    [ValidateSet('Major', 'Minor', 'Patch')]
    [string]$IncrementType = 'Patch',
    
    [Parameter()]
    [string]$Suffix,
    
    [Parameter()]
    [switch]$RemoveSuffix
)

$ErrorActionPreference = 'Stop'

# Find Directory.Build.props
$propsFile = Join-Path $PSScriptRoot 'Directory.Build.props'
if (-not (Test-Path $propsFile)) {
    Write-Error "Directory.Build.props not found at: $propsFile"
    exit 1
}

Write-Host "📄 Reading version from: $propsFile" -ForegroundColor Cyan

# Read the file
$content = Get-Content $propsFile -Raw
$originalContent = $content

# Extract current version
if ($content -match '<VersionPrefix[^>]*>([^<]+)</VersionPrefix>') {
    $currentVersion = $matches[1]
    Write-Host "📌 Current version: $currentVersion" -ForegroundColor Gray
} else {
    Write-Error "Could not find VersionPrefix in Directory.Build.props"
    exit 1
}

# Extract current suffix
$currentSuffix = ''
if ($content -match '<VersionSuffix[^>]*>([^<]+)</VersionSuffix>') {
    $currentSuffix = $matches[1]
    Write-Host "📌 Current suffix: $currentSuffix" -ForegroundColor Gray
}

# Parse version
if ($currentVersion -match '^(\d+)\.(\d+)\.(\d+)$') {
    $major = [int]$matches[1]
    $minor = [int]$matches[2]
    $patch = [int]$matches[3]
} else {
    Write-Error "Invalid version format: $currentVersion. Expected format: X.Y.Z"
    exit 1
}

# Increment version
switch ($IncrementType) {
    'Major' {
        $major++
        $minor = 0
        $patch = 0
        Write-Host "⬆️  Incrementing MAJOR version" -ForegroundColor Yellow
    }
    'Minor' {
        $minor++
        $patch = 0
        Write-Host "⬆️  Incrementing MINOR version" -ForegroundColor Yellow
    }
    'Patch' {
        $patch++
        Write-Host "⬆️  Incrementing PATCH version" -ForegroundColor Yellow
    }
}

$newVersion = "$major.$minor.$patch"
Write-Host "✨ New version: $newVersion" -ForegroundColor Green

# Update version in content
$content = $content -replace '<VersionPrefix[^>]*>[^<]+</VersionPrefix>', "<VersionPrefix>$newVersion</VersionPrefix>"

# Handle suffix
if ($RemoveSuffix) {
    Write-Host "🗑️  Removing version suffix" -ForegroundColor Yellow
    $content = $content -replace '<VersionSuffix[^>]*>[^<]*</VersionSuffix>', '<VersionSuffix></VersionSuffix>'
    $newSuffix = ''
} elseif ($PSBoundParameters.ContainsKey('Suffix')) {
    Write-Host "🏷️  Setting suffix to: $Suffix" -ForegroundColor Yellow
    $content = $content -replace '<VersionSuffix[^>]*>[^<]*</VersionSuffix>', "<VersionSuffix>$Suffix</VersionSuffix>"
    $newSuffix = $Suffix
} else {
    $newSuffix = $currentSuffix
}

# Display final version
$finalVersion = if ($newSuffix) { "$newVersion-$newSuffix" } else { $newVersion }
Write-Host ""
Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Final Version: $finalVersion" -ForegroundColor Green -NoNewline
Write-Host " 🎉" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Write back to file
Set-Content -Path $propsFile -Value $content -NoNewline

Write-Host "✅ Updated Directory.Build.props" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Review the changes: git diff Directory.Build.props" -ForegroundColor Gray
Write-Host "  2. Commit: git add Directory.Build.props && git commit -m 'Bump version to $finalVersion'" -ForegroundColor Gray
Write-Host "  3. Push: git push origin master" -ForegroundColor Gray
Write-Host ""
Write-Host "Or create a tag for immediate release:" -ForegroundColor Cyan
Write-Host "  git tag v$finalVersion && git push origin v$finalVersion" -ForegroundColor Gray
Write-Host ""
