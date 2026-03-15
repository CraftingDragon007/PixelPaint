using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PixelPaint.Localization;
using PixelPaint.Models;

namespace PixelPaint.Services;

public interface IFileService
{
    IReadOnlyList<FilePickerFileType> FileTypeFilter { get; }

    IReadOnlyList<FilePickerFileType> ImportFileTypeFilter { get; }

    /// <summary>
    ///     Load an image from a file supported by PixelPaint
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <returns>The loaded image</returns>
    /// <exception cref="ArgumentException">Thrown when the file type is not supported</exception>
    (Image image, (uint width, uint height) editorSize) LoadImage(string path);

    /// <summary>
    ///     Save an image to a file supported by PixelPaint
    /// </summary>
    /// <param name="image">The image to save</param>
    /// <param name="path">The path to save the image to</param>
    /// <param name="editorSize">The size of the editor</param>
    void SaveImage(Image image, string path, (uint width, uint height) editorSize);

    /// <summary>
    ///     Import a raster image (JPEG, BMP, GIF, PBM, PGM, PPM, PNG, TGA, TIFF, WebP, QOI) as pixel art.
    ///     Each pixel of the source image becomes one art pixel in the result.
    /// </summary>
    /// <param name="path">Path to a supported raster image file</param>
    /// <returns>The imported image</returns>
    /// <exception cref="ArgumentException">Thrown when the file type is not supported</exception>
    Image ImportImage(string path);

    /// <summary>
    ///     Estimate SVG file size for the provided image and indicate whether a warning should be shown.
    /// </summary>
    /// <param name="image">Image to be saved as SVG</param>
    /// <returns>Estimated size in bytes and whether the estimate exceeds the warning threshold</returns>
    (long estimatedSizeBytes, bool shouldWarn) GetSvgSaveWarning(Image image);
}

public partial class FileService : IFileService
{
    private const string AxpExtension = "axp";
    private const string BxpExtension = "bxp";
    private const string PxpExtension = "pxp";
    private const string SvgExtension = "svg";
    private const string PngExtension = "png";
    private const string BmpExtension = "bmp";
    private const string JpgExtension = "jpg";
    private const string JpegExtension = "jpeg";
    private const string GifExtension = "gif";
    private const string PbmExtension = "pbm";
    private const string PgmExtension = "pgm";
    private const string PpmExtension = "ppm";
    private const string TgaExtension = "tga";
    private const string TifExtension = "tif";
    private const string TiffExtension = "tiff";
    private const string WebpExtension = "webp";
    private const string QoiExtension = "qoi";

    private const string UnsupportedFileTypeMessage = "Unsupported file type";
    private const string InvalidBxpFileMessage = "Not a valid .bxp file";
    private const string InvalidPxpFileMessage = "Not a valid .pxp file";

    private const int LegacyPanelHeight = 319;
    private const int LegacyPanelWidth = 653;
    private const int SvgPreviewScale = 20;
    private const long SvgLargeFileWarningThresholdBytes = 5L * 1024 * 1024;
    private const string SvgNamespace = "http://www.w3.org/2000/svg";
    private static readonly byte[] AxpMagicHeader = "PPAF"u8.ToArray();
    private static readonly byte[] BxpHeader = "PixelPaint"u8.ToArray();

    /// <summary>
    ///     Save an image to a file supported by PixelPaint
    /// </summary>
    /// <param name="image">The image to save</param>
    /// <param name="path">The path to save the image to</param>
    /// <param name="editorSize">The size of the editor</param>
    public void SaveImage(Image image, string path, (uint width, uint height) editorSize)
    {
        switch (GetFileExtension(path))
        {
            case PxpExtension:
                SaveImageToPxp(image, path);
                break;
            case BxpExtension:
                SaveImageToBxp(image, path);
                break;
            case AxpExtension:
                SaveImageToAxp(image, path, editorSize);
                break;
            case SvgExtension:
                SaveImageToSvg(image, path);
                break;
            default:
                throw new ArgumentException(UnsupportedFileTypeMessage);
        }
    }

