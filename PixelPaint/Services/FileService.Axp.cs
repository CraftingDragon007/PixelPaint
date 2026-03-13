using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media;
using PixelPaint.Models;

namespace PixelPaint.Services;

public partial class FileService
{
    private const byte AxpVersion1 = 1;
    private const byte AxpVersion2 = 2;

    private enum AxpEncoding : byte
    {
        SolidColor = 1,
        PaletteBitPacked = 2,
        PaletteRle = 3,
        RawArgb32 = 4
    }

    private sealed record AxpPayload(AxpEncoding Encoding, Color[] Palette, byte[] Data);

    private static void SaveImageToAxp(Image image, string path, (uint width, uint height) editorSize)
    {
        var pixels = FlattenPixels(image);
        if (pixels.Length == 0)
            throw new ArgumentException("Image has no pixels");

        var payload = BuildBestAxpPayload(pixels);

        using var fileStream = File.Open(path, FileMode.Create, FileAccess.Write);
        fileStream.Position = 0;
        fileStream.Write(AxpMagicHeader);
        fileStream.WriteByte(AxpVersion2);
        fileStream.WriteByte((byte)payload.Encoding);
        WriteUint(fileStream, (uint)image.PixelCountX);
        WriteUint(fileStream, (uint)image.PixelCountY);
        WriteUint(fileStream, editorSize.width);
        WriteUint(fileStream, editorSize.height);

        switch (payload.Encoding)
        {
            case AxpEncoding.SolidColor:
                WriteColor(fileStream, payload.Palette[0]);
                break;
            case AxpEncoding.PaletteBitPacked:
            case AxpEncoding.PaletteRle:
                WriteUint(fileStream, (uint)payload.Palette.Length);
                WritePalette(fileStream, payload.Palette);
                WriteUint(fileStream, (uint)payload.Data.Length);
                fileStream.Write(payload.Data, 0, payload.Data.Length);
                break;
            case AxpEncoding.RawArgb32:
                WriteUint(fileStream, (uint)payload.Data.Length);
                fileStream.Write(payload.Data, 0, payload.Data.Length);
                break;
            default:
                throw new InvalidDataException("Unsupported AXP encoding");
        }
    }

    /// <summary>
    ///     Load an image from a .axp file (Advanced Pixel Paint File)
    /// </summary>
    private static (Image image, (uint width, uint height) editorSize) LoadImageFromAxp(string path)
    {
        using var fileStream = File.OpenRead(path);
        VerifyAxpMagicHeader(fileStream);

        var version = fileStream.ReadByte();
        return version switch
        {
            AxpVersion1 => LoadLegacyAxpVersion1(fileStream),
            AxpVersion2 => LoadAxpVersion2(fileStream),
            _ => throw new NotSupportedException("Unsupported version")
        };
    }

    private static (Image image, (uint width, uint height) editorSize) LoadAxpVersion2(Stream stream)
    {
        var encoding = (AxpEncoding)ReadRequiredByte(stream);
        var imageWidth = ReadUint(stream);
        var imageHeight = ReadUint(stream);
        var editorWidth = ReadUint(stream);
        var editorHeight = ReadUint(stream);
        ValidateImageDimensions(imageWidth, imageHeight);

        var pixelCount = checked((int)(imageWidth * imageHeight));
        var pixels = encoding switch
        {
            AxpEncoding.SolidColor => ReadSolidColorPixels(stream, pixelCount),
            AxpEncoding.PaletteBitPacked => ReadPaletteBitPackedPixels(stream, pixelCount),
            AxpEncoding.PaletteRle => ReadPaletteRlePixels(stream, pixelCount),
            AxpEncoding.RawArgb32 => ReadRawArgbPixels(stream, pixelCount),
            _ => throw new InvalidDataException("Unsupported AXP encoding")
        };

        var image = CreateImageFromFlatPixels((int)imageWidth, (int)imageHeight, pixels);
        return (image, (editorWidth, editorHeight));
    }

