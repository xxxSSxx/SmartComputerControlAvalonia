using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartComputerControlAvalonia.Views;

public partial class MainWindow : Window
{
    private bool _initializing = true;

    public MainWindow()
    {
        InitializeComponent();
        LoadFromConfig();
        _initializing = false;
    }

    private void LoadFromConfig()
    {
        CartellaBox.Text = App.Config.Path;
        NomeBox.Text = App.Config.ComputerName;
        PredefinitoCheck.IsChecked = App.Config.Predefinito;
        AutoavvioCheck.IsChecked = App.Autostart.IsEnabled();
        ConfermaCheck.IsChecked = App.Config.MostraConferma;
        SecondiBox.Value = App.Config.SecondiConferma;
        DebugCheck.IsChecked = App.Config.DebugMode;
    }

    private async void OnBrowseClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Seleziona la cartella monitorata",
                AllowMultiple = false
            });

        if (folders.Count > 0)
        {
            var path = folders[0].Path.LocalPath;
            if (Directory.Exists(path))
            {
                CartellaBox.Text = path;
                App.Config.Path = path;
                App.Config.Save();
                App.Watcher.Restart();
            }
        }
    }

    private void OnNomeChanged(object? sender, TextChangedEventArgs e)
    {
        if (_initializing) return;
        App.Config.ComputerName = NomeBox.Text ?? "";
        App.Config.Save();
    }

    private void OnPredefinitoChanged(object? sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        App.Config.Predefinito = PredefinitoCheck.IsChecked ?? false;
        App.Config.Save();
    }

    private void OnAutoavvioChanged(object? sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        if (AutoavvioCheck.IsChecked == true)
            App.Autostart.Enable();
        else
            App.Autostart.Disable();
    }

    private void OnConfermaChanged(object? sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        App.Config.MostraConferma = ConfermaCheck.IsChecked ?? false;
        App.Config.Save();
    }

    private void OnSecondiChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (_initializing) return;
        App.Config.SecondiConferma = (int)(e.NewValue ?? 30);
        App.Config.Save();
    }

    private void OnDebugChanged(object? sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        var debug = DebugCheck.IsChecked ?? false;
        App.Config.DebugMode = debug;
        App.Config.Save();
        // Propaga immediatamente al CommandService
        App.Commands.DebugMode = debug;
    }
}
