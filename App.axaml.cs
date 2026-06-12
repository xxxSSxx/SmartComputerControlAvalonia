using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SmartComputerControlAvalonia.Services;
using SmartComputerControlAvalonia.Views;

namespace SmartComputerControlAvalonia;

public partial class App : Application
{
    // Servizi condivisi (singleton "casalingo", senza DI framework)
    public static ConfigService Config { get; private set; } = null!;
    public static WatcherService Watcher { get; private set; } = null!;
    public static ShutdownService Shutdown { get; private set; } = null!;
    public static AutostartService Autostart { get; private set; } = null!;
    public static NotificationService Notifications { get; private set; } = null!;

    private MainWindow? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Inizializza i servizi
        Config = new ConfigService();
        Shutdown = new ShutdownService();
        Notifications = new NotificationService();
        Watcher = new WatcherService(Config, Shutdown, Notifications);
        Autostart = new AutostartService();

        // Su macOS: nascondi l'icona dal Dock con setActivationPolicy(.accessory).
        // IMPORTANTE: deve essere chiamato DOPO che Avalonia ha inizializzato NSApp,
        // altrimenti viene sovrascritto. Usiamo Dispatcher.Post per metterla in coda.
        if (OperatingSystem.IsMacOS())
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                MacOSNotifications.SetAccessoryActivationPolicy();
            }, Avalonia.Threading.DispatcherPriority.Background);
        }

        // Inizializza notifiche (e tray icon nativa su Windows)
        Notifications.Initialize(
            onOpenSettings: ShowMainWindow,
            onExit: ExitApp);

        // Su Windows, NotifyIcon WinForms gestisce la tray. Disattiviamo
        // quella di Avalonia per evitare doppia icona.
        if (Notifications.UsesNativeTray)
        {
            try
            {
                var trayIcons = TrayIcon.GetIcons(this);
                if (trayIcons != null)
                {
                    foreach (var ti in trayIcons)
                        ti.IsVisible = false;
                }
            }
            catch { /* ignora */ }
        }

        // Notifica di avvio
        Notifications.Show("SmartComputerControl", "In esecuzione");

        // Avvia il watcher subito in background
        Watcher.Start();

        // IMPORTANTE: forziamo MainWindow a null per partire SENZA finestra.
        // La finestra viene creata on-demand quando l'utente clicca sulla tray.
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = null;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ExitApp()
    {
        Watcher.Stop();
        Notifications.Dispose();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    // ====== Tray icon handlers ======

    private void OnTrayClicked(object? sender, System.EventArgs e)
    {
        ShowMainWindow();
    }

    private void OnShowSettings(object? sender, System.EventArgs e)
    {
        ShowMainWindow();
    }

    private void OnExitClicked(object? sender, System.EventArgs e)
    {
        ExitApp();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null || !_mainWindow.IsVisible)
        {
            _mainWindow = new MainWindow();
            // Quando la finestra viene chiusa con la X, non terminare l'app;
            // si limita a nascondersi.
            _mainWindow.Closing += (s, e) =>
            {
                e.Cancel = true;
                _mainWindow!.Hide();
            };
            _mainWindow.Show();
        }
        else
        {
            _mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
            _mainWindow.Activate();
        }
    }
}
