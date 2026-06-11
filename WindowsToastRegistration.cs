#if WINDOWS
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SmartComputerControlAvalonia.Services;

/// <summary>
/// Registra l'app per le toast notifications moderne di Windows 10/11.
///
/// Servono 3 cose:
///   1. Un AppUserModelID univoco (es. "com.sergio.smartcomputercontrol")
///   2. Uno shortcut .lnk nello Start Menu che associa l'AppUserModelID
///      all'eseguibile e all'icona
///   3. (Opzionalmente) una chiave di registro per associare l'AppId
///      al display name e icona per le notifiche
///
/// La registrazione viene fatta al primo avvio. E' idempotente: chiamarla
/// piu' volte non causa problemi.
/// </summary>
public static class WindowsToastRegistration
{
    public const string AppId = "com.sergio.smartcomputercontrol";
    public const string AppDisplayName = "SmartComputerControl";

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(
        [MarshalAs(UnmanagedType.LPWStr)] string AppID);

    /// <summary>
    /// Chiamare all'avvio dell'app, prima di mostrare qualsiasi notifica.
    /// </summary>
    public static void Register()
    {
        try
        {
            // 1. Imposta l'AppUserModelID per il processo corrente
            SetCurrentProcessExplicitAppUserModelID(AppId);

            // 2. Crea lo shortcut nello Start Menu (idempotente)
            CreateStartMenuShortcut();

            // 3. Registra l'app nel registro per le notifiche
            RegisterAppInRegistry();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToastRegistration] Errore: {ex.Message}");
        }
    }

    private static void CreateStartMenuShortcut()
    {
        var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        var programsFolder = Path.Combine(startMenu, "Programs");
        var shortcutPath = Path.Combine(programsFolder, AppDisplayName + ".lnk");

        // Path eseguibile e icona
        var exePath = Environment.ProcessPath ?? "";
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "avalonia-logo.ico");

        // Se lo shortcut esiste gia' e punta al posto giusto, non rifaccio nulla
        if (File.Exists(shortcutPath))
        {
            // Per semplicita', lo ricreiamo sempre — il sistema operativo non si
            // lamenta e cosi' siamo sicuri che l'icona/path siano aggiornati.
        }

        // Crea lo shortcut usando WScript.Shell via COM dinamico
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null) return;
        var shell = Activator.CreateInstance(shellType);

        try
        {
            var sc = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null, shell, new object?[] { shortcutPath });

            if (sc == null) return;
            var scType = sc.GetType();

            scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object?[] { exePath });
            scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object?[] { AppContext.BaseDirectory });
            if (File.Exists(iconPath))
            {
                scType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc, new object?[] { iconPath });
            }
            scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);

            // Associa l'AppUserModelID allo shortcut.
            // Questo richiede WindowsAPICodePack, MA possiamo evitarlo sfruttando
            // un trucco: NON e' richiesto se l'AppId e' stato impostato sul processo
            // (con SetCurrentProcessExplicitAppUserModelID, che facciamo sopra).
            // Comunque per essere robusti, registriamo l'AppId anche tramite registry.
        }
        finally
        {
            if (shell != null)
                Marshal.FinalReleaseComObject(shell);
        }
    }

    private static void RegisterAppInRegistry()
    {
        // Registra l'AppUserModelID nel registro:
        //   HKCU\Software\Classes\AppUserModelId\<AppId>
        //     DisplayName = "SmartComputerControl"
        //     IconUri     = "<path icona>"
        //
        // Questo dice a Windows come mostrare l'app nelle notifiche/centro.
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "avalonia-logo.ico");

            using var key = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\AppUserModelId\{AppId}");
            if (key == null) return;

            key.SetValue("DisplayName", AppDisplayName);
            if (File.Exists(iconPath))
                key.SetValue("IconUri", iconPath);
            key.SetValue("ShowInSettings", 0);  // non mostra l'app nelle impostazioni notifiche di Windows
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ToastRegistration] Registry: {ex.Message}");
        }
    }
}
#endif
