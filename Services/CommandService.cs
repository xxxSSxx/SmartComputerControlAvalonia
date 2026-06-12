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
    Shutdown,
    Restart,
    Sleep,
    Lock,
    Mute,
    Volume
}

/// <summary>
/// Comando parseato dal contenuto del file.
/// </summary>
public record ParsedCommand(CommandType Type, int? IntParam = null)
{
    /// <summary>
    /// Comandi che richiedono il dialog di conferma con countdown.
    /// </summary>
    public bool RequiresConfirmation =>
        Type == CommandType.Shutdown || Type == CommandType.Restart;

    /// <summary>
    /// Descrizione user-friendly del comando.
    /// </summary>
    public string DisplayName => Type switch
    {
        CommandType.Shutdown => "Spegnimento",
        CommandType.Restart => "Riavvio",
        CommandType.Sleep => "Sospensione",
        CommandType.Lock => "Blocco schermo",
        CommandType.Mute => "Muta/smuta audio",
        CommandType.Volume => $"Volume a {IntParam ?? 0}%",
        _ => "Comando sconosciuto"
    };
}

/// <summary>
/// Servizio che esegue i comandi sul sistema, cross-platform.
/// </summary>
public class CommandService
{
    /// <summary>
    /// Se true, non esegue davvero i comandi distruttivi (shutdown/restart).
    /// Utile per test.
    /// </summary>
    public bool DebugMode { get; set; }

    /// <summary>
    /// Parsea il contenuto di un file .txt e restituisce il comando.
    /// File vuoto = shutdown (retrocompatibilita').
    /// Tollera virgolette, BOM, spazi, e altri caratteri di "rumore"
    /// che possono apparire a seconda della shell che ha creato il file.
    /// </summary>
    public static ParsedCommand Parse(string content)
    {
        var raw = (content ?? "").Trim();

        // Rimuove BOM UTF-8/UTF-16 (alcune shell Windows lo aggiungono)
        if (raw.Length > 0 && (raw[0] == '\uFEFF' || raw[0] == '\uFFFE'))
            raw = raw.Substring(1);

        // Rimuove virgolette singole o doppie agli estremi
        // (es. echo "shutdown" su CMD scrive 'shutdown' tra virgolette)
        raw = raw.Trim('"', '\'', ' ', '\t', '\r', '\n').ToLowerInvariant();

        // Vuoto o "shutdown" → shutdown
        if (string.IsNullOrEmpty(raw) || raw == "shutdown")
            return new ParsedCommand(CommandType.Shutdown);

        if (raw == "restart") return new ParsedCommand(CommandType.Restart);
        if (raw == "sleep") return new ParsedCommand(CommandType.Sleep);
        if (raw == "lock") return new ParsedCommand(CommandType.Lock);
        if (raw == "mute") return new ParsedCommand(CommandType.Mute);

        // volume:N
        if (raw.StartsWith("volume:") || raw.StartsWith("volume "))
        {
            var part = raw[6..].TrimStart(':', ' ');
            if (int.TryParse(part, out var n))
            {
                n = Math.Clamp(n, 0, 100);
                return new ParsedCommand(CommandType.Volume, n);
            }
        }

        return new ParsedCommand(CommandType.Unknown);
    }

    /// <summary>
    /// Esegue il comando sul sistema. Se DebugMode = true, log e basta.
    /// </summary>
    public void Execute(ParsedCommand cmd)
    {
        if (DebugMode)
        {
            Console.WriteLine($"[DEBUG] Comando '{cmd.DisplayName}' simulato (DebugMode ON).");
            return;
        }

        switch (cmd.Type)
        {
            case CommandType.Shutdown:
                ExecuteShutdown();
                break;

            case CommandType.Restart:
                ExecuteRestart();
                break;

            case CommandType.Sleep:
                ExecuteSleep();
                break;

            case CommandType.Lock:
                ExecuteLock();
                break;

            case CommandType.Mute:
                ExecuteMute();
                break;

            case CommandType.Volume:
                if (cmd.IntParam.HasValue) ExecuteVolume(cmd.IntParam.Value);
                break;
        }
    }

    // ===== SHUTDOWN =====
    private void ExecuteShutdown()
    {
        if (OperatingSystem.IsWindows())
            Run("shutdown", "/s /t 0");
        else if (OperatingSystem.IsMacOS())
            RunAppleScript("tell application \"System Events\" to shut down");
        else if (OperatingSystem.IsLinux())
            Run("sudo", "shutdown -h now");
    }

    // ===== RESTART =====
    private void ExecuteRestart()
    {
        if (OperatingSystem.IsWindows())
            Run("shutdown", "/r /t 0");
        else if (OperatingSystem.IsMacOS())
            RunAppleScript("tell application \"System Events\" to restart");
        else if (OperatingSystem.IsLinux())
            Run("sudo", "shutdown -r now");
    }

    // ===== SLEEP =====
    private void ExecuteSleep()
    {
        if (OperatingSystem.IsWindows())
            Run("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0");
        else if (OperatingSystem.IsMacOS())
            Run("pmset", "sleepnow");
        else if (OperatingSystem.IsLinux())
            Run("systemctl", "suspend");
    }

    // ===== LOCK =====
    private void ExecuteLock()
    {
        if (OperatingSystem.IsWindows())
            Run("rundll32.exe", "user32.dll,LockWorkStation");
        else if (OperatingSystem.IsMacOS())
            // Su Mac il "lock screen" si ottiene mandando il display in sleep
            Run("pmset", "displaysleepnow");
        else if (OperatingSystem.IsLinux())
            Run("loginctl", "lock-session");
    }

    // ===== MUTE (toggle) =====
    private void ExecuteMute()
    {
        if (OperatingSystem.IsWindows())
        {
#if WINDOWS
            try
            {
                var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var dev = enumerator.GetDefaultAudioEndpoint(
                    NAudio.CoreAudioApi.DataFlow.Render,
                    NAudio.CoreAudioApi.Role.Multimedia);
                dev.AudioEndpointVolume.Mute = !dev.AudioEndpointVolume.Mute;
            }
            catch (Exception ex) { Console.WriteLine($"Errore mute: {ex.Message}"); }
#endif
        }
        else if (OperatingSystem.IsMacOS())
        {
            RunAppleScript("set volume output muted (not (output muted of (get volume settings)))");
        }
        else if (OperatingSystem.IsLinux())
        {
            Run("pactl", "set-sink-mute @DEFAULT_SINK@ toggle");
        }
    }

    // ===== VOLUME =====
    private void ExecuteVolume(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);

        if (OperatingSystem.IsWindows())
        {
#if WINDOWS
            try
            {
                var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
                var dev = enumerator.GetDefaultAudioEndpoint(
                    NAudio.CoreAudioApi.DataFlow.Render,
                    NAudio.CoreAudioApi.Role.Multimedia);
                dev.AudioEndpointVolume.MasterVolumeLevelScalar = percent / 100f;
            }
            catch (Exception ex) { Console.WriteLine($"Errore volume: {ex.Message}"); }
#endif
        }
        else if (OperatingSystem.IsMacOS())
        {
            // Mac: scala 0-100 → 0-100
            RunAppleScript($"set volume output volume {percent}");
        }
        else if (OperatingSystem.IsLinux())
        {
            Run("pactl", $"set-sink-volume @DEFAULT_SINK@ {percent}%");
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
}
