using System;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Media;
using PixelPaint.Models;

namespace PixelPaint.Services;

public partial class FileService
{
    private static void SaveImageToBxp(Image image, string path)
    {
        var pixelSize = GetLegacyPixelSize(image, allowTolerance: true);

        using var fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
        fileStream.Position = 0;
        fileStream.Write(BxpHeader);

        var pixelSizeBytes = BitConverter.GetBytes(pixelSize);
        fileStream.WriteByte((byte)pixelSizeBytes.Length);
        fileStream.Write(pixelSizeBytes);
        fileStream.WriteByte(77);

        var fileSize = image.PixelCount * 3;
        var fileSizeBytes = BitConverter.GetBytes(fileSize);
        fileStream.WriteByte((byte)fileSizeBytes.Length);
        fileStream.WriteByte(77);
        fileStream.Write(fileSizeBytes);
        fileStream.WriteByte(77);

        WriteLegacyPixelData(fileStream, image);
    }

    private static void SaveImageToPxp(Image image, string path)
    {
        var pixelSize = GetLegacyPixelSize(image, allowTolerance: false);
        var content = new StringBuilder($"Size={pixelSize}\n");

        for (var y = 0; y < image.PixelCountY; y++)
        for (var x = 0; x < image.PixelCountX; x++)
        {
            var pixel = image.Pixels[x, y];
            content.Append($"{pixel.R}|{pixel.G}|{pixel.B}\n");
        }

        File.WriteAllText(path, content.ToString());
    }

    /// <summary>
    ///     Load an image from a .bxp file (Better Pixel Paint File)
    /// </summary>
    private static Image LoadImageFromBxp(string path)
    {
        using var stream = File.OpenRead(path);

        var headerBuffer = new byte[BxpHeader.Length];
        stream.ReadExactly(headerBuffer, 0, headerBuffer.Length);
        if (!headerBuffer.SequenceEqual(BxpHeader))
            throw new ArgumentException(InvalidBxpFileMessage);

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
            throw new ArgumentException(InvalidBxpFileMessage);

        var pixelCountX = LegacyPanelWidth / pixelSize;
        var pixelCountY = LegacyPanelHeight / pixelSize;
        var image = CreateImage(pixelCountX, pixelCountY);
        image.PixelCount = pixelCount;

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
    private static Image LoadImageFromPxp(string path)
    {
        var content = File.ReadAllText(path);
        var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var size = lines[0].Split('=')[1].Split('x').Select(int.Parse).FirstOrDefault();
        if (size == 0)
            throw new ArgumentException(InvalidPxpFileMessage);

        var x = 0;
        var y = 0;
        var pixelCountX = LegacyPanelWidth / size;
        var pixelCountY = LegacyPanelHeight / size;
        var image = CreateImage(pixelCountX, pixelCountY);

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

    private static void WriteLegacyPixelData(Stream stream, Image image)
    {
        for (var y = 0; y < image.PixelCountY; y++)
        {
            for (var x = 0; x < image.PixelCountX; x++)
            {
                var pixel = image.Pixels[x, y];
                stream.WriteByte(pixel.R);
                stream.WriteByte(pixel.G);
                stream.WriteByte(pixel.B);
            }
        }
    }
}

