using PixelPaint.Services;

namespace PixelPaintTests;

public class FileServiceBitmapTests
{
    private static readonly string TestDataDirectory = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData");

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

    [TestCase("image1.jpg", 2400, 1600)]
    [TestCase("image_depth_24bit.bmp", 1800, 1200)]
    [TestCase("image_depth_32bit_rgba.png", 1920, 1080)]
    [TestCase("image_format_gif.gif", 1920, 1080)]
    [TestCase("image_format_pbm.pbm", 1920, 1080)]
    [TestCase("image_format_pgm.pgm", 1920, 1080)]
    [TestCase("image_format_ppm.ppm", 1920, 1080)]
    [TestCase("image_format_tga.tga", 1920, 1080)]
    [TestCase("image_format_tiff.tiff", 1920, 1080)]
    [TestCase("image_format_webp.webp", 1920, 1080)]
    [TestCase("image_format_qoi.qoi", 1920, 1080)]
    public void ImportImage_SupportedFormats_ImportsImage(string fileName, int expectedWidth, int expectedHeight)
    {
        var path = GetTestDataPath(fileName);

        var image = _fileService.ImportImage(path);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(image.PixelCountX, Is.EqualTo(expectedWidth));
            Assert.That(image.PixelCountY, Is.EqualTo(expectedHeight));
            Assert.That(image.PixelCount, Is.EqualTo(expectedWidth * expectedHeight));
        }
    }

    [Test]
    public void ImportImage_Extension_IsCaseInsensitive()
    {
        var sourcePath = GetTestDataPath("image_format_webp.webp");
        var path = GetTempFilePath("copy.WEBP");
        File.Copy(sourcePath, path, overwrite: true);

        var image = _fileService.ImportImage(path);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(image.PixelCountX, Is.EqualTo(1920));
            Assert.That(image.PixelCountY, Is.EqualTo(1080));
            Assert.That(image.PixelCount, Is.EqualTo(1920 * 1080));
        }
    }

    [Test]
    public void ImportImage_JpegExtension_ImportsImage()
    {
        var sourcePath = GetTestDataPath("image1.jpg");
        var path = GetTempFilePath("image1.jpeg");
        File.Copy(sourcePath, path, overwrite: true);

        var image = _fileService.ImportImage(path);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(image.PixelCountX, Is.EqualTo(2400));
            Assert.That(image.PixelCountY, Is.EqualTo(1600));
            Assert.That(image.PixelCount, Is.EqualTo(2400 * 1600));
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

    [Test]
    public void ImportImage_UnsupportedExtension_ThrowsArgumentException()
    {
        var path = GetTempFilePath("unsupported.txt");
        File.WriteAllText(path, "not an image");

        Assert.That(() => _fileService.ImportImage(path), Throws.TypeOf<ArgumentException>());
    }

    private string GetTempFilePath(string fileName) => Path.Combine(_tempDirectory, fileName);

    private static string GetTestDataPath(string fileName) => Path.Combine(TestDataDirectory, fileName);
}

