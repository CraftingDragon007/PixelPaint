using PixelPaint.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PixelPaintTests;

public class FileServiceBitmapTests
{
    private FileService _fileService = null!;
    private string _tempDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _fileService = new FileService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"PixelPaintBitmapTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    [TestCase("jpg")]
    [TestCase("jpeg")]
    [TestCase("bmp")]
    [TestCase("gif")]
    [TestCase("pbm")]
    [TestCase("pgm")]
    [TestCase("ppm")]
    [TestCase("png")]
    [TestCase("tga")]
    [TestCase("tif")]
    [TestCase("tiff")]
    [TestCase("webp")]
    [TestCase("qoi")]
    public void ImportImage_SupportedExtensions_ImportsImage(string extension)
    {
        var path = GetTempFilePath($"white.{extension}");
        WriteWhiteImage(path);

        var image = _fileService.ImportImage(path);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(image.PixelCountX, Is.EqualTo(2));
            Assert.That(image.PixelCountY, Is.EqualTo(1));
            Assert.That(image.PixelCount, Is.EqualTo(2));
            Assert.That((int)image.Pixels[0, 0].A, Is.EqualTo(255));
            Assert.That((int)image.Pixels[1, 0].A, Is.EqualTo(255));
            Assert.That((int)image.Pixels[0, 0].R, Is.GreaterThanOrEqualTo(240));
            Assert.That((int)image.Pixels[0, 0].G, Is.GreaterThanOrEqualTo(240));
            Assert.That((int)image.Pixels[0, 0].B, Is.GreaterThanOrEqualTo(240));
            Assert.That((int)image.Pixels[1, 0].R, Is.GreaterThanOrEqualTo(240));
            Assert.That((int)image.Pixels[1, 0].G, Is.GreaterThanOrEqualTo(240));
            Assert.That((int)image.Pixels[1, 0].B, Is.GreaterThanOrEqualTo(240));
        }
    }

    [Test]
    public void ImportImage_Extension_IsCaseInsensitive()
    {
        var path = GetTempFilePath("white.WEBP");
        WriteWhiteImage(path);

        var image = _fileService.ImportImage(path);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(image.PixelCountX, Is.EqualTo(2));
            Assert.That(image.PixelCountY, Is.EqualTo(1));
            Assert.That(image.PixelCount, Is.EqualTo(2));
        }
    }

    [Test]
    public void ImportFileTypeFilter_IncludesAllSupportedFormats()
    {
        var filter = _fileService.ImportFileTypeFilter.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(filter.Patterns, Does.Contain("*.jpg"));
            Assert.That(filter.Patterns, Does.Contain("*.jpeg"));
            Assert.That(filter.Patterns, Does.Contain("*.bmp"));
            Assert.That(filter.Patterns, Does.Contain("*.gif"));
            Assert.That(filter.Patterns, Does.Contain("*.pbm"));
            Assert.That(filter.Patterns, Does.Contain("*.pgm"));
            Assert.That(filter.Patterns, Does.Contain("*.ppm"));
            Assert.That(filter.Patterns, Does.Contain("*.png"));
            Assert.That(filter.Patterns, Does.Contain("*.tga"));
            Assert.That(filter.Patterns, Does.Contain("*.tif"));
            Assert.That(filter.Patterns, Does.Contain("*.tiff"));
            Assert.That(filter.Patterns, Does.Contain("*.webp"));
            Assert.That(filter.Patterns, Does.Contain("*.qoi"));
            Assert.That(filter.MimeTypes, Does.Contain("image/jpeg"));
            Assert.That(filter.MimeTypes, Does.Contain("image/bmp"));
            Assert.That(filter.MimeTypes, Does.Contain("image/gif"));
            Assert.That(filter.MimeTypes, Does.Contain("image/x-portable-bitmap"));
            Assert.That(filter.MimeTypes, Does.Contain("image/x-portable-graymap"));
            Assert.That(filter.MimeTypes, Does.Contain("image/x-portable-pixmap"));
            Assert.That(filter.MimeTypes, Does.Contain("image/png"));
            Assert.That(filter.MimeTypes, Does.Contain("image/x-tga"));
            Assert.That(filter.MimeTypes, Does.Contain("image/tiff"));
            Assert.That(filter.MimeTypes, Does.Contain("image/webp"));
            Assert.That(filter.MimeTypes, Does.Contain("image/qoi"));
        }
    }

    private string GetTempFilePath(string fileName) => Path.Combine(_tempDirectory, fileName);

    private static void WriteWhiteImage(string path)
    {
        using var image = new Image<Rgba32>(2, 1);
        var white = new Rgba32(255, 255, 255, 255);
        image[0, 0] = white;
        image[1, 0] = white;

        try
        {
            image.Save(path);
        }
        catch (UnknownImageFormatException)
        {
            // If a specific encoder is not available, write PNG bytes and keep the
            // extension to still validate extension-based import routing.
            image.SaveAsPng(path);
        }
    }
}

