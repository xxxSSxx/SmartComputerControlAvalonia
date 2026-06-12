using System;
using System.Runtime.InteropServices;

namespace SmartComputerControlAvalonia;

/// <summary>
/// Notifiche native macOS usando UNUserNotificationCenter (UserNotifications.framework).
/// Funziona dentro un .app bundle. Al primo invio, macOS chiede il permesso all'utente
/// con un popup di sistema "SmartComputerControl vuole inviare notifiche?".
///
/// Implementato via P/Invoke a Objective-C runtime — niente dipendenze esterne.
/// </summary>
public static class MacOSNotifications
{
    // ===== Objective-C runtime =====
    private const string LibObjC = "/usr/lib/libobjc.dylib";
    private const string LibFoundation = "/System/Library/Frameworks/Foundation.framework/Foundation";
    private const string LibUserNotifications = "/System/Library/Frameworks/UserNotifications.framework/UserNotifications";

    [DllImport(LibObjC, EntryPoint = "objc_getClass")]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(LibObjC, EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_void(IntPtr receiver, IntPtr selector, ulong arg1, IntPtr block);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_long(IntPtr receiver, IntPtr selector, long arg1);

    // ===== Caricamento dei framework =====
    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern IntPtr dlopen(string path, int mode);

    private const int RTLD_NOW = 2;
    private static bool _frameworksLoaded;

    private static void EnsureFrameworksLoaded()
    {
        if (_frameworksLoaded) return;
        dlopen(LibFoundation, RTLD_NOW);
        dlopen(LibUserNotifications, RTLD_NOW);
        _frameworksLoaded = true;
    }

    // ===== Helpers =====
    private static IntPtr NSString(string s)
    {
        var nsString = objc_getClass("NSString");
        var sel = sel_registerName("stringWithUTF8String:");
        var utf8 = Marshal.StringToHGlobalAuto(s);
        try
        {
            return objc_msgSend(nsString, sel, utf8);
        }
        finally
        {
            Marshal.FreeHGlobal(utf8);
        }
    }

    // ===== API pubblica =====

    /// <summary>
    /// Imposta l'app come "accessory" (no Dock icon, no menu bar globale).
    /// Va chiamato all'avvio, dopo che Avalonia ha inizializzato NSApplication.
    /// Equivalente a NSApp.setActivationPolicy(.accessory) in Swift.
    /// </summary>
    public static void SetAccessoryActivationPolicy()
    {
        try
        {
            EnsureFrameworksLoaded();
            dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", RTLD_NOW);

            var nsAppClass = objc_getClass("NSApplication");
            if (nsAppClass == IntPtr.Zero) return;

            var sharedAppSel = sel_registerName("sharedApplication");
            var nsApp = objc_msgSend(nsAppClass, sharedAppSel);
            if (nsApp == IntPtr.Zero) return;

            // NSApplicationActivationPolicyAccessory = 1
            //   0 = Regular (icona Dock + menu globale)
            //   1 = Accessory (no Cmd+Tab, no menu globale, no pallino sotto Dock)
            //   2 = Prohibited
            var setPolicySel = sel_registerName("setActivationPolicy:");
            objc_msgSend_long(nsApp, setPolicySel, 1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MacOSNotifications] SetAccessoryActivationPolicy errore: {ex.Message}");
        }
    }

    /// <summary>
    /// Richiede il permesso all'utente di inviare notifiche.
    /// Va chiamato una volta all'avvio dell'app. Se il permesso e' gia' stato dato
    /// (o negato) in passato, macOS non mostra di nuovo il popup.
    /// </summary>
    public static void RequestAuthorization()
    {
        try
        {
            EnsureFrameworksLoaded();

            var unCenter = objc_getClass("UNUserNotificationCenter");
            if (unCenter == IntPtr.Zero) return;

            var currentSel = sel_registerName("currentNotificationCenter");
            var center = objc_msgSend(unCenter, currentSel);
            if (center == IntPtr.Zero) return;

            // UNAuthorizationOptions: Alert (1) | Sound (2) | Badge (4) = 7
            var requestSel = sel_registerName("requestAuthorizationWithOptions:completionHandler:");

            // Passiamo IntPtr.Zero come completion handler (non ci interessa il risultato)
            objc_msgSend_void(center, requestSel, 7UL, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MacOSNotifications] RequestAuthorization errore: {ex.Message}");
        }
    }

    /// <summary>
    /// Mostra una notifica con titolo e corpo.
    /// </summary>
    public static void Show(string title, string body)
    {
        try
        {
            EnsureFrameworksLoaded();

            // 1. Crea UNMutableNotificationContent
            var unContentClass = objc_getClass("UNMutableNotificationContent");
            if (unContentClass == IntPtr.Zero)
            {
                Console.WriteLine("[MacOSNotifications] UNMutableNotificationContent non disponibile");
                return;
            }
            var allocSel = sel_registerName("alloc");
            var initSel = sel_registerName("init");
            var content = objc_msgSend(unContentClass, allocSel);
            content = objc_msgSend(content, initSel);

            // 2. Imposta title e body
            var setTitleSel = sel_registerName("setTitle:");
            var setBodySel = sel_registerName("setBody:");
            objc_msgSend(content, setTitleSel, NSString(title));
            objc_msgSend(content, setBodySel, NSString(body));

            // 3. Crea un identifier univoco
            var nsUuidClass = objc_getClass("NSUUID");
            var uuid = objc_msgSend(nsUuidClass, sel_registerName("UUID"));
            var uuidString = objc_msgSend(uuid, sel_registerName("UUIDString"));

            // 4. Crea UNNotificationRequest
            var unRequestClass = objc_getClass("UNNotificationRequest");
            var requestSel = sel_registerName("requestWithIdentifier:content:trigger:");
            var request = objc_msgSend(unRequestClass, requestSel, uuidString, content, IntPtr.Zero);

            // 5. Aggiungilo al notification center
            var unCenter = objc_getClass("UNUserNotificationCenter");
            var center = objc_msgSend(unCenter, sel_registerName("currentNotificationCenter"));
            var addSel = sel_registerName("addNotificationRequest:withCompletionHandler:");
            objc_msgSend(center, addSel, request, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MacOSNotifications] Show errore: {ex.Message}");
        }
    }
}
