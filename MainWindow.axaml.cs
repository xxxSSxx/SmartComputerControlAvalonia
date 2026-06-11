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
}