    private static (Image image, (uint width, uint height) editorSize) LoadLegacyAxpVersion1(Stream stream)
    {
        var imageWidth = ReadUint(stream);
        var imageHeight = ReadUint(stream);
        var editorWidth = ReadUint(stream);
        var editorHeight = ReadUint(stream);
        ValidateImageDimensions(imageWidth, imageHeight);

        var colorCount = ReadUint(stream);
        var colors = ReadLegacyAxpPalette(stream, colorCount);
        var pixels = ReadLegacyAxpPixels(stream, imageWidth, imageHeight, colors);
        var image = CreateLegacyAxpImage(imageWidth, imageHeight, pixels);

        return (image, (editorWidth, editorHeight));
    }

    private static void VerifyAxpMagicHeader(Stream stream)
    {
        var magic = new byte[AxpMagicHeader.Length];
        stream.ReadExactly(magic);
        if (!magic.SequenceEqual(AxpMagicHeader))
            throw new InvalidDataException("Invalid file format");
    }

    private static AxpPayload BuildBestAxpPayload(Color[] pixels)
    {
        var solidPayload = TryBuildSolidColorPayload(pixels);
        if (solidPayload is not null)
            return solidPayload;

        var palette = BuildPalette(pixels, out var indices);
        var candidates = new List<AxpPayload>
        {
            BuildRawArgbPayload(pixels)
        };

        if (palette.Length > 0)
        {
            candidates.Add(BuildPaletteBitPackedPayload(palette, indices));
            candidates.Add(BuildPaletteRlePayload(palette, indices));
        }

        return candidates.OrderBy(candidate => GetTotalAxpPayloadSize(candidate)).First();
    }

    private static AxpPayload? TryBuildSolidColorPayload(IReadOnlyList<Color> pixels)
    {
        var first = pixels[0];
        for (var i = 1; i < pixels.Count; i++)
        {
            if (pixels[i] != first)
                return null;
        }

        return new AxpPayload(AxpEncoding.SolidColor, [first], []);
    }

    private static Color[] BuildPalette(IReadOnlyList<Color> pixels, out int[] indices)
    {
        var palette = new List<Color>();
        var indexByColor = new Dictionary<Color, int>();
        indices = new int[pixels.Count];

        for (var i = 0; i < pixels.Count; i++)
        {
            var color = pixels[i];
            if (!indexByColor.TryGetValue(color, out var index))
            {
                index = palette.Count;
                palette.Add(color);
                indexByColor[color] = index;
            }

            indices[i] = index;
        }

        return [.. palette];
    }

    private static AxpPayload BuildPaletteBitPackedPayload(Color[] palette, IReadOnlyList<int> indices)
    {
        var bitsPerPixel = GetBitsPerPixel(palette.Length);
        using var payloadStream = new MemoryStream();

        byte currentByte = 0;
        var bitsWritten = 0;
        foreach (var colorIndex in indices)
        {
            for (var bit = 0; bit < bitsPerPixel; bit++)
            {
                currentByte |= (byte)(((colorIndex >> bit) & 1) << bitsWritten);
                if (++bitsWritten != 8)
                    continue;

                payloadStream.WriteByte(currentByte);
                currentByte = 0;
                bitsWritten = 0;
            }
        }

        if (bitsWritten > 0)
            payloadStream.WriteByte(currentByte);

        return new AxpPayload(AxpEncoding.PaletteBitPacked, palette, payloadStream.ToArray());
    }

    private static AxpPayload BuildPaletteRlePayload(Color[] palette, IReadOnlyList<int> indices)
    {
        using var payloadStream = new MemoryStream();
        var currentIndex = indices[0];
        var runLength = 1;

        for (var i = 1; i < indices.Count; i++)
        {
            if (indices[i] == currentIndex)
            {
                runLength++;
                continue;
            }

            Write7BitEncodedInt(payloadStream, runLength);
            Write7BitEncodedInt(payloadStream, currentIndex);
            currentIndex = indices[i];
            runLength = 1;
        }

        Write7BitEncodedInt(payloadStream, runLength);
        Write7BitEncodedInt(payloadStream, currentIndex);

        return new AxpPayload(AxpEncoding.PaletteRle, palette, payloadStream.ToArray());
    }

