using Avalonia.Media;
using PixelPaint.Models;
using PixelPaint.Services;

namespace PixelPaintTests;

public class FileServiceSvgTests
{
    private FileService _fileService = null!;

    [SetUp]
    public void Setup()
    {
        _fileService = new FileService();
    }

    [Test]
    public void GetSvgSaveWarning_SmallImage_DoesNotWarn()
    {
        var image = CreateImage(32, 32, (_, _) => Colors.White);

        var (estimatedSizeBytes, shouldWarn) = _fileService.GetSvgSaveWarning(image);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(estimatedSizeBytes, Is.GreaterThan(0));
            Assert.That(shouldWarn, Is.False);
        }
    }

    [Test]
    public void GetSvgSaveWarning_LargeImage_Warns()
    {
        var image = CreateImage(512, 512, (_, _) => Colors.White);

        var (_, shouldWarn) = _fileService.GetSvgSaveWarning(image);

        Assert.That(shouldWarn, Is.True);
    }

    [Test]
    public void GetSvgSaveWarning_Transparency_IncreasesEstimate()
    {
        var opaque = CreateImage(128, 128, (_, _) => Colors.Red);
        var transparent = CreateImage(128, 128, (_, _) => Color.FromArgb(120, 255, 0, 0));

        var (opaqueEstimate, _) = _fileService.GetSvgSaveWarning(opaque);
        var (transparentEstimate, _) = _fileService.GetSvgSaveWarning(transparent);

        Assert.That(transparentEstimate, Is.GreaterThan(opaqueEstimate));
    }

    private static Image CreateImage(int width, int height, Func<int, int, Color> colorFactory)
    {
        var image = new Image
        {
            PixelCountX = width,
            PixelCountY = height,
            PixelCount = width * height,
            Pixels = new Color[width, height]
        };

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            image.Pixels[x, y] = colorFactory(x, y);

        return image;
    }
}

