using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using PixelPaint.Models;

namespace PixelPaint.Services;

public partial class FileService
{
    private static void SaveImageToAxp(Image image, string path, (uint width, uint height) editorSize)
    {
        var colors = image.Pixels.Cast<Color>().Distinct().ToArray();
        if (colors.Length == 0)
            throw new ArgumentException("Image has no colors");

        using var fileStream = File.Open(path, FileMode.Create, FileAccess.Write);
        fileStream.Position = 0;
        fileStream.Write(AxpMagicHeader);
        fileStream.WriteByte(1);

        WriteUint(fileStream, (uint)image.PixelCountX);
        WriteUint(fileStream, (uint)image.PixelCountY);
        WriteUint(fileStream, editorSize.width);
        WriteUint(fileStream, editorSize.height);
        WriteUint(fileStream, (uint)colors.Length);

        WriteAxpPalette(fileStream, colors);
        WriteAxpPixelData(fileStream, image, colors);
    }

    /// <summary>
    ///     Load an image from a .axp file (Advanced Pixel Paint File)
    /// </summary>
    /// <param name="path">The path to the axp file</param>
    /// <returns>The loaded image</returns>
    private static (Image image, (uint width, uint height) editorSize) LoadImageFromAxp(string path)
    {
        using var fileStream = File.OpenRead(path);

        VerifyAxpMagicHeader(fileStream);

        var version = fileStream.ReadByte();
        if (version != 1)
            throw new NotSupportedException("Unsupported version");

        var imageWidth = ReadUint(fileStream);
        var imageHeight = ReadUint(fileStream);
        var editorWidth = ReadUint(fileStream);
        var editorHeight = ReadUint(fileStream);

        var colorCount = ReadUint(fileStream);
        var colors = ReadAxpPalette(fileStream, colorCount);
        var pixels = ReadAxpPixels(fileStream, imageWidth, imageHeight, colors);
        var image = CreateAxpImage(imageWidth, imageHeight, pixels);

        return (image, (editorWidth, editorHeight));
    }

    private static void VerifyAxpMagicHeader(Stream stream)
    {
        var magic = new byte[AxpMagicHeader.Length];
        stream.ReadExactly(magic);
        if (!magic.SequenceEqual(AxpMagicHeader))
            throw new InvalidDataException("Invalid file format");
    }

    private static void WriteAxpPalette(Stream stream, IReadOnlyList<Color> colors)
    {
        foreach (var color in colors)
        {
            stream.WriteByte(color.A);
            stream.WriteByte(color.R);
            stream.WriteByte(color.G);
            stream.WriteByte(color.B);
        }
    }

    private static void WriteAxpPixelData(Stream stream, Image image, IReadOnlyList<Color> colors)
    {
        var bitsPerPixel = (uint)Math.Ceiling(Math.Log2(Math.Max(1, colors.Count)));
        if (colors.Count <= 1)
            return;

        var index = colors.Select((color, i) => (color, i)).ToDictionary(x => x.color, x => x.i);

        byte currentByte = 0;
        var bitsWritten = 0;

        foreach (var pixelColor in image.Pixels)
        {
            var colorIndex = index[pixelColor];
            for (var i = 0; i < bitsPerPixel; i++)
            {
                currentByte |= (byte)(((colorIndex >> i) & 1) << bitsWritten);
                if (++bitsWritten != 8)
                    continue;

                stream.WriteByte(currentByte);
                currentByte = 0;
                bitsWritten = 0;
            }
        }

        if (bitsWritten > 0)
            stream.WriteByte(currentByte);
    }

    private static Color[] ReadAxpPalette(Stream stream, uint colorCount)
    {
        var colors = new Color[colorCount];
        for (var i = 0; i < colorCount; i++)
        {
            colors[i] = Color.FromArgb(
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte(),
                (byte)stream.ReadByte());
        }

        return colors;
    }

    private static Color[] ReadAxpPixels(Stream stream, uint imageWidth, uint imageHeight, IReadOnlyList<Color> colors)
    {
        var bitsPerPixel = colors.Count > 1 ? (int)Math.Ceiling(Math.Log2(colors.Count)) : 0;
        var pixels = new Color[imageWidth * imageHeight];

        if (colors.Count == 0)
        {
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Colors.Transparent;
            return pixels;
        }

        if (colors.Count == 1)
        {
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = colors[0];
            return pixels;
        }

        byte currentByte = 0;
        var bitsAvailable = 0;
        var pixelIndex = 0;

        while (pixelIndex < pixels.Length)
        {
            if (bitsAvailable < bitsPerPixel)
            {
                var nextByte = stream.ReadByte();
                if (nextByte == -1)
                    throw new EndOfStreamException("Unexpected end of pixel data stream.");

                currentByte |= (byte)(nextByte << bitsAvailable);
                bitsAvailable += 8;
            }

            var colorIndex = 0;
            for (var i = 0; i < bitsPerPixel; i++)
            {
                colorIndex |= (currentByte & 1) << i;
                currentByte >>= 1;
            }

            bitsAvailable -= bitsPerPixel;

            if (colorIndex >= colors.Count)
                throw new InvalidDataException($"Color index {colorIndex} out of bounds for palette size {colors.Count}");

            pixels[pixelIndex++] = colors[colorIndex];
        }

        return pixels;
    }

    private static Image CreateAxpImage(uint imageWidth, uint imageHeight, IReadOnlyList<Color> pixels)
    {
        var image = new Image
        {
            PixelCountX = (int)imageWidth,
            PixelCountY = (int)imageHeight,
            Pixels = new Color[imageWidth, imageHeight],
            PixelCount = (int)(imageWidth * imageHeight)
        };

        for (var y = 0; y < imageHeight; y++)
        for (var x = 0; x < imageWidth; x++)
            image.Pixels[x, y] = pixels[(int)(y * imageWidth + x)];

        return image;
    }
}

