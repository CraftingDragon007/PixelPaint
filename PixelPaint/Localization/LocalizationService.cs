using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace PixelPaint.Localization;

public sealed class LocalizationService : INotifyPropertyChanged
{
    private const string ResourceBaseName = "PixelPaint.Resources.Strings";
    private static readonly CultureInfo FallbackCulture = new("en");
    private static readonly ResourceManager ResourceManager = new(ResourceBaseName, typeof(LocalizationService).Assembly);
    private static readonly Lazy<IReadOnlyList<CultureInfo>> SupportedCultureCache = new(DiscoverSupportedCultures);

    private CultureInfo _currentCulture;

    public LocalizationService()
    {
        _currentCulture = ResolveSupportedCulture(CultureInfo.CurrentUICulture);
        ApplyCulture(_currentCulture);
    }

    public static LocalizationService Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<CultureInfo>? CultureChanged;

    public CultureInfo CurrentCulture => _currentCulture;

    public IReadOnlyList<CultureInfo> SupportedCultures => SupportedCultureCache.Value;

    public string this[string key]
    {
        get
        {
            var localizedValue = ResourceManager.GetString(key, _currentCulture)
                                 ?? ResourceManager.GetString(key, FallbackCulture);
            return string.IsNullOrWhiteSpace(localizedValue) ? key : localizedValue;
        }
    }

    public string Format(string key, params object[] args) =>
        string.Format(_currentCulture, this[key], args);

    public void SetCulture(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return;

        try
        {
            SetCulture(CultureInfo.GetCultureInfo(cultureName));
        }
        catch (CultureNotFoundException)
        {
            // Ignore invalid persisted culture values and keep current culture.
        }
    }

    public void SetCulture(CultureInfo culture)
    {
        var supportedCulture = ResolveSupportedCulture(culture);
        if (_currentCulture.Name.Equals(supportedCulture.Name, StringComparison.OrdinalIgnoreCase))
            return;

        _currentCulture = supportedCulture;
        ApplyCulture(_currentCulture);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        CultureChanged?.Invoke(this, _currentCulture);
    }

    public static CultureInfo ResolveSupportedCulture(CultureInfo? requestedCulture)
    {
        var supportedCultures = SupportedCultureCache.Value;
        if (requestedCulture is null)
            return FallbackCulture;

        var exactMatch = supportedCultures.FirstOrDefault(culture =>
            culture.Name.Equals(requestedCulture.Name, StringComparison.OrdinalIgnoreCase));
        if (exactMatch is not null)
            return exactMatch;

        var languageCode = requestedCulture.TwoLetterISOLanguageName;
        var languageMatch = supportedCultures.FirstOrDefault(culture =>
            culture.TwoLetterISOLanguageName.Equals(languageCode, StringComparison.OrdinalIgnoreCase));

        return languageMatch ?? FallbackCulture;
    }

    private static IReadOnlyList<CultureInfo> DiscoverSupportedCultures()
    {
        var cultureByName = new Dictionary<string, CultureInfo>(StringComparer.OrdinalIgnoreCase)
        {
            [FallbackCulture.Name] = FallbackCulture
        };

        var probeCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures)
            .Where(culture => !string.IsNullOrWhiteSpace(culture.Name));

        foreach (var culture in probeCultures)
        {
            var resourceSet = ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
            if (resourceSet is null)
                continue;

            var normalizedCulture = CultureInfo.GetCultureInfo(culture.Name);
            cultureByName[normalizedCulture.Name] = normalizedCulture;
        }

        return cultureByName.Values
            .OrderBy(culture => culture.NativeName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}

