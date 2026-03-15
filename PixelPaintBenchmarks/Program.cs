using Avalonia;
using Avalonia.ReactiveUI;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PixelPaint;
using PixelPaint.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


BenchmarkRunner.Run<BitmapImportBenchmarks>();

/// <summary>
/// Benchmarks import performance across the supported raster formats.
/// <list type="bullet">
///   <item><b>Avalonia-native decode</b> for common formats like PNG/JPEG/BMP.</item>
///   <item><b>ImageSharp fallback/decode</b> for extended formats and special bit depths.</item>
/// </list>
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 10)]
public class BitmapImportBenchmarks
{
    // ── paths resolved relative to the benchmark binary ──────────────────────
    private static readonly string TestDataDir =
        Path.Combine(AppContext.BaseDirectory, "TestData");
    private static readonly string GeneratedDataDir =
        Path.Combine(AppContext.BaseDirectory, "GeneratedTestData");

    // Avalonia native path ─────────────────────────────────────────────────────
    private string _rgba32PngPath = null!;   // ImportViaAvaloniaCopyPixels
    private string _bmpPath      = null!;   // ImportViaAvaloniaCopyPixels
    private string _jpegPath     = null!;   // ImportViaAvaloniaCopyPixels

    // ImageSharp fallback path ────────────────────────────────────────────────
    private string _palette4bitPath  = null!;  // ImportViaImageSharp
    private string _palette8bitPath  = null!;  // ImportViaImageSharp
    private string _gray8bitPath     = null!;  // ImportViaImageSharp
    private string _gray16bitPath    = null!;  // ImportViaImageSharp
    private string _mono1bitPath     = null!;  // ImportViaImageSharp
    private string _gifPath          = null!;
    private string _pbmPath          = null!;
    private string _pgmPath          = null!;
    private string _ppmPath          = null!;
    private string _tgaPath          = null!;
    private string _tiffPath         = null!;
    private string _webpPath         = null!;
    private string _qoiPath          = null!;

    private FileService _fileService = null!;

    [GlobalSetup]
    public void Setup()
    {
        // ── Avalonia bootstrap ─────────────────────────────────────────────────
        // Must run in each BenchmarkDotNet child process (top-level Program.cs
        // statements are not executed in child processes).
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .SetupWithoutStarting();

        _fileService = new FileService();

        _rgba32PngPath  = Path.Combine(TestDataDir, "image_depth_32bit_rgba.png");
        _bmpPath        = Path.Combine(TestDataDir, "image_depth_24bit.bmp");
        _jpegPath       = Path.Combine(TestDataDir, "image1.jpg");
        _palette4bitPath = Path.Combine(TestDataDir, "image_depth_4bit_palette.png");
        _palette8bitPath = Path.Combine(TestDataDir, "image_depth_8bit_palette.png");
        _gray8bitPath   = Path.Combine(TestDataDir, "image_depth_8bit_gray.png");
        _gray16bitPath  = Path.Combine(TestDataDir, "image_depth_16bit_gray.png");
        _mono1bitPath   = Path.Combine(TestDataDir, "image_depth_1bit.png");

        Directory.CreateDirectory(GeneratedDataDir);
        _gifPath = CreateGeneratedSample("sample.gif");
        _pbmPath = CreateGeneratedSample("sample.pbm");
        _pgmPath = CreateGeneratedSample("sample.pgm");
        _ppmPath = CreateGeneratedSample("sample.ppm");
        _tgaPath = CreateGeneratedSample("sample.tga");
        _tiffPath = CreateGeneratedSample("sample.tiff");
        _webpPath = CreateGeneratedSample("sample.webp");
        _qoiPath = CreateGeneratedSample("sample.qoi");
    }

    // ── Avalonia CopyPixels path ──────────────────────────────────────────────

    [Benchmark(Description = "Avalonia – RGBA-32 PNG")]
    public object ImportRgba32Png() => _fileService.ImportImage(_rgba32PngPath);

    [Benchmark(Description = "Avalonia – 24-bit BMP")]
    public object ImportBmp24() => _fileService.ImportImage(_bmpPath);

    [Benchmark(Description = "Avalonia – JPEG")]
    public object ImportJpeg() => _fileService.ImportImage(_jpegPath);

    // ── ImageSharp fallback path ──────────────────────────────────────────────

    [Benchmark(Description = "ImageSharp – 4-bit palette PNG")]
    public object ImportPalette4bit() => _fileService.ImportImage(_palette4bitPath);

    [Benchmark(Description = "ImageSharp – 8-bit palette PNG")]
    public object ImportPalette8bit() => _fileService.ImportImage(_palette8bitPath);

    [Benchmark(Description = "ImageSharp – 8-bit grayscale PNG")]
    public object ImportGray8bit() => _fileService.ImportImage(_gray8bitPath);

    [Benchmark(Description = "ImageSharp – 16-bit grayscale PNG")]
    public object ImportGray16bit() => _fileService.ImportImage(_gray16bitPath);

    [Benchmark(Description = "ImageSharp – 1-bit monochrome PNG")]
    public object ImportMono1bit() => _fileService.ImportImage(_mono1bitPath);

    [Benchmark(Description = "ImageSharp – GIF")]
    public object ImportGif() => _fileService.ImportImage(_gifPath);

    [Benchmark(Description = "ImageSharp – PBM")]
    public object ImportPbm() => _fileService.ImportImage(_pbmPath);

    [Benchmark(Description = "ImageSharp – PGM")]
    public object ImportPgm() => _fileService.ImportImage(_pgmPath);

    [Benchmark(Description = "ImageSharp – PPM")]
    public object ImportPpm() => _fileService.ImportImage(_ppmPath);

    [Benchmark(Description = "ImageSharp – TGA")]
    public object ImportTga() => _fileService.ImportImage(_tgaPath);

    [Benchmark(Description = "ImageSharp – TIFF")]
    public object ImportTiff() => _fileService.ImportImage(_tiffPath);

    [Benchmark(Description = "ImageSharp – WebP")]
    public object ImportWebp() => _fileService.ImportImage(_webpPath);

    [Benchmark(Description = "ImageSharp – QOI")]
    public object ImportQoi() => _fileService.ImportImage(_qoiPath);

    private static string CreateGeneratedSample(string fileName)
    {
        var outputPath = Path.Combine(GeneratedDataDir, fileName);
        using var image = CreateGradientImage(256, 256);
        image.Save(outputPath);
        return outputPath;
    }

    private static Image<Rgba32> CreateGradientImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32(
                    (byte)(x % 256),
                    (byte)(y % 256),
                    (byte)((x * 3 + y * 5) % 256),
                    255);
            }
        }

        return image;
    }
}
