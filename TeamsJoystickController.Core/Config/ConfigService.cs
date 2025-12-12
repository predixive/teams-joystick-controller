using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TeamsJoystickController.Core.Logging;

namespace TeamsJoystickController.Core.Config;

public class ConfigService
{
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configFilePath = Path.Combine(appData, "TeamsJoystickController", "config.json");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public string ConfigFilePath => _configFilePath;

    public AppConfig Load()
    {
        if (!File.Exists(_configFilePath))
        {
            Log.Info("Configuration file not found. Creating default configuration.");
            return CreateAndPersistDefaultConfig();
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
            if (config == null)
            {
                Log.Error("Configuration file was empty or invalid. Using default configuration.");
                return CreateAndPersistDefaultConfig();
            }

            return config;
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load configuration. Using default configuration.", ex);
            return CreateAndPersistDefaultConfig();
        }
    }

    public void Save(AppConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        EnsureDirectory();
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(_configFilePath, json);
    }

    private AppConfig CreateAndPersistDefaultConfig()
    {
        var defaultConfig = CreateDefaultConfig();
        Save(defaultConfig);
        return defaultConfig;
    }

    private void EnsureDirectory()
    {
        var directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            Buttons = new Dictionary<int, ButtonConfig>
            {
                [1] = new ButtonConfig { Single = "ToggleMute", Double = "ToggleCamera" },
                [2] = new ButtonConfig { Single = "ShareScreenPreferred", Double = "OpenShareTray" },
                [3] = new ButtonConfig { Single = "React:Like", Double = "React:Love", Triple = "React:Clap" },
                [4] = new ButtonConfig { Single = "ToggleHand" },
                [5] = new ButtonConfig { Single = "Spare" }
            },
            Teams = new TeamsConfig
            {
                SharePreferredMonitorIndex = 1
            },
            Timing = new TimingConfig
            {
                DoublePressThresholdMs = 250,
                TriplePressThresholdMs = 350
            }
        };
    }
}
