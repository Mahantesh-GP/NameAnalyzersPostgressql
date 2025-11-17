# Runs deploy with environment variables or explicit parameters
$ErrorActionPreference = 'Stop'

$DbHost = if ($env:PGHOST) { $env:PGHOST } else { 'localhost' }
$DbPort = if ($env:PGPORT) { [int]$env:PGPORT } else { 5432 }
$DbName = if ($env:PGDATABASE) { $env:PGDATABASE } else { 'phonetic_native' }
$DbUser = if ($env:PGUSER) { $env:PGUSER } else { 'postgres' }
$DbPassword = if ($env:PGPASSWORD) { $env:PGPASSWORD } else { 'postgres' }

$script = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) 'deploy.ps1'

& $script -DbHost $DbHost -DbPort $DbPort -DbName $DbName -DbUser $DbUser -DbPassword $DbPassword
