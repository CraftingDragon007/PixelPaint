using Avalonia.Media;
using PixelPaint.Models;
using PixelPaint.Services;

namespace PixelPaintTests;

public class FileServiceAxpTests
{
    private FileService _fileService = null!;
    private string _tempDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _fileService = new FileService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"PixelPaintTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
    }

    [Test]
    public void Axp_SingleColor_Roundtrip_IsTiny_AndPreservesPixels()
    {
        var image = CreateImage(32, 16, (_, _) => Colors.White);
        var path = GetTempFilePath("single.axp");

        _fileService.SaveImage(image, path, (640, 320));
        var result = _fileService.LoadImage(path);

        Assert.That(new FileInfo(path).Length, Is.LessThan(40));
        AssertImageEquals(image, result.image);
        Assert.That(result.editorSize, Is.EqualTo((640u, 320u)));
    }

    [Test]
    public void Axp_TwoColorCheckerboard_UsesCompactEncoding_AndRoundtrips()
    {
        const int width = 32;
        const int height = 32;
        var image = CreateImage(width, height, (x, y) => (x + y) % 2 == 0 ? Colors.Black : Colors.White);
        var path = GetTempFilePath("checker.axp");

        _fileService.SaveImage(image, path, (800, 600));
        var result = _fileService.LoadImage(path);

        const int rawArgbSize = width * height * 4;
        Assert.That(new FileInfo(path).Length, Is.LessThan(rawArgbSize));
        AssertImageEquals(image, result.image);
        Assert.That(result.editorSize, Is.EqualTo((800u, 600u)));
    }

    [Test]
    public void Axp_RleFriendlyImage_UsesVeryLittleSpace_AndRoundtrips()
    {
        const int width = 64;
        const int height = 32;
        var image = CreateImage(width, height, (_, y) => y < height / 2 ? Colors.Red : Colors.Blue);
        var path = GetTempFilePath("rle.axp");

        _fileService.SaveImage(image, path, (1024, 768));
        var result = _fileService.LoadImage(path);

        Assert.That(new FileInfo(path).Length, Is.LessThan(80));
        AssertImageEquals(image, result.image);
        Assert.That(result.editorSize, Is.EqualTo((1024u, 768u)));
    }

    [Test]
    public void Axp_ManyUniqueTransparentColors_Roundtrips()
    {
        var image = CreateImage(20, 20, (x, y) =>
            Color.FromArgb(
                (byte)((x * 11 + y * 7) % 256),
                (byte)((x * 13) % 256),
                (byte)((y * 17) % 256),
                (byte)((x * 19 + y * 23) % 256)));
        var path = GetTempFilePath("unique.axp");

        _fileService.SaveImage(image, path, (900, 700));
        var result = _fileService.LoadImage(path);

        AssertImageEquals(image, result.image);
        Assert.That(result.editorSize, Is.EqualTo((900u, 700u)));
    }

    [Test]
    public void Axp_LegacyVersion1_CanStillBeLoaded()
    {
        var path = GetTempFilePath("legacy.axp");
        WriteLegacyVersion1Axp(path);

        var result = _fileService.LoadImage(path);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.image.PixelCountX, Is.EqualTo(2));
            Assert.That(result.image.PixelCountY, Is.EqualTo(2));
            Assert.That(result.editorSize, Is.EqualTo((123u, 456u)));
            Assert.That(result.image.Pixels[0, 0], Is.EqualTo(Colors.Red));
            Assert.That(result.image.Pixels[1, 0], Is.EqualTo(Colors.Blue));
            Assert.That(result.image.Pixels[0, 1], Is.EqualTo(Colors.Blue));
            Assert.That(result.image.Pixels[1, 1], Is.EqualTo(Colors.Red));
        }
    }

    [Test]
    public void Axp_TruncatedRawPayload_Throws()
    {
        var path = GetTempFilePath("truncated.axp");
        using (var stream = File.Open(path, FileMode.Create, FileAccess.Write))
        {
            stream.Write("PPAF"u8);
            stream.WriteByte(2); // version
            stream.WriteByte(4); // raw argb encoding
            WriteUint(stream, 2);
            WriteUint(stream, 2);
            WriteUint(stream, 10);
            WriteUint(stream, 10);
            WriteUint(stream, 16); // payload length, but write too little data below
            stream.WriteByte(255);
            stream.WriteByte(0);
            stream.WriteByte(0);
            stream.WriteByte(0);
        }

        Assert.That(() => _fileService.LoadImage(path), Throws.TypeOf<EndOfStreamException>());
    }

    [Test]
    public void Axp_ThreeColorBitPackedImage_Roundtrips()
    {
        var image = CreateImage(8, 2, (x, y) => ((x + y) % 3) switch
        {
            0 => Colors.Red,
            1 => Colors.Green,
            _ => Colors.Blue
        });
        var path = GetTempFilePath("three-color.axp");

        _fileService.SaveImage(image, path, (640, 480));
        var result = _fileService.LoadImage(path);

        AssertImageEquals(image, result.image);
        Assert.That(result.editorSize, Is.EqualTo((640u, 480u)));
    }

    [TestCase("image1.jpg")]
    [TestCase("image2.png")]
    [TestCase("image3.jpg")]
    [TestCase("image_depth_1bit.png")]
    [TestCase("image_depth_4bit_palette.png")]
    [TestCase("image_depth_8bit_gray.png")]
    [TestCase("image_depth_8bit_palette.png")]
    [TestCase("image_depth_16bit_gray.png")]
    [TestCase("image_depth_24bit.bmp")]
    [TestCase("image_depth_32bit_rgba.png")]
    public void Axp_ImportedBitmapFromRepository_Roundtrips(string fileName)
    {
        var sourcePath = GetTestImagePath(fileName);
        Assert.That(File.Exists(sourcePath), $"Test image '{fileName}' was not found at '{sourcePath}'.");

        var importedImage = _fileService.ImportImage(sourcePath);
        var path = GetTempFilePath(Path.GetFileNameWithoutExtension(fileName) + ".axp");

        _fileService.SaveImage(importedImage, path, (640, 480));
        var result = _fileService.LoadImage(path);

        AssertImageEquals(importedImage, result.image);
        Assert.That(result.editorSize, Is.EqualTo((640u, 480u)));
    }


    private string GetTempFilePath(string fileName) => Path.Combine(_tempDirectory, fileName);

    private static string GetTestImagePath(string fileName) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", fileName);

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

    private static void AssertImageEquals(Image expected, Image actual)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(actual.PixelCountX, Is.EqualTo(expected.PixelCountX));
            Assert.That(actual.PixelCountY, Is.EqualTo(expected.PixelCountY));
            Assert.That(actual.PixelCount, Is.EqualTo(expected.PixelCount));
        }

        for (var y = 0; y < expected.PixelCountY; y++)
        for (var x = 0; x < expected.PixelCountX; x++)
            Assert.That(actual.Pixels[x, y], Is.EqualTo(expected.Pixels[x, y]), $"Pixel mismatch at ({x}, {y})");
    }

    private static void WriteLegacyVersion1Axp(string path)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
        stream.Write("PPAF"u8);
        stream.WriteByte(1);
        WriteUint(stream, 2);
        WriteUint(stream, 2);
        WriteUint(stream, 123);
        WriteUint(stream, 456);
        WriteUint(stream, 2);

        WriteColor(stream, Colors.Red);
        WriteColor(stream, Colors.Blue);

        // 2 colors => 1 bit per pixel, LSB first in row-major order: R, B, B, R => 0,1,1,0 => 0b00000110
        stream.WriteByte(0b0000_0110);
    }

    private static void WriteUint(Stream stream, uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        stream.Write(bytes, 0, 4);
    }

    private static void WriteColor(Stream stream, Color color)
    {
        stream.WriteByte(color.A);
        stream.WriteByte(color.R);
        stream.WriteByte(color.G);
        stream.WriteByte(color.B);
    }
}