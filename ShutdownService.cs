using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SmartComputerControlAvalonia.Services;

/// <summary>
/// Spegne il computer in modo cross-platform.
/// </summary>
public class ShutdownService
{
    // Se true, mostra solo console output invece di spegnere davvero
    public bool DebugMode { get; set; } = false;

    public void Spegni(string filex)
    {
        TryDeleteWithRetry(filex);

        if (DebugMode)
        {
            Console.WriteLine("[DEBUG] Il PC sarebbe stato spento ora.");
            return;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("shutdown", "/s /t 00 /f");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("osascript",
                    "-e 'tell application \"System Events\" to shut down'");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("sudo", "shutdown -h now");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore spegnimento: {ex.Message}");
        }
    }

    private void TryDeleteWithRetry(string filex)
    {
        for (int i = 0; i < 5; i++)
        {
            try
            {
                File.Delete(filex);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(500);
            }
            catch { return; }
        }
    }
}
