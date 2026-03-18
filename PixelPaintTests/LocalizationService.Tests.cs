using System.Globalization;
using System.ComponentModel;
using PixelPaint.Localization;

namespace PixelPaintTests;

public class LocalizationServiceTests
{
    private CultureInfo? _originalCulture;
    private CultureInfo? _originalUiCulture;

    [SetUp]
    public void SetUp()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        _originalUiCulture = CultureInfo.CurrentUICulture;
    }

    [TearDown]
    public void TearDown()
    {
        if (_originalCulture is not null)
            CultureInfo.CurrentCulture = _originalCulture;

        if (_originalUiCulture is not null)
            CultureInfo.CurrentUICulture = _originalUiCulture;

        if (_originalCulture is not null)
            CultureInfo.DefaultThreadCurrentCulture = _originalCulture;

        if (_originalUiCulture is not null)
            CultureInfo.DefaultThreadCurrentUICulture = _originalUiCulture;

        LocalizationService.Instance.SetCulture("en");
    }

    [Test]
    public void ResolveSupportedCulture_MapsToEnglishFallback_WhenCultureIsUnsupported()
    {
        var culture = LocalizationService.ResolveSupportedCulture(new CultureInfo("pl-PL"));

        Assert.That(culture.Name, Is.EqualTo("en"));
    }

    [Test]
    public void SupportedCultures_ContainsCulturesFromResourceFiles()
    {
        var supportedCultureNames = LocalizationService.Instance.SupportedCultures
            .Select(culture => culture.Name)
            .ToArray();

        Assert.That(supportedCultureNames, Does.Contain("en"));
        Assert.That(supportedCultureNames, Does.Contain("de"));
        Assert.That(supportedCultureNames, Does.Contain("fr"));
        Assert.That(supportedCultureNames, Does.Contain("es"));
        Assert.That(supportedCultureNames, Does.Contain("it"));
    }

    [Test]
    public void ResolveSupportedCulture_MapsToFrench_WhenCultureIsFrench()
    {
        var culture = LocalizationService.ResolveSupportedCulture(new CultureInfo("fr-CA"));

        Assert.That(culture.Name, Is.EqualTo("fr"));
    }

    [Test]
    public void ResolveSupportedCulture_MapsToSpanish_WhenCultureIsSpanish()
    {
        var culture = LocalizationService.ResolveSupportedCulture(new CultureInfo("es-MX"));

        Assert.That(culture.Name, Is.EqualTo("es"));
    }

    [Test]
    public void ResolveSupportedCulture_MapsToItalian_WhenCultureIsItalian()
    {
        var culture = LocalizationService.ResolveSupportedCulture(new CultureInfo("it-IT"));

        Assert.That(culture.Name, Is.EqualTo("it"));
    }

    [Test]
    public void SetCulture_SwitchesTranslationsToGerman()
    {
        LocalizationService.Instance.SetCulture("de");

        Assert.That(LocalizationService.Instance["Menu_File"], Is.EqualTo("Datei"));
    }

    [Test]
    public void SetCulture_SwitchesTranslationsToFrench()
    {
        LocalizationService.Instance.SetCulture("fr");

        Assert.That(LocalizationService.Instance["Menu_File"], Is.EqualTo("Fichier"));
    }

    [Test]
    public void SetCulture_SwitchesTranslationsToSpanish()
    {
        LocalizationService.Instance.SetCulture("es");

        Assert.That(LocalizationService.Instance["Menu_File"], Is.EqualTo("Archivo"));
    }

    [Test]
    public void SetCulture_SwitchesTranslationsToItalian()
    {
        LocalizationService.Instance.SetCulture("it");

        Assert.That(LocalizationService.Instance["Menu_Edit"], Is.EqualTo("Modifica"));
    }

    [Test]
    public void MenuFileImport_UsesShortFormatList()
    {
        LocalizationService.Instance.SetCulture("en");
        Assert.That(LocalizationService.Instance["Menu_File_Import"], Is.EqualTo("Import (png, jpg, bmp, ...)"));
    }

    [Test]
    public void SetCulture_RaisesCultureChanged_WhenLanguageChanges()
    {
        CultureInfo? changedCulture = null;
        void OnCultureChanged(object? _, CultureInfo culture) => changedCulture = culture;

        LocalizationService.Instance.CultureChanged += OnCultureChanged;
        try
        {
            LocalizationService.Instance.SetCulture("de");
        }
        finally
        {
            LocalizationService.Instance.CultureChanged -= OnCultureChanged;
        }

        Assert.That(changedCulture?.Name, Is.EqualTo("de"));
    }

    [Test]
    public void SetCulture_RaisesIndexerPropertyChanged_WhenLanguageChanges()
    {
        var raisedProperties = new List<string>();

        void OnPropertyChanged(object? _, PropertyChangedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.PropertyName))
                raisedProperties.Add(args.PropertyName);
        }

        LocalizationService.Instance.PropertyChanged += OnPropertyChanged;
        try
        {
            LocalizationService.Instance.SetCulture("de");
        }
        finally
        {
            LocalizationService.Instance.PropertyChanged -= OnPropertyChanged;
        }

        Assert.That(raisedProperties, Does.Contain("Item").Or.Contain("Item[]"));
    }
}

