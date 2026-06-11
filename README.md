# SmartComputerControl - Avalonia Cross-Platform

Versione Avalonia di SmartComputerControl. Gira su:
- **Windows**
- **macOS**
- **Linux**

Tray icon nativa su tutte e tre.

---

## Setup iniziale (una volta sola)

### 1. Installa .NET 10 SDK
https://dotnet.microsoft.com/download

Verifica:
```
dotnet --version    →    deve uscire 10.0.x
```

### 2. Installa i template Avalonia
```
dotnet new install Avalonia.Templates
```

### 3. (opzionale) installa estensioni IDE
- **VS Code**: estensioni "C# Dev Kit" + "Avalonia for VS Code"
- **Rider**: supporto Avalonia integrato
- **Visual Studio**: estensione "Avalonia for Visual Studio"

---

## Apertura del progetto

1. Apri la cartella `SmartComputerControlAvalonia` con il tuo editor.
2. Aggiungi un'icona valida (vedi `Assets/LEGGIMI-icona.txt`).
3. Da terminale, in questa cartella:

```
dotnet restore
dotnet build
dotnet run
```

Al primo `dotnet run` parte: vedrai un'icona Avalonia nella tray (Windows) o
nel menu bar (Mac/Linux). Click destro → "Apri impostazioni" per configurare.

---

## Compilare per distribuzione (eseguibile singolo)

### Windows
```
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
Output: `bin/Release/net10.0/win-x64/publish/SmartComputerControl.exe`

### macOS Apple Silicon
```
dotnet publish -c Release -r osx-arm64 --self-contained true \
  -p:PublishSingleFile=true
```
Output: `bin/Release/net10.0/osx-arm64/publish/SmartComputerControl`

### macOS Intel
```
dotnet publish -c Release -r osx-x64 --self-contained true \
  -p:PublishSingleFile=true
```

### Linux
```
dotnet publish -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true
```

---

## Struttura del progetto

```
SmartComputerControlAvalonia/
├── SmartComputerControlAvalonia.csproj   # progetto
├── app.manifest                          # manifest Windows (DPI, ecc.)
├── Program.cs                            # entry point
├── App.axaml / .cs                       # app + tray icon
├── Assets/
│   └── avalonia-logo.ico                 # icona tray (da fornire)
├── Views/
│   └── MainWindow.axaml / .cs            # finestra impostazioni
└── Services/                              # logica di business
    ├── ConfigService.cs                  # config.json
    ├── WatcherService.cs                 # FileSystemWatcher + polling
    ├── ShutdownService.cs                # shutdown cross-platform
    └── AutostartService.cs               # autoavvio cross-platform
```

---

## Dove vengono salvati i dati

Avalonia non ha una sua "AppDataDirectory" come MAUI, quindi uso `Environment.SpecialFolder.ApplicationData`:

| OS | Path |
|---|---|
| Windows | `%APPDATA%\SmartComputerControl\config.json` |
| macOS | `~/Library/Application Support/SmartComputerControl/config.json` |
| Linux | `~/.config/SmartComputerControl/config.json` |

---

## Differenze rispetto a MAUI

| Aspetto | MAUI | Avalonia |
|---|---|---|
| Tray icon | Libreria esterna | **Nativa, integrata** |
| Linux desktop | Non supportato | **Supportato ufficialmente** |
| Folder picker | Da implementare | Nativo (StorageProvider) |
| Footprint | ~150 MB | ~70 MB |
| Cold start | Lento | Veloce |

---

## Modalita' debug

In `App.axaml.cs`, nella `OnFrameworkInitializationCompleted`, puoi aggiungere:
```csharp
Shutdown.DebugMode = true;
```
Cosi il PC non viene spento davvero, esce solo un messaggio nella console.

---

## Note macOS

Al primo spegnimento, macOS chiedera' di autorizzare l'app a controllare
"System Events". Confermare una volta.

## Note Linux

Per spegnere senza password serve aggiungere una regola sudoers:
```
echo "$USER ALL=(ALL) NOPASSWD: /sbin/shutdown" | sudo tee /etc/sudoers.d/scc
```
