# =============================================================
# build-windows-installer.ps1
# Compila e crea l'installer Windows in un solo colpo.
# =============================================================
# Prerequisiti:
#   1. Avere installato Inno Setup (https://jrsoftware.org/isdl.php)
#   2. Eseguire questo script in PowerShell dalla cartella del progetto
# =============================================================

$ErrorActionPreference = "Stop"

Write-Host "[BUILD] SmartComputerControl per Windows x64..." -ForegroundColor Cyan

# 1. Pulisci output precedenti
Write-Host "[1/4] Pulizia output precedenti..." -ForegroundColor Yellow
if (Test-Path "publish-win-x64") { Remove-Item "publish-win-x64" -Recurse -Force }
if (Test-Path "installer-output") { Remove-Item "installer-output" -Recurse -Force }

# 2. dotnet publish (self-contained, single file)
Write-Host "[2/4] Pubblicazione .NET (potrebbe richiedere qualche minuto)..." -ForegroundColor Yellow
dotnet publish -f net10.0-windows -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o publish-win-x64

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRORE: Publish fallita" -ForegroundColor Red
    exit 1
}

# 3. Trova l'eseguibile di Inno Setup
Write-Host "[3/4] Ricerca Inno Setup..." -ForegroundColor Yellow
$innoSetup = $null
$candidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe"
)
foreach ($c in $candidates) {
    if (Test-Path $c) { $innoSetup = $c; break }
}

if (-not $innoSetup) {
    Write-Host "ERRORE: Inno Setup non trovato!" -ForegroundColor Red
    Write-Host "  Scarica e installa da: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

Write-Host "  Trovato: $innoSetup" -ForegroundColor Green

# 4. Lancia Inno Setup Compiler
Write-Host "[4/4] Generazione installer con Inno Setup..." -ForegroundColor Yellow
& $innoSetup "installer.iss"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRORE: Inno Setup fallito" -ForegroundColor Red
    exit 1
}

# 5. Mostra risultato
$installerFile = Get-ChildItem "installer-output\*.exe" | Select-Object -First 1
if ($installerFile) {
    $sizeMB = [math]::Round($installerFile.Length / 1MB, 1)
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Green
    Write-Host "OK Installer creato:" -ForegroundColor Green
    Write-Host "   $($installerFile.FullName)" -ForegroundColor White
    Write-Host "   Dimensione: ${sizeMB} MB" -ForegroundColor White
    Write-Host "============================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Puoi caricarlo sul tuo sito web e distribuirlo agli amici." -ForegroundColor Cyan
} else {
    Write-Host "AVVISO: Build completata ma installer non trovato in installer-output\" -ForegroundColor Yellow
}
