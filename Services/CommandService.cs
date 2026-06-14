using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SmartComputerControlAvalonia.Services;

/// <summary>
/// Tipi di comando supportati.
/// </summary>
public enum CommandType
{
    Unknown,

    // Power
    Shutdown, Restart, Sleep, Hibernate, Lock, Logoff, ScreenOff,

    // Audio
    Mute, Unmute, VolumeUp, VolumeDown, Volume,

    // Sistema
    NotifyText, Screenshot, Eject,
    Say, Beep,

    // Rete
    WifiOn, WifiOff,

    // App
    OpenApp, CloseApp, KillAllBrowsers
}

/// <summary>
/// Comando parseato dal contenuto del file.
/// </summary>
public record ParsedCommand(CommandType Type, int? IntParam = null, string? StringParam = null)
{
    /// <summary>
    /// Comandi che richiedono il dialog di conferma con countdown.
    /// </summary>
    public bool RequiresConfirmation =>
        Type == CommandType.Shutdown
        || Type == CommandType.Restart
        || Type == CommandType.Hibernate
        || Type == CommandType.Logoff;

    /// <summary>
    /// Descrizione user-friendly del comando.
    /// </summary>
    public string DisplayName => Type switch
    {
        CommandType.Shutdown          => "Spegnimento",
        CommandType.Restart           => "Riavvio",
        CommandType.Sleep             => "Sospensione",
        CommandType.Hibernate         => "Ibernazione",
        CommandType.Lock              => "Blocco schermo",
        CommandType.Logoff            => "Logout utente",
        CommandType.ScreenOff         => "Spegnimento schermo",
        CommandType.Mute              => "Muta audio",
        CommandType.Unmute            => "Smuta audio",
        CommandType.VolumeUp          => "Aumenta volume",
        CommandType.VolumeDown        => "Abbassa volume",
        CommandType.Volume            => $"Volume a {IntParam ?? 0}%",
        CommandType.NotifyText        => $"Notifica: {StringParam}",
        CommandType.Screenshot        => "Screenshot",
        CommandType.Eject             => "Espelli supporto",
        CommandType.Say               => $"Pronuncia: {StringParam}",
        CommandType.Beep              => "Beep",
        CommandType.WifiOn            => "WiFi ON",
        CommandType.WifiOff           => "WiFi OFF",
        CommandType.OpenApp           => $"Apri: {StringParam}",
        CommandType.CloseApp          => $"Chiudi: {StringParam}",
        CommandType.KillAllBrowsers   => "Chiudi tutti i browser",
        _ => "Comando sconosciuto"
    };
}

/// <summary>
/// Servizio che esegue i comandi sul sistema, cross-platform.
/// </summary>
public class CommandService
{
    /// <summary>Se true, log e basta (no esecuzione vera).</summary>
    public bool DebugMode { get; set; }

