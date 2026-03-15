using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
        LocalizationService.Instance.SetCulture(System.Globalization.CultureInfo.CurrentUICulture);

        var collection = new ServiceCollection();
        collection.AddCommonServices();

        var services = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new EditWindow(
                services.GetRequiredService<IDrawingService>(),
                services.GetRequiredService<IFileService>(),
                services.GetRequiredService<EditorZoomController>(),
                services.GetRequiredService<LocalizationService>());

        base.OnFrameworkInitializationCompleted();
    }
}