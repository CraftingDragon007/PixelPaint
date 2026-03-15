using Avalonia;
using Avalonia.ReactiveUI;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PixelPaint;
using PixelPaint.Services;


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

    // Avalonia native path ─────────────────────────────────────────────────────
    private string _rgba32PngPath = null!;   // ImportViaAvaloniaCopyPixels
    private string _bmpPath      = null!;   // ImportViaAvaloniaCopyPixels
    private string _jpegPath     = null!;   // ImportViaAvaloniaCopyPixels

    // ImageSharp fallback path ────────────────────────────────────────────────
    private string _palette4BitPath  = null!;  // ImportViaImageSharp
    private string _palette8BitPath  = null!;  // ImportViaImageSharp
    private string _gray8BitPath     = null!;  // ImportViaImageSharp
    private string _gray16BitPath    = null!;  // ImportViaImageSharp
    private string _mono1BitPath     = null!;  // ImportViaImageSharp
    private string _gifPath          = null!;
    private string _pbmPath          = null!;
    private string _pgmPath          = null!;
    private string _ppmPath          = null!;
    private string _tgaPath          = null!;
    private string _tiffPath         = null!;
    private string _webpPath         = null!;
    private string _qoiPath          = null!;
    private string _axpPath          = null!;
    private string _axpSavePath      = null!;
    private PixelPaint.Models.Image _axpSaveSourceImage = null!;
    private (uint width, uint height) _axpSaveEditorSize;

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
        _palette4BitPath = Path.Combine(TestDataDir, "image_depth_4bit_palette.png");
        _palette8BitPath = Path.Combine(TestDataDir, "image_depth_8bit_palette.png");
        _gray8BitPath   = Path.Combine(TestDataDir, "image_depth_8bit_gray.png");
        _gray16BitPath  = Path.Combine(TestDataDir, "image_depth_16bit_gray.png");
        _mono1BitPath   = Path.Combine(TestDataDir, "image_depth_1bit.png");

        _gifPath = Path.Combine(TestDataDir, "image_format_gif.gif");
        _pbmPath = Path.Combine(TestDataDir, "image_format_pbm.pbm");
        _pgmPath = Path.Combine(TestDataDir, "image_format_pgm.pgm");
        _ppmPath = Path.Combine(TestDataDir, "image_format_ppm.ppm");
        _tgaPath = Path.Combine(TestDataDir, "image_format_tga.tga");
        _tiffPath = Path.Combine(TestDataDir, "image_format_tiff.tiff");
        _webpPath = Path.Combine(TestDataDir, "image_format_webp.webp");
        _qoiPath = Path.Combine(TestDataDir, "image_format_qoi.qoi");

        _axpPath = Path.Combine(AppContext.BaseDirectory, "benchmark_sample.axp");
        var axpSourceImage = _fileService.ImportImage(_rgba32PngPath);
        _fileService.SaveImage(axpSourceImage, _axpPath, ((uint)axpSourceImage.PixelCountX, (uint)axpSourceImage.PixelCountY));

        _axpSavePath = Path.Combine(AppContext.BaseDirectory, "benchmark_save.axp");
        _axpSaveSourceImage = axpSourceImage;
        _axpSaveEditorSize = ((uint)axpSourceImage.PixelCountX, (uint)axpSourceImage.PixelCountY);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_axpPath))
            File.Delete(_axpPath);

        if (File.Exists(_axpSavePath))
            File.Delete(_axpSavePath);
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
    public object ImportPalette4Bit() => _fileService.ImportImage(_palette4BitPath);

    [Benchmark(Description = "ImageSharp – 8-bit palette PNG")]
    public object ImportPalette8Bit() => _fileService.ImportImage(_palette8BitPath);

    [Benchmark(Description = "ImageSharp – 8-bit grayscale PNG")]
    public object ImportGray8Bit() => _fileService.ImportImage(_gray8BitPath);

    [Benchmark(Description = "ImageSharp – 16-bit grayscale PNG")]
    public object ImportGray16Bit() => _fileService.ImportImage(_gray16BitPath);

    [Benchmark(Description = "ImageSharp – 1-bit monochrome PNG")]
    public object ImportMono1Bit() => _fileService.ImportImage(_mono1BitPath);

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

    [Benchmark(Description = "AXP – Load")]
    public object LoadAxp() => _fileService.LoadImage(_axpPath);

    [Benchmark(Description = "AXP – Save")]
    public void SaveAxp() => _fileService.SaveImage(_axpSaveSourceImage, _axpSavePath, _axpSaveEditorSize);
}