    /// <summary>
    /// Parsea il contenuto di un file .txt e restituisce il comando.
    /// File vuoto = shutdown (retrocompatibilita').
    /// </summary>
    public static ParsedCommand Parse(string content)
    {
        var raw = (content ?? "").Trim();

        if (raw.Length > 0 && (raw[0] == '\uFEFF' || raw[0] == '\uFFFE'))
            raw = raw.Substring(1);

        raw = raw.Trim('"', '\'', ' ', '\t', '\r', '\n');

        // Salviamo il contenuto "originale" (senza lowercase) per i parametri
        // di comandi tipo notify: e say: dove le maiuscole/minuscole contano
        var lower = raw.ToLowerInvariant();

        if (string.IsNullOrEmpty(lower) || lower == "shutdown")
            return new ParsedCommand(CommandType.Shutdown);

        // Comandi senza parametri (matchano per stringa esatta lowercase)
        switch (lower)
        {
            case "restart":        return new ParsedCommand(CommandType.Restart);
            case "sleep":          return new ParsedCommand(CommandType.Sleep);
            case "hibernate":      return new ParsedCommand(CommandType.Hibernate);
            case "lock":           return new ParsedCommand(CommandType.Lock);
            case "logoff":
            case "logout":         return new ParsedCommand(CommandType.Logoff);
            case "screen-off":
            case "screenoff":      return new ParsedCommand(CommandType.ScreenOff);
            case "mute":           return new ParsedCommand(CommandType.Mute);
            case "unmute":         return new ParsedCommand(CommandType.Unmute);
            case "volume-up":
            case "volumeup":       return new ParsedCommand(CommandType.VolumeUp);
            case "volume-down":
            case "volumedown":     return new ParsedCommand(CommandType.VolumeDown);
            case "screenshot":     return new ParsedCommand(CommandType.Screenshot);
            case "eject":          return new ParsedCommand(CommandType.Eject);
            case "beep":           return new ParsedCommand(CommandType.Beep);
            case "wifi-on":
            case "wifion":         return new ParsedCommand(CommandType.WifiOn);
            case "wifi-off":
            case "wifioff":        return new ParsedCommand(CommandType.WifiOff);
            case "kill-all-browsers":
            case "killallbrowsers": return new ParsedCommand(CommandType.KillAllBrowsers);
        }

        // Comandi con parametri: usano la forma "verbo:argomento"
        // Per il payload (argomento) usiamo il raw originale, non lowercase
        var colonIdx = raw.IndexOf(':');
        if (colonIdx > 0)
        {
            var verb = raw.Substring(0, colonIdx).Trim().ToLowerInvariant();
            var arg  = raw.Substring(colonIdx + 1).Trim();

            switch (verb)
            {
                case "volume":
                    if (int.TryParse(arg, out var n))
                        return new ParsedCommand(CommandType.Volume, Math.Clamp(n, 0, 100));
                    break;

                case "notify":
                    if (!string.IsNullOrEmpty(arg))
                        return new ParsedCommand(CommandType.NotifyText, null, arg);
                    break;

                case "say":
                    if (!string.IsNullOrEmpty(arg))
                        return new ParsedCommand(CommandType.Say, null, arg);
                    break;

                case "open":
                    if (!string.IsNullOrEmpty(arg))
                        return new ParsedCommand(CommandType.OpenApp, null, arg);
                    break;

                case "close":
                case "quit":
                    if (!string.IsNullOrEmpty(arg))
                        return new ParsedCommand(CommandType.CloseApp, null, arg);
                    break;
            }
        }

        // Vecchio formato "volume N" con spazio
        if (lower.StartsWith("volume "))
        {
            var part = lower.Substring(7).Trim();
            if (int.TryParse(part, out var n))
                return new ParsedCommand(CommandType.Volume, Math.Clamp(n, 0, 100));
        }

        return new ParsedCommand(CommandType.Unknown);
    }

    /// <summary>Esegue il comando sul sistema. Se DebugMode = true, log e basta.</summary>
    public void Execute(ParsedCommand cmd)
    {
        if (DebugMode)
        {
            Console.WriteLine($"[DEBUG] Comando '{cmd.DisplayName}' simulato (DebugMode ON).");
            return;
        }

        switch (cmd.Type)
        {
            // Power
            case CommandType.Shutdown:    ExecuteShutdown(); break;
            case CommandType.Restart:     ExecuteRestart(); break;
            case CommandType.Sleep:       ExecuteSleep(); break;
            case CommandType.Hibernate:   ExecuteHibernate(); break;
            case CommandType.Lock:        ExecuteLock(); break;
            case CommandType.Logoff:      ExecuteLogoff(); break;
            case CommandType.ScreenOff:   ExecuteScreenOff(); break;

            // Audio
            case CommandType.Mute:        ExecuteSetMute(true); break;
            case CommandType.Unmute:      ExecuteSetMute(false); break;
            case CommandType.VolumeUp:    ExecuteVolumeStep(+10); break;
            case CommandType.VolumeDown:  ExecuteVolumeStep(-10); break;
            case CommandType.Volume:
                if (cmd.IntParam.HasValue) ExecuteVolume(cmd.IntParam.Value);
                break;

            // Sistema
            case CommandType.NotifyText:
                if (!string.IsNullOrEmpty(cmd.StringParam))
                    ExecuteNotifyText(cmd.StringParam);
                break;
            case CommandType.Screenshot:    ExecuteScreenshot(); break;
            case CommandType.Eject:         ExecuteEject(); break;
            case CommandType.Say:
                if (!string.IsNullOrEmpty(cmd.StringParam))
                    ExecuteSay(cmd.StringParam);
                break;
            case CommandType.Beep:          ExecuteBeep(); break;

            // Rete
            case CommandType.WifiOn:        ExecuteWifi(true); break;
            case CommandType.WifiOff:       ExecuteWifi(false); break;

            // App
            case CommandType.OpenApp:
                if (!string.IsNullOrEmpty(cmd.StringParam))
                    ExecuteOpenApp(cmd.StringParam);
                break;
            case CommandType.CloseApp:
                if (!string.IsNullOrEmpty(cmd.StringParam))
                    ExecuteCloseApp(cmd.StringParam);
                break;
            case CommandType.KillAllBrowsers: ExecuteKillAllBrowsers(); break;
        }
    }