    /// <summary>
    ///     Load an image from a file supported by PixelPaint
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <returns>The loaded image</returns>
    /// <exception cref="ArgumentException">Thrown when the file type is not supported</exception>
    public (Image image, (uint width, uint height) editorSize) LoadImage(string path)
    {
        return GetFileExtension(path) switch
        {
            PxpExtension => (LoadImageFromPxp(path), (LegacyPanelWidth, LegacyPanelHeight)),
            BxpExtension => (LoadImageFromBxp(path), (LegacyPanelWidth, LegacyPanelHeight)),
            AxpExtension => LoadImageFromAxp(path),
            SvgExtension => LoadImageFromSvg(path),
            _ => throw new ArgumentException(UnsupportedFileTypeMessage)
        };
    }

    /// <summary>
    ///     Import a raster image (JPEG, BMP, GIF, PBM, PGM, PPM, PNG, TGA, TIFF, WebP, QOI) as pixel art.
    ///     Each pixel of the source image becomes one art pixel in the result.
    /// </summary>
    /// <param name="path">Path to a supported raster image file</param>
    /// <returns>The imported image</returns>
    /// <exception cref="ArgumentException">Thrown when the file type is not supported</exception>
    public Image ImportImage(string path) =>
        GetFileExtension(path) switch
        {
            PngExtension or BmpExtension or JpgExtension or JpegExtension or GifExtension or PbmExtension or
                PgmExtension or PpmExtension or
                TgaExtension or TifExtension or TiffExtension or WebpExtension or QoiExtension =>
                ImportImageFromBitmap(path),
            _ => throw new ArgumentException(UnsupportedFileTypeMessage)
        };

    public IReadOnlyList<FilePickerFileType> ImportFileTypeFilter =>
    [
        new FilePickerFileType(LocalizationService.Instance["FileType_RasterImage"])
        {
            Patterns = ["*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.pbm", "*.pgm", "*.ppm", "*.png", "*.tga", "*.tif", "*.tiff", "*.webp", "*.qoi"],
            MimeTypes = ["image/jpeg", "image/bmp", "image/gif", "image/x-portable-bitmap", "image/x-portable-graymap", "image/x-portable-pixmap", "image/png", "image/x-tga", "image/tiff", "image/webp", "image/qoi"]
        }
    ];

    public IReadOnlyList<FilePickerFileType> FileTypeFilter =>
    [
        new(LocalizationService.Instance["FileType_AdvancedPixelPaint"])
        {
            Patterns = ["*.axp"], MimeTypes =
                ["application/octet-stream"],
            AppleUniformTypeIdentifiers = ["com.pixel-paint.axp"]
        },
        new(LocalizationService.Instance["FileType_SvgImage"])
        {
            Patterns = ["*.svg"], MimeTypes =
                ["image/svg+xml"],
            AppleUniformTypeIdentifiers = ["public.svg-image"]
        },
        new(LocalizationService.Instance["FileType_BetterPixelPaint"])
        {
            Patterns = ["*.bxp"], MimeTypes =
                ["application/octet-stream"],
            AppleUniformTypeIdentifiers = ["com.pixel-paint.bxp"]
        },
        new(LocalizationService.Instance["FileType_PixelPaint"])
        {
            Patterns = ["*.pxp"], MimeTypes =
                ["text/plain"],
            AppleUniformTypeIdentifiers = ["com.pixel-paint.pxp"]
        }
    ];

    private static string GetFileExtension(string path) =>
        Path.GetExtension(path).TrimStart('.').ToLowerInvariant();

    private static int GetLegacyPixelSize(Image image, bool allowTolerance)
    {
        var pixelSizeX = LegacyPanelWidth / image.PixelCountX;
        var pixelSizeY = LegacyPanelHeight / image.PixelCountY;

        if (allowTolerance)
        {
            if (Math.Abs(pixelSizeX - pixelSizeY) > 1)
                throw new ArgumentException("Pixel size is not quadratic, please use a different file type or image size");
        }
        else if (pixelSizeX != pixelSizeY)
        {
            throw new ArgumentException("Pixel size is not quadratic, please use a different file type or image size");
        }

        return pixelSizeX;
    }

    private static Image CreateImage(int pixelCountX, int pixelCountY)
    {
        return new Image
        {
            PixelCountX = pixelCountX,
            PixelCountY = pixelCountY,
            PixelCount = pixelCountX * pixelCountY,
            Pixels = new Color[pixelCountX, pixelCountY]
        };
    }

    private static void WriteUint(Stream stream, uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        stream.Write(bytes, 0, 4);
    }

    private static uint ReadUint(Stream stream)
    {
        var bytes = new byte[4];
        stream.ReadExactly(bytes);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes);
    }
}