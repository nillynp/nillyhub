# ============================================================================
# NillyHub — Clean Uninstaller (Zero Admin)
# created by Nilly graphics (Sotiris Christos) — https://nillygraphics.com/
# ============================================================================
param([switch]$Silent)

$ErrorActionPreference = 'SilentlyContinue'
Write-Host ''
Write-Host '  Αφαίρεση συντομεύσεων του NillyHub...' -ForegroundColor Yellow

Stop-Process -Name 'NillyHub' -Force -ErrorAction SilentlyContinue

$desktopLnk = Join-Path ([Environment]::GetFolderPath('Desktop')) 'NillyHub.lnk'
$startLnk = Join-Path ([Environment]::GetFolderPath('Programs')) 'Nilly graphics\NillyHub.lnk'
if (Test-Path $desktopLnk) { Remove-Item $desktopLnk -Force }
if (Test-Path $startLnk) { Remove-Item $startLnk -Force }

Write-Host '  OK -> Αφαιρέθηκαν καθαρά οι συντομεύσεις του NillyHub (τα εργαλεία σου έμειναν άθικτα).' -ForegroundColor Green
Write-Host ''
if (-not $Silent) { Read-Host '  Πάτησε Enter για κλείσιμο' }
