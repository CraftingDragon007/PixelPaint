using PixelPaint.Services;

namespace PixelPaintTests;

public class FileServiceBitmapTests
{
    private const string WhiteJpegBase64 =
        "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAIDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD3+iiigD//2Q==";

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

    [Test]
    public void ImportImage_JpegExtension_ImportsImage()
    {
        var path = GetTempFilePath("white.jpeg");
        WriteWhiteJpeg(path);

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
    public void ImportImage_JpgExtension_IsCaseInsensitive()
    {
        var path = GetTempFilePath("white.JPG");
        WriteWhiteJpeg(path);

        var image = _fileService.ImportImage(path);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(image.PixelCountX, Is.EqualTo(2));
            Assert.That(image.PixelCountY, Is.EqualTo(1));
            Assert.That(image.PixelCount, Is.EqualTo(2));
        }
    }

    [Test]
    public void ImportFileTypeFilter_IncludesJpgAndJpeg()
    {
        var filter = _fileService.ImportFileTypeFilter.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(filter.Patterns, Does.Contain("*.jpg"));
            Assert.That(filter.Patterns, Does.Contain("*.jpeg"));
            Assert.That(filter.MimeTypes, Does.Contain("image/jpeg"));
        }
    }

    private string GetTempFilePath(string fileName) => Path.Combine(_tempDirectory, fileName);

    private static void WriteWhiteJpeg(string path) =>
        File.WriteAllBytes(path, Convert.FromBase64String(WhiteJpegBase64));
}

