using System;
using System.Diagnostics;
using System.IO;
#if WINDOWS
using System.Drawing;
using System.Windows.Forms;
using Avalonia.Threading;
#endif

namespace SmartComputerControlAvalonia.Services;

/// <summary>
/// Tray icon + notifiche native, per piattaforma:
/// - Windows: NotifyIcon (WinForms) + balloon tip
/// - macOS:   tray Avalonia + osascript
/// - Linux:   tray Avalonia + notify-send
/// </summary>
public class NotificationService : IDisposable
{
    private Action? _onOpenSettings;
    private Action? _onExit;

#if WINDOWS
    private NotifyIcon? _notifyIcon;
#endif

    public bool UsesNativeTray => OperatingSystem.IsWindows();

    public void Initialize(Action onOpenSettings, Action onExit)
    {
        _onOpenSettings = onOpenSettings;
        _onExit = onExit;

#if WINDOWS
        // Registra l'app in Windows (AppUserModelID + icona per i toast)
        WindowsAppRegistration.Register();

        try
        {
            Icon icon;
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "scc-icon.ico");
            if (File.Exists(iconPath))
                icon = new Icon(iconPath);
            else
                icon = SystemIcons.Application;

            var menu = new ContextMenuStrip();
            menu.Items.Add("Apri impostazioni", null, (s, e) => _onOpenSettings?.Invoke());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Esci", null, (s, e) => _onExit?.Invoke());

            _notifyIcon = new NotifyIcon
            {
                Icon = icon,
                Visible = true,
                Text = "SmartComputerControl",
                ContextMenuStrip = menu
            };

            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    _onOpenSettings?.Invoke();
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Errore init NotifyIcon: {ex.Message}");
        }
#endif
    }

    public void Show(string title, string body)
    {
        Console.WriteLine($"[{title}] {body}");

        if (OperatingSystem.IsWindows())
        {
            ShowWindows(title, body);
        }
        else if (OperatingSystem.IsMacOS())
        {
            ShowMacOS(title, body);
        }
        else if (OperatingSystem.IsLinux())
        {
            ShowLinux(title, body);
        }
    }

    private void ShowWindows(string title, string body)
    {
#if WINDOWS
        if (_notifyIcon == null) return;
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _notifyIcon.BalloonTipIcon = ToolTipIcon.None;
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = body;
                _notifyIcon.ShowBalloonTip(3000);
            }
            catch (Exception ex) { Console.WriteLine($"Errore balloon: {ex.Message}"); }
        });
#endif
    }

    private bool _macAuthRequested;

    private void ShowMacOS(string title, string body)
    {
        try
        {
            // Al primo invio, richiede il permesso (popup di sistema macOS).
            // Funziona solo se l'app gira dentro un .app bundle.
            if (!_macAuthRequested)
            {
                MacOSNotifications.RequestAuthorization();
                _macAuthRequested = true;
            }
            MacOSNotifications.Show(title, body);
        }
        catch (Exception ex) { Console.WriteLine($"Errore macOS: {ex.Message}"); }
    }

    private void ShowLinux(string title, string body)
    {
        try
        {
            // notify-send: cerchiamo l'icona in vari path probabili
            // (Assets accanto all'eseguibile, ico o png).
            var baseDir = AppContext.BaseDirectory;
            var iconCandidates = new[]
            {
                Path.Combine(baseDir, "Assets", "scc-icon.png"),
                Path.Combine(baseDir, "Assets", "scc-icon.ico"),
            };
            string? iconPath = null;
            foreach (var c in iconCandidates)
            {
                if (File.Exists(c)) { iconPath = c; break; }
            }

            var psi = new ProcessStartInfo
            {
                FileName = "notify-send",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("--app-name=SmartComputerControl");
            if (iconPath != null)
            {
                psi.ArgumentList.Add($"--icon={iconPath}");
            }
            psi.ArgumentList.Add(title);
            psi.ArgumentList.Add(body);

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore Linux (notify-send installato?): {ex.Message}");
        }
    }

    public void Dispose()
    {
#if WINDOWS
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
#endif
    }
}
