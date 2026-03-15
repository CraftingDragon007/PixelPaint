using Avalonia;
using Avalonia.ReactiveUI;
using PixelPaint;

namespace PixelPaintTests;

[SetUpFixture]
public sealed class AvaloniaTestSetup
{
    [OneTimeSetUp]
    public void InitializeAvalonia()
    {
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .SetupWithoutStarting();
    }
}

