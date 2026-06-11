#if WINDOWS
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SmartComputerControlAvalonia.Services;

/// <summary>
/// Registra l'app in Windows con un AppUserModelID e un'icona associata,
/// in modo che le notifiche toast mostrino l'icona giusta invece del
/// quadratino generico.
///
/// NON crea shortcut nello Start Menu — solo registry. E' idempotente.
/// </summary>
public static class WindowsAppRegistration
{
    public const string AppId = "Sergio.SmartComputerControl";
    public const string AppDisplayName = "SmartComputerControl";

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(
        [MarshalAs(UnmanagedType.LPWStr)] string AppID);

    public static void Register()
    {
        try
        {
            // 1. Path icona assoluto
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "scc-icon.ico");

            // 2. Registra l'AppUserModelID nel registry
            //    HKCU\Software\Classes\AppUserModelId\<AppId>
            //      DisplayName -> nome visibile nelle notifiche
            //      IconUri     -> path icona
            using (var key = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\AppUserModelId\{AppId}"))
            {
                if (key != null)
                {
                    key.SetValue("DisplayName", AppDisplayName);
                    if (File.Exists(iconPath))
                        key.SetValue("IconUri", iconPath);
                    key.SetValue("ShowInSettings", 0);
                }
            }

            // 3. Imposta l'AppUserModelID sul processo corrente
            //    (deve essere fatto DOPO la registrazione registry)
            SetCurrentProcessExplicitAppUserModelID(AppId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WindowsAppRegistration] Errore: {ex.Message}");
        }
    }
}
#endif
