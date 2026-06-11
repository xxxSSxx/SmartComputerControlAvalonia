using System;
using System.IO;
using System.Text.Json;

namespace SmartComputerControlAvalonia.Services;

public class AppConfig
{
    public string Path { get; set; } = "";
    public string ComputerName { get; set; } = "";
    public bool Predefinito { get; set; } = false;
}

public class ConfigService
{
    private readonly string _configPath;
    private AppConfig _config;

    public ConfigService()
    {
        // Cartella per-utente standard di sistema
        // Win:   %APPDATA%\SmartComputerControl\config.json
        // Mac:   ~/Library/Application Support/SmartComputerControl/config.json
        // Linux: ~/.config/SmartComputerControl/config.json
        var folder = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SmartComputerControl");
        Directory.CreateDirectory(folder);
        _configPath = System.IO.Path.Combine(folder, "config.json");

        _config = Load();
    }

    public string Path
    {
        get => _config.Path;
        set => _config.Path = value;
    }

    public string ComputerName
    {
        get => _config.ComputerName;
        set => _config.ComputerName = value;
    }

    public bool Predefinito
    {
        get => _config.Predefinito;
        set => _config.Predefinito = value;
    }

    private AppConfig Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg != null) return cfg;
            }
        }
        catch { }
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_config, opts);
            File.WriteAllText(_configPath, json);
        }
        catch { }
    }
}
