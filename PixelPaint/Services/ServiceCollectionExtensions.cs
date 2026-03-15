using Microsoft.Extensions.DependencyInjection;
using PixelPaint.Localization;
using PixelPaint.ViewModels;

namespace PixelPaint.Services;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this ServiceCollection services)
    {
        services.AddSingleton(LocalizationService.Instance);
        services.AddTransient<IFileService, FileService>();
        services.AddSingleton<EditorZoomController>();
        services.AddTransient<IDrawingService, DrawingService>();

        services.AddSingleton<MainWindowViewModel>();
    }
}