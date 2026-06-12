@echo off
REM ============================================================
REM RunSmartComputerControl.bat
REM Menu unico per le operazioni Windows.
REM Funziona sia se messo nel progetto sia sul Desktop.
REM Su Windows si lavora SUI FILE LOCALI: niente git pull.
REM ============================================================

setlocal enabledelayedexpansion

REM Cartella dove si trova questo .bat
set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

REM Se il .bat e' dentro la cartella del progetto, lo usa direttamente;
REM altrimenti (es. sul Desktop) cerca in Documents\PROGRAMMAZIONE
if exist "%SCRIPT_DIR%\SmartComputerControlAvalonia.csproj" (
    set "PROJ=%SCRIPT_DIR%"
) else (
    set "PROJ=%USERPROFILE%\Documents\PROGRAMMAZIONE\SmartComputerControlAvalonia"
)

if not exist "%PROJ%" (
    echo Cartella progetto non trovata: %PROJ%
    pause
    exit /b 1
)

cd /d "%PROJ%"

:menu
cls
echo ============================================================
echo          SmartComputerControl - Menu di gestione
echo ============================================================
echo.
echo   1^) Compila e avvia ^(debug^)
echo   2^) Crea installer distribuibile ^(.exe^) e committa su git
echo   3^) Installa l'installer appena creato
echo   4^) Esci
echo.
set /p SCELTA="Scelta [1-4]: "

if "%SCELTA%"=="1" goto opzione1
if "%SCELTA%"=="2" goto opzione2
if "%SCELTA%"=="3" goto opzione3
if "%SCELTA%"=="4" exit /b 0
goto menu

:opzione1
echo.
echo ==^> Compilo...
dotnet build -f net10.0-windows -c Debug
if errorlevel 1 (
    echo.
    echo ^>^>^> Compilazione FALLITA.
    pause
    exit /b 1
)
echo.
echo ==^> Avvio l'app...
start "" "bin\Debug\net10.0-windows\SmartComputerControl.exe"
echo.
echo OK Fatto.
pause
exit /b 0

:opzione2
echo.
echo ==^> Genero l'installer...
powershell -ExecutionPolicy Bypass -File ".\build-windows-installer.ps1"
if errorlevel 1 (
    echo.
    echo ^>^>^> Compilazione FALLITA. Niente commit/push.
    pause
    exit /b 1
)
echo.
echo ==^> Verifico stato git...
git status -s
echo.
git status | findstr /C:"nothing to commit" >nul
if not errorlevel 1 (
    echo Nessuna modifica da committare. Build riuscita ma niente da pushare.
    echo Apro la cartella installer in Esplora Risorse...
    start "" "installer-output"
    pause
    exit /b 0
)
echo.
set /p MSG="Messaggio di commit (Invio per saltare commit/push): "
if "%MSG%"=="" (
    echo Salto commit/push. Apro la cartella installer in Esplora Risorse...
    start "" "installer-output"
    pause
    exit /b 0
)
echo.
echo ==^> git add ^&^& commit ^&^& push ...
git add .
git commit -m "%MSG%"
git push
if errorlevel 1 (
    echo.
    echo ^>^>^> Push FALLITO. Controlla l'output sopra.
    pause
    exit /b 1
)
echo.
echo OK Build + commit + push completati!
echo Apro la cartella installer in Esplora Risorse...
start "" "installer-output"
pause
exit /b 0

:opzione3
echo.
set "INSTALLER="
for /f "delims=" %%I in ('dir /b /o-d "installer-output\*.exe" 2^>nul') do (
    if not defined INSTALLER set "INSTALLER=installer-output\%%I"
)

if not defined INSTALLER (
    echo Nessun installer trovato in installer-output\
    echo Esegui prima l'opzione 2 per generare l'installer.
    pause
    goto menu
)

echo Trovato installer: %INSTALLER%
echo.
echo ==^> Chiudo l'app se in esecuzione...
taskkill /F /IM SmartComputerControl.exe 2>nul
echo.
echo ==^> Lancio l'installer...
echo    ^(Windows SmartScreen potrebbe avvisare: "Maggiori info" -^> "Esegui comunque"^)
echo.
start "" "%INSTALLER%"
echo.
echo OK Installer avviato. Segui le istruzioni a schermo.
pause
exit /b 0
