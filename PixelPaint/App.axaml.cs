using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using PixelPaint.Localization;
using PixelPaint.Services;
using PixelPaint.Views;

namespace PixelPaint;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        collection.AddCommonServices();

        var services = collection.BuildServiceProvider();
        var localizationService = services.GetRequiredService<LocalizationService>();
        var userPreferencesStore = services.GetRequiredService<IUserPreferencesStore>();

        var preferredCulture = userPreferencesStore.Load().PreferredCulture;
        if (string.IsNullOrWhiteSpace(preferredCulture))
            localizationService.SetCulture(CultureInfo.CurrentUICulture);
        else
            localizationService.SetCulture(preferredCulture);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new EditWindow(
                services.GetRequiredService<IDrawingService>(),
                services.GetRequiredService<IFileService>(),
                services.GetRequiredService<EditorZoomController>(),
                localizationService,
                userPreferencesStore);

        base.OnFrameworkInitializationCompleted();
    }
}