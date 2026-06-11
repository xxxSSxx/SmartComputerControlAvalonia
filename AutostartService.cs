using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
#if WINDOWS
using Microsoft.Win32;
#endif

namespace SmartComputerControlAvalonia.Services;

/// <summary>
/// Autoavvio cross-platform.
/// </summary>
public class AutostartService
{
    private const string AppName = "SmartComputerControl";

    public bool IsEnabled()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return IsEnabledWindows();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))     return IsEnabledMac();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))   return IsEnabledLinux();
        return false;
    }

    public void Enable()
    {
        var exePath = Environment.ProcessPath ?? "";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) EnableWindows(exePath);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) EnableMac(exePath);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) EnableLinux(exePath);
    }

    public void Disable()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) DisableWindows();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) DisableMac();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) DisableLinux();
    }

    // ============ WINDOWS - usiamo Registry via reflection per evitare warning CS ============
    private bool IsEnabledWindows()
    {
        if (!OperatingSystem.IsWindows()) return false;
#pragma warning disable CA1416
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppName) != null;
        }
        catch { return false; }
#pragma warning restore CA1416
    }

    private void EnableWindows(string exePath)
    {
        if (!OperatingSystem.IsWindows()) return;
#pragma warning disable CA1416
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.SetValue(AppName, $"\"{exePath}\"");
        }
        catch { }
#pragma warning restore CA1416
    }

    private void DisableWindows()
    {
        if (!OperatingSystem.IsWindows()) return;
#pragma warning disable CA1416
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue(AppName, false);
        }
        catch { }
#pragma warning restore CA1416
    }

    // ============ macOS ============
    private string MacAgentPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library", "LaunchAgents",
            "com.sergio.smartcomputercontrol.plist");

    private bool IsEnabledMac() => File.Exists(MacAgentPath);

    private void EnableMac(string exePath)
    {
        try
        {
            // Su Mac, exePath punta all'eseguibile interno del bundle:
            //   /Applications/SmartComputerControl.app/Contents/MacOS/SmartComputerControl
            // Avviarlo direttamente NON e' come avviare l'app bundle: macOS perde
            // il contesto del bundle (icona, Info.plist, permessi notifiche).
            // Soluzione: risaliamo al .app e usiamo 'open -a' per avviarlo come app vera.
            string? appBundlePath = null;
            var idx = exePath.IndexOf(".app/", StringComparison.Ordinal);
            if (idx >= 0)
            {
                appBundlePath = exePath.Substring(0, idx + 4); // include ".app"
            }

            Directory.CreateDirectory(Path.GetDirectoryName(MacAgentPath)!);

            string plist;
            if (appBundlePath != null)
            {
                // Caso normale: app installata come .app bundle (es. in /Applications)
                // Usiamo 'open' che avvia l'app rispettando il bundle.
                plist = $"""
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.sergio.smartcomputercontrol</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/bin/open</string>
        <string>{appBundlePath}</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>
""";
            }
            else
            {
                // Fallback: app lanciata in dev (dotnet run) — usa l'eseguibile diretto.
                // Probabilmente non serve davvero (non si autoavvia un'app in debug),
                // ma manteniamo il caso per compatibilita'.
                plist = $"""
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.sergio.smartcomputercontrol</string>
    <key>ProgramArguments</key>
    <array>
        <string>{exePath}</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>
""";
            }

            File.WriteAllText(MacAgentPath, plist);
            Process.Start("launchctl", $"load {MacAgentPath}");
        }
        catch { }
    }

    private void DisableMac()
    {
        try
        {
            if (File.Exists(MacAgentPath))
            {
                Process.Start("launchctl", $"unload {MacAgentPath}");
                File.Delete(MacAgentPath);
            }
        }
        catch { }
    }

    // ============ LINUX ============
    private string LinuxDesktopPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "autostart",
            "smartcomputercontrol.desktop");

    private bool IsEnabledLinux() => File.Exists(LinuxDesktopPath);

    private void EnableLinux(string exePath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LinuxDesktopPath)!);
            var desktop = $"""
[Desktop Entry]
Type=Application
Name=SmartComputerControl
Exec={exePath}
X-GNOME-Autostart-enabled=true
""";
            File.WriteAllText(LinuxDesktopPath, desktop);
        }
        catch { }
    }

    private void DisableLinux()
    {
        try
        {
            if (File.Exists(LinuxDesktopPath)) File.Delete(LinuxDesktopPath);
        }
        catch { }
    }
}
