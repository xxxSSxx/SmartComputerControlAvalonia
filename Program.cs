using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;

namespace SmartComputerControlAvalonia;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Su macOS: impostiamo l'app come "accessory" (no Dock icon) PRIMA che
        // Avalonia inizializzi NSApplication, altrimenti l'icona resta visibile
        // nel Dock per tutta la sessione.
        if (OperatingSystem.IsMacOS())
        {
            MacOSNotifications.SetAccessoryActivationPolicy();
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, lifetime =>
        {
            lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        });
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
