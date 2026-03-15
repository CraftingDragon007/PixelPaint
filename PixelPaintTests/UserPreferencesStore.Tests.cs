using PixelPaint.Services;
using System.Text.Json;

namespace PixelPaintTests;

public class UserPreferencesStoreTests
{
    private string _tempDirectoryPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _tempDirectoryPath = Path.Combine(Path.GetTempPath(), "PixelPaintTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectoryPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectoryPath))
            Directory.Delete(_tempDirectoryPath, recursive: true);
    }

    [Test]
    public void Load_ReturnsDefault_WhenSettingsFileDoesNotExist()
    {
        var store = CreateStore();

        var preferences = store.Load();

        Assert.That(preferences.PreferredCulture, Is.Null);
    }

    [Test]
    public void Save_ThenLoad_RoundtripsPreferredCulture()
    {
        var store = CreateStore();

        store.Save(new UserPreferences { PreferredCulture = "de" });
        var reloadedPreferences = store.Load();

        Assert.That(reloadedPreferences.PreferredCulture, Is.EqualTo("de"));
    }

    [Test]
    public void Save_WritesVersionToConfigFile()
    {
        var settingsFilePath = Path.Combine(_tempDirectoryPath, "settings.json");
        var store = new UserPreferencesStore(settingsFilePath);

        store.Save(new UserPreferences { PreferredCulture = "fr" });

        using var document = JsonDocument.Parse(File.ReadAllText(settingsFilePath));
        var root = document.RootElement;
        Assert.That(root.GetProperty("Version").GetInt32(), Is.EqualTo(1));
        Assert.That(root.GetProperty("PreferredCulture").GetString(), Is.EqualTo("fr"));
    }

    [Test]
    public void Load_ReadsConfigWithoutVersion()
    {
        var settingsFilePath = Path.Combine(_tempDirectoryPath, "settings.json");
        File.WriteAllText(settingsFilePath, "{" +
                                           "\"PreferredCulture\":\"es\"" +
                                           "}");
        var store = new UserPreferencesStore(settingsFilePath);

        var preferences = store.Load();

        Assert.That(preferences.PreferredCulture, Is.EqualTo("es"));
    }

    [Test]
    public void Load_ReturnsDefault_WhenConfigVersionIsUnsupported()
    {
        var settingsFilePath = Path.Combine(_tempDirectoryPath, "settings.json");
        File.WriteAllText(settingsFilePath, "{" +
                                           "\"Version\":99," +
                                           "\"PreferredCulture\":\"de\"" +
                                           "}");
        var store = new UserPreferencesStore(settingsFilePath);

        var preferences = store.Load();

        Assert.That(preferences.PreferredCulture, Is.Null);
    }

    [Test]
    public void Load_ReturnsDefault_WhenSettingsFileContainsInvalidJson()
    {
        var settingsFilePath = Path.Combine(_tempDirectoryPath, "settings.json");
        File.WriteAllText(settingsFilePath, "this is not json");
        var store = new UserPreferencesStore(settingsFilePath);

        var preferences = store.Load();

        Assert.That(preferences.PreferredCulture, Is.Null);
    }

    private UserPreferencesStore CreateStore()
    {
        var settingsFilePath = Path.Combine(_tempDirectoryPath, "settings.json");
        return new UserPreferencesStore(settingsFilePath);
    }
}

