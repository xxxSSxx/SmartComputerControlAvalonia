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

    private void ShowMacOS(string title, string body)
    {
        try
        {
            // Sanitize per AppleScript: gli apici doppi vanno escapati
            // come backslash-virgolette dentro lo script.
            var safeTitle = title.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var safeBody = body.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var script = $"display notification \"{safeBody}\" with title \"{safeTitle}\"";

            var psi = new ProcessStartInfo
            {
                FileName = "osascript",
                // ArgumentList passa gli argomenti senza shell parsing,
                // cosi' niente problemi con apici/spazi.
                ArgumentList = { "-e", script },
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
        catch (Exception ex) { Console.WriteLine($"Errore macOS: {ex.Message}"); }
    }

    private void ShowLinux(string title, string body)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "notify-send",
                ArgumentList = { "--app-name=SmartComputerControl", title, body },
                UseShellExecute = false,
                CreateNoWindow = true
            };
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