    // ===== POWER =====
    private void ExecuteShutdown()
    {
        if (OperatingSystem.IsWindows())     Run("shutdown", "/s /t 0");
        else if (OperatingSystem.IsMacOS())  RunAppleScript("tell application \"System Events\" to shut down");
        else if (OperatingSystem.IsLinux())  Run("sudo", "shutdown -h now");
    }

    private void ExecuteRestart()
    {
        if (OperatingSystem.IsWindows())     Run("shutdown", "/r /t 0");
        else if (OperatingSystem.IsMacOS())  RunAppleScript("tell application \"System Events\" to restart");
        else if (OperatingSystem.IsLinux())  Run("sudo", "shutdown -r now");
    }

    private void ExecuteSleep()
    {
        if (OperatingSystem.IsWindows())     Run("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0");
        else if (OperatingSystem.IsMacOS())  Run("pmset", "sleepnow");
        else if (OperatingSystem.IsLinux())  Run("systemctl", "suspend");
    }

    private void ExecuteHibernate()
    {
        if (OperatingSystem.IsWindows())     Run("shutdown", "/h");
        else if (OperatingSystem.IsMacOS())  Run("pmset", "sleepnow"); // macOS non ha vera hibernate
        else if (OperatingSystem.IsLinux())  Run("systemctl", "hibernate");
    }

    private void ExecuteLock()
    {
        if (OperatingSystem.IsWindows())
            Run("rundll32.exe", "user32.dll,LockWorkStation");
        else if (OperatingSystem.IsMacOS())
            RunAppleScript("tell application \"System Events\" to keystroke \"q\" using {command down, control down}");
        else if (OperatingSystem.IsLinux())
            Run("loginctl", "lock-session");
    }

    private void ExecuteLogoff()
    {
        if (OperatingSystem.IsWindows())     Run("shutdown", "/l");
        else if (OperatingSystem.IsMacOS())  RunAppleScript("tell application \"System Events\" to log out");
        else if (OperatingSystem.IsLinux())  Run("loginctl", $"terminate-user {Environment.UserName}");
    }

    private void ExecuteScreenOff()
    {
        if (OperatingSystem.IsWindows())
        {
            try { TurnOffMonitorWindows(); }
            catch (Exception ex) { Console.WriteLine($"Errore screen off: {ex.Message}"); }
        }
        else if (OperatingSystem.IsMacOS())  Run("pmset", "displaysleepnow");
        else if (OperatingSystem.IsLinux())
        {
            if (!TryRun("xset", "dpms force off"))
                Run("loginctl", "lock-session");
        }
    }

