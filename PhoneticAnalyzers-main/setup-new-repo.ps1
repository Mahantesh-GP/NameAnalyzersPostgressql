#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Setup script to connect this repository to a new GitHub remote

.DESCRIPTION
    This script will help you set up this cloned repository with a new GitHub remote URL.
    Run this after creating a new repository on GitHub.

.PARAMETER NewRepoUrl
    The URL of your new GitHub repository (e.g., https://github.com/Mahantesh-GP/PhoneticAnalyzers-Production.git)

.EXAMPLE
    .\setup-new-repo.ps1 -NewRepoUrl "https://github.com/Mahantesh-GP/PhoneticAnalyzers-Production.git"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$NewRepoUrl
)

Write-Host "🔧 Setting up new repository connection..." -ForegroundColor Cyan
Write-Host ""

# Validate URL format
if ($NewRepoUrl -notmatch '^https://github\.com/.+/.+\.git$') {
    Write-Host "❌ Invalid repository URL format!" -ForegroundColor Red
    Write-Host "Expected format: https://github.com/username/repo-name.git" -ForegroundColor Yellow
    exit 1
}

Write-Host "Current remote:" -ForegroundColor Yellow
git remote -v
Write-Host ""

Write-Host "Removing old remote 'origin'..." -ForegroundColor Yellow
git remote remove origin

Write-Host "Adding new remote 'origin' -> $NewRepoUrl" -ForegroundColor Green
git remote add origin $NewRepoUrl

Write-Host ""
Write-Host "Updated remote:" -ForegroundColor Yellow
git remote -v
Write-Host ""

Write-Host "✅ Remote updated successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Verify your changes: git status" -ForegroundColor White
Write-Host "2. Push to new repository: git push -u origin main" -ForegroundColor White
Write-Host "3. Verify on GitHub: $($NewRepoUrl -replace '\.git$', '')" -ForegroundColor White
Write-Host ""
Write-Host "Your original repository at https://github.com/Mahantesh-GP/PhoneticAnalyzers.git is unchanged." -ForegroundColor Gray
