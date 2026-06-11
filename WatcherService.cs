using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartComputerControlAvalonia.Services;

public class WatcherService
{
    private readonly ConfigService _config;
    private readonly ShutdownService _shutdown;
    private readonly NotificationService _notifications;
    private FileSystemWatcher? _watcher;
    private System.Timers.Timer? _pollTimer;
    private DateTime _avvio = DateTime.Now;

    public WatcherService(ConfigService config, ShutdownService shutdown, NotificationService notifications)
    {
        _config = config;
        _shutdown = shutdown;
        _notifications = notifications;
    }

    public void Start()
    {
        _avvio = DateTime.Now;
        if (string.IsNullOrEmpty(_config.Path) || !Directory.Exists(_config.Path))
            return;

        Task.Run(() => PulisciFileVecchi(_config.Path));

        _watcher = new FileSystemWatcher(_config.Path)
        {
            Filter = "*.txt",
            IncludeSubdirectories = true,
            InternalBufferSize = 65536,
            NotifyFilter = NotifyFilters.Attributes
                         | NotifyFilters.CreationTime
                         | NotifyFilters.FileName
                         | NotifyFilters.LastWrite
                         | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Created += OnFileEvent;
        _watcher.Changed += OnFileEvent;
        _watcher.Renamed += OnFileRenamed;

        _pollTimer = new System.Timers.Timer(5000);
        _pollTimer.Elapsed += (s, e) =>
        {
            if (!Directory.Exists(_config.Path)) return;
            try
            {
                foreach (var f in Directory.GetFiles(_config.Path, "*.txt"))
                    LeggiFile(f);
            }
            catch { }
        };
        _pollTimer.Start();
    }

    public void Stop()
    {
        _watcher?.Dispose();
        _watcher = null;
        _pollTimer?.Stop();
        _pollTimer?.Dispose();
        _pollTimer = null;
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    private void PulisciFileVecchi(string cartella)
    {
        try
        {
            foreach (var f in Directory.GetFiles(cartella, "*.txt"))
            {
                try
                {
                    if (File.GetLastWriteTime(f) < _avvio)
                        File.Delete(f);
                }
                catch { }
            }
        }
        catch { }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e) => LeggiFile(e.FullPath);

    private void OnFileEvent(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Created ||
            e.ChangeType == WatcherChangeTypes.Changed)
        {
            LeggiFile(e.FullPath);
        }
    }

    private void LeggiFile(string filex)
    {
        if (!File.Exists(filex)) return;

        // NUOVA LOGICA: il target di spegnimento viene dal NOME del file
        // (senza estensione), non dal contenuto.
        // Esempi:
        //   "Win11.txt"   → target = "Win11"
        //   "MacSe.acc"   → target = "MacSe"
        //   "all.txt"     → target = "all"
        //   "google.txt"  → target = "google"
        var target = Path.GetFileNameWithoutExtension(filex);
        var myName = _config.ComputerName?.Trim() ?? "";

        bool match =
            (string.Equals(target, "google", StringComparison.OrdinalIgnoreCase) && _config.Predefinito) ||
            (string.Equals(target, "all", StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(myName) &&
             string.Equals(target, myName, StringComparison.OrdinalIgnoreCase));

        if (!match)
        {
            // Non e' un comando per me. Lascio stare il file: forse e' per un
            // altro PC e Dropbox lo sincronizzera' altrove. Non lo cancello
            // perche' potrei cancellarlo prima che l'altro PC lo legga.
            return;
        }

        // Notifico che ho intercettato il comando
        _notifications.Show("SmartComputerControl",
            $"Comando ricevuto per '{target}'. Verifico...");

        // Verifica che il file sia "fresco" (post-avvio del programma)
        try
        {
            var fileWriteDate = File.GetLastWriteTime(filex);
            if (_avvio < fileWriteDate)
            {
                _notifications.Show("SmartComputerControl",
                    "Spegnimento in corso...");
                _shutdown.Spegni(filex);
            }
            else
            {
                // File vecchio: cancellalo senza spegnere (es. rimasto da prima)
                File.Delete(filex);
                _notifications.Show("SmartComputerControl",
                    "Comando obsoleto ignorato (file precedente all'avvio).");
            }
        }
        catch { }
    }
}