    // ===== AUDIO =====
    private void ExecuteSetMute(bool mute)
    {
        if (OperatingSystem.IsWindows())
        {
#if WINDOWS
            try
            {
                var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var dev = enumerator.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia);
                dev.AudioEndpointVolume.Mute = mute;
            }
            catch (Exception ex) { Console.WriteLine($"Errore mute: {ex.Message}"); }
#endif
        }
        else if (OperatingSystem.IsMacOS())
            RunAppleScript($"set volume output muted {(mute ? "true" : "false")}");
        else if (OperatingSystem.IsLinux())
            Run("pactl", $"set-sink-mute @DEFAULT_SINK@ {(mute ? "1" : "0")}");
    }

    private void ExecuteVolume(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        if (OperatingSystem.IsWindows())
        {
#if WINDOWS
            try
            {
                var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var dev = enumerator.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia);
                dev.AudioEndpointVolume.MasterVolumeLevelScalar = percent / 100f;
            }
            catch (Exception ex) { Console.WriteLine($"Errore volume: {ex.Message}"); }
#endif
        }
        else if (OperatingSystem.IsMacOS())  RunAppleScript($"set volume output volume {percent}");
        else if (OperatingSystem.IsLinux())  Run("pactl", $"set-sink-volume @DEFAULT_SINK@ {percent}%");
    }

    private void ExecuteVolumeStep(int deltaPercent)
    {
        if (OperatingSystem.IsWindows())
        {
#if WINDOWS
            try
            {
                var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var dev = enumerator.GetDefaultAudioEndpoint(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.Role.Multimedia);
                var current = dev.AudioEndpointVolume.MasterVolumeLevelScalar;
                var newVal = Math.Clamp(current + (deltaPercent / 100f), 0f, 1f);
                dev.AudioEndpointVolume.MasterVolumeLevelScalar = newVal;
            }
            catch (Exception ex) { Console.WriteLine($"Errore volume step: {ex.Message}"); }
#endif
        }
        else if (OperatingSystem.IsMacOS())
        {
            var sign = deltaPercent >= 0 ? "+" : "-";
            var abs = Math.Abs(deltaPercent);
            RunAppleScript(
                $"set curVol to output volume of (get volume settings)\n" +
                $"set newVol to curVol {sign} {abs}\n" +
                $"if newVol < 0 then set newVol to 0\n" +
                $"if newVol > 100 then set newVol to 100\n" +
                $"set volume output volume newVol");
        }
        else if (OperatingSystem.IsLinux())
        {
            var sign = deltaPercent >= 0 ? "+" : "-";
            var abs = Math.Abs(deltaPercent);
            Run("pactl", $"set-sink-volume @DEFAULT_SINK@ {sign}{abs}%");
        }
    }

    // ===== NOTIFICA TESTO =====
    private void ExecuteNotifyText(string text)
    {
        // Riusa il NotificationService per consistenza
        var ns = new NotificationService();
        ns.Show("Messaggio remoto", text);
    }

    // ===== SCREENSHOT =====
    private void ExecuteScreenshot()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var path = Path.Combine(desktop, $"screenshot_{ts}.png");

        if (OperatingSystem.IsWindows())
        {
            // PowerShell con .NET per cattura schermo
            var script =
                "Add-Type -AssemblyName System.Windows.Forms; " +
                "Add-Type -AssemblyName System.Drawing; " +
                "$b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds; " +
                "$bmp = New-Object System.Drawing.Bitmap $b.Width,$b.Height; " +
                "$g = [System.Drawing.Graphics]::FromImage($bmp); " +
                "$g.CopyFromScreen($b.Location,[System.Drawing.Point]::Empty,$b.Size); " +
                $"$bmp.Save('{path.Replace("\\","\\\\")}',[System.Drawing.Imaging.ImageFormat]::Png)";
            Run("powershell", $"-NoProfile -Command \"{script}\"");
        }
        else if (OperatingSystem.IsMacOS())
        {
            Run("screencapture", $"-x \"{path}\"");
        }
        else if (OperatingSystem.IsLinux())
        {
            // Provo gnome-screenshot, poi grim (Wayland), poi scrot
            if (!TryRun("gnome-screenshot", $"-f \"{path}\""))
                if (!TryRun("grim", $"\"{path}\""))
                    TryRun("scrot", $"\"{path}\"");
        }

        Console.WriteLine($"Screenshot salvato in: {path}");
    }

    // ===== EJECT =====
    private void ExecuteEject()
    {
        if (OperatingSystem.IsWindows())
        {
            // Apre cassetto CD (mci command)
            Run("powershell", "-NoProfile -Command \"$sh = New-Object -ComObject Shell.Application; $sh.Namespace(17).Items() | Where-Object { $_.Type -eq 'CD Drive' } | ForEach-Object { $_.InvokeVerb('Eject') }\"");
        }
        else if (OperatingSystem.IsMacOS())
        {
            RunAppleScript("tell application \"Finder\" to eject (every disk whose ejectable is true)");
        }
        else if (OperatingSystem.IsLinux())
        {
            Run("eject", "");
        }
    }

    // ===== SAY (TTS) =====
    private void ExecuteSay(string text)
    {
        if (OperatingSystem.IsWindows())
        {
            // PowerShell System.Speech
            var safeText = text.Replace("'", "''");
            Run("powershell", $"-NoProfile -Command \"Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('{safeText}')\"");
        }
        else if (OperatingSystem.IsMacOS())
        {
            var psi = new ProcessStartInfo
            {
                FileName = "say",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add(text);
            try { Process.Start(psi); }
            catch (Exception ex) { Console.WriteLine($"Errore say: {ex.Message}"); }
        }
        else if (OperatingSystem.IsLinux())
        {
            // spd-say e' incluso con speech-dispatcher (di solito gia' presente)
            if (!TryRunArgs("spd-say", text))
                TryRunArgs("espeak", text);
        }
    }

    // ===== BEEP =====
    private void ExecuteBeep()
    {
        if (OperatingSystem.IsWindows())
        {
            Run("powershell", "-NoProfile -Command \"[Console]::Beep(800,300)\"");
        }
        else if (OperatingSystem.IsMacOS())
        {
            RunAppleScript("beep");
        }
        else if (OperatingSystem.IsLinux())
        {
            // Suona il bell sound di sistema
            if (!TryRun("paplay", "/usr/share/sounds/freedesktop/stereo/bell.oga"))
                if (!TryRun("aplay", "/usr/share/sounds/sound-icons/glass-water-1.wav"))
                    TryRun("printf", "\\a");
        }
    }

    // ===== WIFI =====
    private void ExecuteWifi(bool on)
    {
        if (OperatingSystem.IsWindows())
        {
            Run("netsh", $"interface set interface name=\"Wi-Fi\" admin={(on ? "ENABLED" : "DISABLED")}");
        }
        else if (OperatingSystem.IsMacOS())
        {
            // Trova l'interfaccia Wi-Fi e la accende/spegne
            Run("networksetup", $"-setairportpower en0 {(on ? "on" : "off")}");
        }
        else if (OperatingSystem.IsLinux())
        {
            Run("nmcli", $"radio wifi {(on ? "on" : "off")}");
        }
    }

    // ===== APPS =====
    private void ExecuteOpenApp(string name)
    {
        if (OperatingSystem.IsWindows())
        {
            // Start-Process apre per nome se l'app e' nel PATH/Start Menu
            Run("powershell", $"-NoProfile -Command \"Start-Process '{name.Replace("'", "''")}'\"");
        }
        else if (OperatingSystem.IsMacOS())
        {
            // "open -a NomeApp"
            var psi = new ProcessStartInfo
            {
                FileName = "open",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-a");
            psi.ArgumentList.Add(name);
            try { Process.Start(psi); }
            catch (Exception ex) { Console.WriteLine($"Errore open: {ex.Message}"); }
        }
        else if (OperatingSystem.IsLinux())
        {
            // Provo a lanciare il binario direttamente, fallback su xdg-open
            if (!TryRun(name.ToLowerInvariant(), ""))
                TryRun("xdg-open", name);
        }
    }

    private void ExecuteCloseApp(string name)
    {
        if (OperatingSystem.IsWindows())
        {
            Run("taskkill", $"/IM \"{name}.exe\" /F");
        }
        else if (OperatingSystem.IsMacOS())
        {
            RunAppleScript($"tell application \"{name}\" to quit");
        }
        else if (OperatingSystem.IsLinux())
        {
            Run("pkill", $"-f \"{name}\"");
        }
    }

    private void ExecuteKillAllBrowsers()
    {
        string[] browsers = OperatingSystem.IsWindows()
            ? new[] { "chrome.exe", "msedge.exe", "firefox.exe", "brave.exe", "opera.exe" }
            : new[] { "chrome", "firefox", "Safari", "msedge", "brave", "opera", "Brave Browser", "Microsoft Edge" };

        foreach (var b in browsers)
        {
            if (OperatingSystem.IsWindows())
                Run("taskkill", $"/IM \"{b}\" /F");
            else if (OperatingSystem.IsMacOS())
                RunAppleScript($"tell application \"{b}\" to quit");
            else if (OperatingSystem.IsLinux())
                Run("pkill", $"-f \"{b}\"");
        }
    }

    // ===== Helpers =====
    private static void Run(string file, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
        catch (Exception ex) { Console.WriteLine($"Errore {file}: {ex.Message}"); }
    }

    private static bool TryRun(string file, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var p = Process.Start(psi);
            return p != null;
        }
        catch { return false; }
    }

    /// <summary>
    /// Come TryRun ma con ArgumentList (evita problemi di escaping per testi con spazi/virgolette).
    /// </summary>
    private static bool TryRunArgs(string file, params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var a in args) psi.ArgumentList.Add(a);
            var p = Process.Start(psi);
            return p != null;
        }
        catch { return false; }
    }

    private static void RunAppleScript(string script)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "osascript",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-e");
            psi.ArgumentList.Add(script);
            Process.Start(psi);
        }
        catch (Exception ex) { Console.WriteLine($"Errore AppleScript: {ex.Message}"); }
    }

#if WINDOWS
    // ===== Win32 P/Invoke per screen-off =====
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern System.IntPtr SendMessage(System.IntPtr hWnd, uint Msg, System.IntPtr wParam, System.IntPtr lParam);

    private const uint WM_SYSCOMMAND = 0x0112;
    private const int SC_MONITORPOWER = 0xF170;
    private static readonly System.IntPtr HWND_BROADCAST = new System.IntPtr(0xFFFF);

    private static void TurnOffMonitorWindows()
    {
        SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, new System.IntPtr(SC_MONITORPOWER), new System.IntPtr(2));
    }
#else
    private static void TurnOffMonitorWindows() { }
#endif
}
