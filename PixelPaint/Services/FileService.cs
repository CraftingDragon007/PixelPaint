using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    (Image image, (uint width, uint height) editorSize) LoadImage(string path);

    /// <summary>
    ///     Save an image to a file supported by PixelPaint
    /// </summary>
    /// <param name="image">The image to save</param>
    /// <param name="path">The path to save the image to</param>
    /// <param name="editorSize">The size of the editor</param>
    void SaveImage(Image image, string path, (uint width, uint height) editorSize);
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
    /// <param name="editorSize">The size of the editor</param>
    public void SaveImage(Image image, string path, (uint width, uint height) editorSize)
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
                SaveImageToAxp(image, path, editorSize);
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
    public (Image image, (uint width, uint height) editorSize) LoadImage(string path)
    {
        return path.Split('.').Last() switch
        {
            "pxp" => (LoadImageFromPxp(path), (LegacyPanelWidth, LegacyPanelHeight)),
            "bxp" => (LoadImageFromBxp(path), (LegacyPanelWidth, LegacyPanelHeight)),
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

    private static void SaveImageToAxp(Image image, string path, (uint width, uint height) editorSize)
    {
        var colors = image.Pixels.Cast<Color>().Distinct().ToArray();
        if (colors.Length == 0)
            throw new ArgumentException("Image has no colors");

        using var fileStream = File.Open(path, FileMode.Create, FileAccess.Write);
        fileStream.Position = 0;
        var magic = "PPAF"u8;
        fileStream.Write(magic); // Magic
        fileStream.WriteByte(1); // Version

        WriteUint((uint)image.PixelCountX);
        WriteUint((uint)image.PixelCountY);
        WriteUint(editorSize.width);
        WriteUint(editorSize.height);
        WriteUint((uint)colors.Length);
        
        foreach (var color in colors)
        {
            fileStream.WriteByte(color.A);
            fileStream.WriteByte(color.R);
            fileStream.WriteByte(color.G);
            fileStream.WriteByte(color.B);
        }

        var bitsPerPixel = (uint)Math.Ceiling(Math.Log2(colors.Length));
        var index = colors.Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i);

        byte currentByte = 0;
        var bitsWritten = 0;

        foreach (var color in image.Pixels)
        {
            var colorIndex = index[color];
            for (var i = 0; i < bitsPerPixel; i++)
            {
                currentByte |= (byte)(((colorIndex >> i) & 1) << bitsWritten);
                if (++bitsWritten != 8) continue;
                fileStream.WriteByte(currentByte);
                currentByte = 0;
                bitsWritten = 0;
            }
        }

        if (bitsWritten > 0) fileStream.WriteByte(currentByte);

        return;

        void WriteUint(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
            fileStream.Write(bytes, 0, 4);
        }
    }

    private static void SaveImageToBxp(Image image, string path)
    {
        var pixelSizeX = LegacyPanelWidth / image.PixelCountX;
        var pixelSizeY = LegacyPanelHeight / image.PixelCountY;

        // tolerance of 1 pixel
        if (Math.Abs(pixelSizeX - pixelSizeY) > 1)
            throw new ArgumentException("Pixel size is not quadratic, please use a different file type or image size");

        using var fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
        fileStream.Position = 0;
        fileStream.Write("PixelPaint"u8);
        var pixelSize = BitConverter.GetBytes(pixelSizeX);
        fileStream.WriteByte((byte)pixelSize.Length);
        fileStream.Write(pixelSize);
        fileStream.WriteByte(77);
        var fileSize = image.PixelCount * 3;
        var fileSizeBytes = BitConverter.GetBytes(fileSize);
        fileStream.WriteByte((byte)fileSizeBytes.Length);
        fileStream.WriteByte(77);
        fileStream.Write(fileSizeBytes);
        fileStream.WriteByte(77);

        for (var y = 0; y < image.PixelCountY; y++)
        {
            for (var x = 0; x < image.PixelCountX; x++)
            {
                var pixel = image.Pixels[x, y];
                fileStream.WriteByte(pixel.R);
                fileStream.WriteByte(pixel.G);
                fileStream.WriteByte(pixel.B);
            }
        }
    }

    private static void SaveImageToPxp(Image image, string path)
    {
        var pixelSizeX = LegacyPanelWidth / image.PixelCountX;
        var pixelSizeY = LegacyPanelHeight / image.PixelCountY;

        if (pixelSizeX != pixelSizeY)
            throw new ArgumentException("Pixel size is not quadratic, please use a different file type or image size");

        var content = $"Size={pixelSizeX}\n";
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
    private static (Image image, (uint width, uint height) editorSize) LoadImageFromAxp(string path)
    {
        using var fileStream = File.OpenRead(path);
    
        // Verify magic number
        var magic = new byte[4];
        fileStream.ReadExactly(magic);
        if (!magic.SequenceEqual("PPAF"u8.ToArray()))
            throw new InvalidDataException("Invalid file format");

        // Verify version
        var version = fileStream.ReadByte();
        if (version != 1)
            throw new NotSupportedException("Unsupported version");

        // Read dimensions
        var imageWidth = ReadUint(fileStream);
        var imageHeight = ReadUint(fileStream);
        var editorWidth = ReadUint(fileStream);
        var editorHeight = ReadUint(fileStream);

        // Read color palette
        var colorCount = ReadUint(fileStream);
        var colors = new Color[colorCount];
        for (int i = 0; i < colorCount; i++)
        {
            colors[i] = Color.FromArgb(
                (byte)fileStream.ReadByte(),
                (byte)fileStream.ReadByte(),
                (byte)fileStream.ReadByte(),
                (byte)fileStream.ReadByte()
            );
        }

        // Calculate bits per pixel
        var bitsPerPixel = colors.Length > 1 ? 
            (int)Math.Log2(colors.Length - 1) + 1 : 0;

        // Read pixel data
        var pixels = new Color[imageWidth * imageHeight];
        switch (colors.Length)
        {
            case > 1:
            {
                byte currentByte = 0;
                var bitsAvailable = 0;
                var pixelIndex = 0;

                while (pixelIndex < pixels.Length)
                {
                    if (bitsAvailable == 0)
                    {
                        currentByte = (byte)fileStream.ReadByte();
                        bitsAvailable = 8;
                    }

                    var colorIndex = 0;
                    for (var i = 0; i < bitsPerPixel && bitsAvailable > 0; i++)
                    {
                        colorIndex |= ((currentByte & 1) << i);
                        currentByte >>= 1;
                        bitsAvailable--;
                    }

                    pixels[pixelIndex++] = colors[colorIndex];
                }

                break;
            }
            case 1:
            {
                // If only one color, fill the entire image with it
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = colors[0];
                break;
            }
        }

        var image = new Image
        {
            PixelCountX = (int)imageWidth,
            PixelCountY = (int)imageHeight,
            Pixels = new Color[imageWidth, imageHeight],
            PixelCount = (int)(imageWidth * imageHeight)
        };
        
        for (var y = 0; y < imageHeight; y++)
        for (var x = 0; x < imageWidth; x++)
            image.Pixels[x, y] = pixels[y * imageWidth + x];
        
        return (image, (editorWidth, editorHeight));
        
        uint ReadUint(Stream stream)
        {
            var bytes = new byte[4];
            stream.ReadExactly(bytes);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes);
        }
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
            Pixels = new Color[pixelCountX, pixelCountY],
            PixelCount = pixelCount
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