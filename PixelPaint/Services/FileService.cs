using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PixelPaint.Models;

namespace PixelPaint.Services;

public interface IFileService
{
    IReadOnlyList<FilePickerFileType> FileTypeFilter { get; }

    /// <summary>
    ///     Load an image from a file supported by PixelPaint
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <returns>The loaded image</returns>
    /// <exception cref="ArgumentException">Thrown when the file type is not supported</exception>
    Image LoadImage(string path);

    /// <summary>
    ///     Save an image to a file supported by PixelPaint
    /// </summary>
    /// <param name="image">The image to save</param>
    /// <param name="path">The path to save the image to</param>
    void SaveImage(Image image, string path);
}

public class FileService : IFileService
{
    private const int LegacyPanelHeight = 319;
    private const int LegacyPanelWidth = 653;

    /// <summary>
    ///     Save an image to a file supported by PixelPaint
    /// </summary>
    /// <param name="image">The image to save</param>
    /// <param name="path">The path to save the image to</param>
    public void SaveImage(Image image, string path)
    {
        switch (path.Split('.').Last())
        {
            case "pxp":
                SaveImageToPxp(image, path);
                break;
            case "bxp":
                SaveImageToBxp(image, path);
                break;
            case "axp":
                SaveImageToAxp(image, path);
                break;
            default:
                throw new ArgumentException("Unsupported file type");
        }
    }

    /// <summary>
    ///     Load an image from a file supported by PixelPaint
    /// </summary>
    /// <param name="path">The path to the file</param>
    /// <returns>The loaded image</returns>
    /// <exception cref="ArgumentException">Thrown when the file type is not supported</exception>
    public Image LoadImage(string path)
    {
        return path.Split('.').Last() switch
        {
            "pxp" => LoadImageFromPxp(path),
            "bxp" => LoadImageFromBxp(path),
            "axp" => LoadImageFromAxp(path),
            _ => throw new ArgumentException("Unsupported file type")
        };
    }

    public IReadOnlyList<FilePickerFileType> FileTypeFilter =>
    [
        new("Advanced Pixel Paint File")
        {
            Patterns = ["*.axp"], MimeTypes =
                ["application/octet-stream"],
            AppleUniformTypeIdentifiers = ["com.pixel-paint.axp"]
        },
        new("Better Pixel Paint File")
        {
            Patterns = ["*.bxp"], MimeTypes =
                ["application/octet-stream"],
            AppleUniformTypeIdentifiers = ["com.pixel-paint.bxp"]
        },
        new("Pixel Paint File")
        {
            Patterns = ["*.pxp"], MimeTypes =
                ["text/plain"],
            AppleUniformTypeIdentifiers = ["com.pixel-paint.pxp"]
        }
    ];

    private static void SaveImageToAxp(Image image, string path)
    {
        throw new NotImplementedException();
    }

    private static void SaveImageToBxp(Image image, string path)
    {
        throw new NotImplementedException();
    }

    private static void SaveImageToPxp(Image image, string path)
    {
        var pixelSize = LegacyPanelWidth / image.PixelCountX;
        var content = $"Size={pixelSize}\n";
        for (var y = 0; y < image.PixelCountY; y++)
        for (var x = 0; x < image.PixelCountX; x++)
        {
            var pixel = image.Pixels[x, y];
            content += $"{pixel.R}|{pixel.G}|{pixel.B}\n";
        }

        File.WriteAllText(path, content);
    }

    /// <summary>
    ///     Load an image from a .axp file (Advanced Pixel Paint File)
    /// </summary>
    /// <param name="path">The path to the axp file</param>
    /// <returns>The loaded image</returns>
    private static Image LoadImageFromAxp(string path)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Load an image from a .bxp file (Better Pixel Paint File)
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static Image LoadImageFromBxp(string path)
    {
        var stream = File.OpenRead(path);
        var header = "PixelPaint"u8.ToArray();
        var headerBuffer = new byte[header.Length];
        stream.ReadExactly(headerBuffer, 0, header.Length);
        if (!header.SequenceEqual(headerBuffer))
            throw new ArgumentException("Not a valid .bxp file");
        var pixelSizeLength = stream.ReadByte();
        var pixelSizeBuffer = new byte[pixelSizeLength];
        stream.ReadExactly(pixelSizeBuffer, 0, pixelSizeLength);
        var pixelSize = BitConverter.ToInt32(pixelSizeBuffer, 0);
        stream.ReadByte();
        var fileSizeLength = stream.ReadByte();
        stream.ReadByte();
        var fileSizeBuffer = new byte[fileSizeLength];
        stream.ReadExactly(fileSizeBuffer, 0, fileSizeLength);
        var fileSize = BitConverter.ToInt32(fileSizeBuffer, 0);
        stream.ReadByte();
        var pixelCount = fileSize / 3;
        if (pixelCount <= 0)
            throw new ArgumentException("Not a valid .bxp file");
        var pixelCountX = LegacyPanelWidth / pixelSize;
        var pixelCountY = LegacyPanelHeight / pixelSize;
        var image = new Image
        {
            PixelCountX = pixelCountX,
            PixelCountY = pixelCountY,
            Pixels = new Color[pixelCountX, pixelCountY]
        };
        for (var i = 0; i < pixelCount; i++)
        {
            var r = (byte)stream.ReadByte();
            var g = (byte)stream.ReadByte();
            var b = (byte)stream.ReadByte();
            var x = i % pixelCountX;
            var y = i / pixelCountX;
            image.Pixels[x, y] = new Color(255, r, g, b);
        }

        return image;
    }

    /// <summary>
    ///     Load an image from a .pxp file (Pixel Paint File)
    ///     Structure of a .pxp file:
    ///     Size=<c>pixelSize</c> <br />
    ///     <c>R</c>|<c>G</c>|<c>B</c> <br />
    ///     <c>R</c>|<c>G</c>|<c>B</c> <br />
    ///     ... <br />
    ///     The pixels are separated by a newline character
    ///     The image width and height are determined by the pixel count which fits in <c>653x319</c>. Each pixel is quadratic
    ///     and has the size of <c>pixelSize</c> screen pixels
    /// </summary>
    /// <param name="path">The path to the .pxp file</param>
    /// <returns>The loaded image</returns>
    private static Image LoadImageFromPxp(string path)
    {
        var content = File.ReadAllText(path);
        var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var size = lines[0].Split('=')[1].Split('x').Select(int.Parse).FirstOrDefault();
        if (size == 0)
            throw new ArgumentException("Not a valid .pxp file");

        var x = 0;
        var y = 0;

        var pixelCountX = LegacyPanelWidth / size;
        var pixelCountY = LegacyPanelHeight / size;

        var image = new Image
        {
            PixelCountX = pixelCountX,
            PixelCountY = pixelCountY,
            PixelCount = pixelCountX * pixelCountY,
            Pixels = new Color[pixelCountX, pixelCountY]
        };

        var i = 1;
        while (y + size <= LegacyPanelHeight)
        {
            while (x + size <= LegacyPanelWidth)
            {
                var color = lines[i].Split('|');
                var r = (byte)int.Parse(color[0]);
                var g = (byte)int.Parse(color[1]);
                var b = (byte)int.Parse(color[2]);
                image.Pixels[x / size, y / size] = new Color(255, r, g, b);
                x += size;
                i++;
            }

            x = 0;
            y += size;
        }

        return image;
    }
}