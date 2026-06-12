using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartComputerControlAvalonia.Services;

public class WatcherService
{
    private readonly ConfigService _config;
    private readonly CommandService _commands;
    private readonly NotificationService _notifications;
    private FileSystemWatcher? _watcher;
    private System.Timers.Timer? _pollTimer;
    private DateTime _avvio = DateTime.Now;
    private readonly object _lock = new();
    private bool _shutdownPending;
    private readonly HashSet<string> _filesInProgress = new(StringComparer.OrdinalIgnoreCase);

    public WatcherService(ConfigService config, CommandService commands, NotificationService notifications)
    {
        _config = config;
        _commands = commands;
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

        // Guard contro doppia esecuzione (FileSystemWatcher + polling timer
        // possono scatenarsi entrambi sullo stesso file)
        lock (_lock)
        {
            if (_shutdownPending) return;
            if (!_filesInProgress.Add(filex)) return;
        }

        try
        {
            ProcessaFile(filex);
        }
        finally
        {
            lock (_lock)
            {
                _filesInProgress.Remove(filex);
            }
        }
    }

    private void ProcessaFile(string filex)
    {
        if (!File.Exists(filex)) return;

        // Il NOME del file (senza estensione) determina IL TARGET (quale PC esegue).
        // Il CONTENUTO del file determina IL COMANDO da eseguire.
        // Contenuto vuoto = shutdown (retrocompatibilita').
        var target = Path.GetFileNameWithoutExtension(filex);
        var myName = _config.ComputerName?.Trim() ?? "";

        bool match =
            (string.Equals(target, "google", StringComparison.OrdinalIgnoreCase) && _config.Predefinito) ||
            (string.Equals(target, "all", StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(myName) &&
             string.Equals(target, myName, StringComparison.OrdinalIgnoreCase));

        if (!match) return;

        // Leggi il contenuto del file (con un piccolo retry, perche'
        // a volte il file e' ancora in scrittura quando arriva l'evento)
        string contenuto = "";
        for (int i = 0; i < 5; i++)
        {
            try
            {
                contenuto = File.ReadAllText(filex);
                break;
            }
            catch
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        var cmd = CommandService.Parse(contenuto);

        if (cmd.Type == CommandType.Unknown)
        {
            _notifications.Show("SmartComputerControl",
                $"Comando non riconosciuto in '{Path.GetFileName(filex)}'");
            try { File.Delete(filex); } catch { }
            return;
        }

        _notifications.Show("SmartComputerControl",
            $"Comando '{cmd.DisplayName}' ricevuto per '{target}'");

        try
        {
            var fileWriteDate = File.GetLastWriteTime(filex);
            if (_avvio >= fileWriteDate)
            {
                // File vecchio, lo ignoriamo
                File.Delete(filex);
                _notifications.Show("SmartComputerControl",
                    "Comando obsoleto ignorato (file precedente all'avvio).");
                return;
            }

            // Cancella subito il file per evitare ri-attivazioni
            try { File.Delete(filex); } catch { }

            // Comandi distruttivi (shutdown/restart): dialog di conferma
            if (cmd.RequiresConfirmation && _config.MostraConferma)
            {
                lock (_lock) { _shutdownPending = true; }

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var dlg = new Views.ShutdownConfirmWindow(_config.SecondiConferma, cmd);
                    dlg.Closed += (s, e) =>
                    {
                        if (dlg.ShouldShutdown)
                        {
                            _notifications.Show("SmartComputerControl",
                                $"{cmd.DisplayName} in corso...");
                            _commands.Execute(cmd);
                        }
                        else
                        {
                            _notifications.Show("SmartComputerControl",
                                $"{cmd.DisplayName} annullato dall'utente.");
                            lock (_lock) { _shutdownPending = false; }
                        }
                    };
                    dlg.Show();
                });
            }
            else
            {
                // Comandi non distruttivi: esegui subito
                _commands.Execute(cmd);
            }
        }
        catch { }
    }
}