    private static AxpPayload BuildRawArgbPayload(IReadOnlyList<Color> pixels)
    {
        using var payloadStream = new MemoryStream(pixels.Count * 4);
        foreach (var color in pixels)
            WriteColor(payloadStream, color);

        return new AxpPayload(AxpEncoding.RawArgb32, [], payloadStream.ToArray());
    }

    private static int GetTotalAxpPayloadSize(AxpPayload payload)
    {
        const int fixedHeaderSize = 4 + 1 + 1 + 4 + 4 + 4 + 4;

        return payload.Encoding switch
        {
            AxpEncoding.SolidColor => fixedHeaderSize + 4,
            AxpEncoding.PaletteBitPacked or AxpEncoding.PaletteRle =>
                fixedHeaderSize + 4 + payload.Palette.Length * 4 + 4 + payload.Data.Length,
            AxpEncoding.RawArgb32 => fixedHeaderSize + 4 + payload.Data.Length,
            _ => int.MaxValue
        };
    }

    private static Color[] FlattenPixels(Image image)
    {
        var pixels = new Color[image.PixelCount];
        var index = 0;
        for (var y = 0; y < image.PixelCountY; y++)
        for (var x = 0; x < image.PixelCountX; x++)
            pixels[index++] = image.Pixels[x, y];

        return pixels;
    }

    private static void WritePalette(Stream stream, IReadOnlyList<Color> colors)
    {
        foreach (var color in colors)
            WriteColor(stream, color);
    }

    private static void WriteColor(Stream stream, Color color)
    {
        stream.WriteByte(color.A);
        stream.WriteByte(color.R);
        stream.WriteByte(color.G);
        stream.WriteByte(color.B);
    }

    private static Color ReadColor(Stream stream)
    {
        return Color.FromArgb(
            ReadRequiredByte(stream),
            ReadRequiredByte(stream),
            ReadRequiredByte(stream),
            ReadRequiredByte(stream));
    }

    private static void ValidateImageDimensions(uint width, uint height)
    {
        if (width == 0 || height == 0)
            throw new InvalidDataException("Invalid image dimensions");

        _ = checked((int)(width * height));
    }

    private static byte ReadRequiredByte(Stream stream)
    {
        var value = stream.ReadByte();
        if (value < 0)
            throw new EndOfStreamException("Unexpected end of file.");
        return (byte)value;
    }

    private static int GetBitsPerPixel(int colorCount)
    {
        if (colorCount <= 1)
            return 1;
        return (int)Math.Ceiling(Math.Log2(colorCount));
    }

    private static Color[] ReadSolidColorPixels(Stream stream, int pixelCount)
    {
        var color = ReadColor(stream);
        var pixels = new Color[pixelCount];
        Array.Fill(pixels, color);
        return pixels;
    }

    private static Color[] ReadPaletteBitPackedPixels(Stream stream, int pixelCount)
    {
        var palette = ReadPaletteWithLength(stream);
        var payload = ReadPayload(stream);
        var bitsPerPixel = GetBitsPerPixel(palette.Length);
        var pixels = new Color[pixelCount];

        using var payloadStream = new MemoryStream(payload, writable: false);
        byte currentByte = 0;
        var bitsAvailable = 0;

        for (var pixelIndex = 0; pixelIndex < pixelCount; pixelIndex++)
        {
            while (bitsAvailable < bitsPerPixel)
            {
                var nextByte = payloadStream.ReadByte();
                if (nextByte < 0)
                    throw new EndOfStreamException("Unexpected end of AXP bit-packed payload.");

                currentByte |= (byte)(nextByte << bitsAvailable);
                bitsAvailable += 8;
            }

            var colorIndex = 0;
            for (var bit = 0; bit < bitsPerPixel; bit++)
            {
                colorIndex |= (currentByte & 1) << bit;
                currentByte >>= 1;
            }

            bitsAvailable -= bitsPerPixel;
            if (colorIndex < 0 || colorIndex >= palette.Length)
                throw new InvalidDataException($"Color index {colorIndex} out of bounds for palette size {palette.Length}");

            pixels[pixelIndex] = palette[colorIndex];
        }

        if (payloadStream.Position != payloadStream.Length && pixelCount == 0)
            throw new InvalidDataException("Invalid AXP payload.");

        return pixels;
    }

