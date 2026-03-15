using System;
using System.IO;
using System.Text.Json;

namespace PixelPaint.Services;

public sealed class UserPreferencesStore : IUserPreferencesStore
{
    private const int CurrentSettingsVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _settingsFilePath;

    public UserPreferencesStore(string? settingsFilePath = null)
    {
        _settingsFilePath = string.IsNullOrWhiteSpace(settingsFilePath)
            ? GetDefaultSettingsFilePath()
            : settingsFilePath;
    }

    public UserPreferences Load()
    {
        if (!File.Exists(_settingsFilePath))
            return new UserPreferences();

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var settingsFile = JsonSerializer.Deserialize<SettingsFile>(json, JsonOptions);

            if (settingsFile?.Version is null)
                return JsonSerializer.Deserialize<UserPreferences>(json, JsonOptions) ?? new UserPreferences();

            if (settingsFile.Version != CurrentSettingsVersion)
                return new UserPreferences();

            return settingsFile.ToUserPreferences();
        }
        catch
        {
            return new UserPreferences();
        }
    }

    public void Save(UserPreferences preferences)
    {
        var directoryPath = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
            Directory.CreateDirectory(directoryPath);

        var settingsFile = SettingsFile.FromUserPreferences(preferences);
        var json = JsonSerializer.Serialize(settingsFile, JsonOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    private sealed class SettingsFile
    {
        public int? Version { get; init; }

        public string? PreferredCulture { get; init; }

        public UserPreferences ToUserPreferences() => new()
        {
            PreferredCulture = PreferredCulture
        };

        public static SettingsFile FromUserPreferences(UserPreferences preferences) => new()
        {
            Version = CurrentSettingsVersion,
            PreferredCulture = preferences.PreferredCulture
        };
    }

    private static string GetDefaultSettingsFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appDataPath))
            appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

        return Path.Combine(appDataPath, "PixelPaint", "settings.json");
    }
}

