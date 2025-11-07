param(
    [string]$SolutionPath = "c:\\Learnings\\PhoneticAnalyzers\\PhoneticAnalyzers.sln"
)

Write-Host "Stopping any running PhoneticAnalyzers.Web processes..." -ForegroundColor Yellow
try {
    $procs = Get-Process -Name "PhoneticAnalyzers.Web" -ErrorAction SilentlyContinue
    if ($procs) {
        $procs | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 1
        Write-Host "Killed $($procs.Count) process(es)." -ForegroundColor Green
    } else {
        Write-Host "No running PhoneticAnalyzers.Web process found." -ForegroundColor DarkGray
    }
}
catch {
    Write-Host "Warning: failed to stop PhoneticAnalyzers.Web: $($_.Exception.Message)" -ForegroundColor DarkYellow
}

# Optional: clear read-only attribute on the exe if any AV tool marked it
$exePath = Join-Path (Split-Path $SolutionPath -Parent) "Web\\bin\\Debug\\net8.0\\PhoneticAnalyzers.Web.exe"
if (Test-Path $exePath) {
    try {
        Attrib -R $exePath -ErrorAction SilentlyContinue | Out-Null
    } catch {}
}

Write-Host "Building solution: $SolutionPath" -ForegroundColor Cyan
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = "build `"$SolutionPath`""
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$proc = [System.Diagnostics.Process]::Start($psi)
$stdout = $proc.StandardOutput.ReadToEnd()
$stderr = $proc.StandardError.ReadToEnd()
$proc.WaitForExit()

Write-Host $stdout
if ($proc.ExitCode -ne 0) {
    Write-Host $stderr -ForegroundColor Red
    Write-Host "Build failed with exit code $($proc.ExitCode)." -ForegroundColor Red
    exit $proc.ExitCode
}

Write-Host "Build succeeded." -ForegroundColor Green