    private static Color[] ReadPaletteRlePixels(Stream stream, int pixelCount)
    {
        var palette = ReadPaletteWithLength(stream);
        var payload = ReadPayload(stream);
        var pixels = new Color[pixelCount];
        using var payloadStream = new MemoryStream(payload, writable: false);

        var pixelIndex = 0;
        while (payloadStream.Position < payloadStream.Length && pixelIndex < pixelCount)
        {
            var runLength = Read7BitEncodedInt(payloadStream);
            var colorIndex = Read7BitEncodedInt(payloadStream);
            if (runLength <= 0)
                throw new InvalidDataException("Invalid AXP RLE run length.");
            if (colorIndex < 0 || colorIndex >= palette.Length)
                throw new InvalidDataException($"Color index {colorIndex} out of bounds for palette size {palette.Length}");
            if (pixelIndex + runLength > pixelCount)
                throw new InvalidDataException("AXP RLE payload overruns the image size.");

            for (var i = 0; i < runLength; i++)
                pixels[pixelIndex++] = palette[colorIndex];
        }

        if (pixelIndex != pixelCount || payloadStream.Position != payloadStream.Length)
            throw new InvalidDataException("AXP RLE payload does not exactly fill the image.");

        return pixels;
    }

    private static Color[] ReadRawArgbPixels(Stream stream, int pixelCount)
    {
        var payload = ReadPayload(stream);
        if (payload.Length != pixelCount * 4)
            throw new InvalidDataException("Invalid raw AXP payload length.");

        var pixels = new Color[pixelCount];
        using var payloadStream = new MemoryStream(payload, writable: false);
        for (var i = 0; i < pixelCount; i++)
            pixels[i] = ReadColor(payloadStream);

        return pixels;
    }

    private static Color[] ReadPaletteWithLength(Stream stream)
    {
        var colorCount = ReadUint(stream);
        var colors = new Color[colorCount];
        for (var i = 0; i < colorCount; i++)
            colors[i] = ReadColor(stream);

        if (colors.Length == 0)
            throw new InvalidDataException("AXP palette must not be empty.");

        return colors;
    }

    private static byte[] ReadPayload(Stream stream)
    {
        var payloadLength = ReadUint(stream);
        var payload = new byte[payloadLength];
        stream.ReadExactly(payload);
        return payload;
    }

    private static void Write7BitEncodedInt(Stream stream, int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        var unsigned = (uint)value;
        while (unsigned >= 0x80)
        {
            stream.WriteByte((byte)(unsigned | 0x80));
            unsigned >>= 7;
        }

        stream.WriteByte((byte)unsigned);
    }

    private static int Read7BitEncodedInt(Stream stream)
    {
        var value = 0;
        var shift = 0;

        while (shift < 35)
        {
            var currentByte = stream.ReadByte();
            if (currentByte < 0)
                throw new EndOfStreamException("Unexpected end of 7-bit encoded integer.");

            value |= (currentByte & 0x7F) << shift;
            if ((currentByte & 0x80) == 0)
                return value;

            shift += 7;
        }

        throw new InvalidDataException("Invalid 7-bit encoded integer.");
    }

    private static Image CreateImageFromFlatPixels(int width, int height, IReadOnlyList<Color> pixels)
    {
        var image = CreateImage(width, height);
        var index = 0;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            image.Pixels[x, y] = pixels[index++];

        return image;
    }

    private static Color[] ReadLegacyAxpPalette(Stream stream, uint colorCount)
    {
        var colors = new Color[colorCount];
        for (var i = 0; i < colorCount; i++)
            colors[i] = ReadColor(stream);

        return colors;
    }

    private static Color[] ReadLegacyAxpPixels(Stream stream, uint imageWidth, uint imageHeight, IReadOnlyList<Color> colors)
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

    private static Image CreateLegacyAxpImage(uint imageWidth, uint imageHeight, IReadOnlyList<Color> pixels)
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

