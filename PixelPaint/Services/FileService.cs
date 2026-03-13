using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
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
            case "svg":
                SaveImageToSvg(image, path);
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
            "svg" => LoadImageFromSvg(path),
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
        new("SVG-Bild")
        {
            Patterns = ["*.svg"], MimeTypes =
                ["image/svg+xml"],
            AppleUniformTypeIdentifiers = ["public.svg-image"]
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

        var bitsPerPixel = (uint)Math.Ceiling(Math.Log2(Math.Max(1, colors.Length)));

        if (colors.Length <= 1) return;
        var index = colors.Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i);

        byte currentByte = 0;
        var bitsWritten = 0;

        foreach (var pixelColor in image.Pixels) // Iterate through actual pixels
        {
            var colorIndex = index[pixelColor];
            for (var i = 0; i < bitsPerPixel; i++)
            {
                // Write LSB first into the current byte
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
        for (var i = 0; i < colorCount; i++)
        {
            colors[i] = Color.FromArgb(
                (byte)fileStream.ReadByte(),
                (byte)fileStream.ReadByte(),
                (byte)fileStream.ReadByte(),
                (byte)fileStream.ReadByte()
            );
        }

        // Calculate bits per pixel
        var bitsPerPixel = colorCount > 1 ? (int)Math.Ceiling(Math.Log2(colorCount)) : 0;

        // Read pixel data
        var pixels = new Color[imageWidth * imageHeight];
        if (colorCount == 0)
        {
            // If no colors, fill the image with transparent pixels
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = Colors.Transparent;
            
        }
        else if (colorCount == 1)
        {
            // If only one color, fill the entire image with it. No pixel data to read.
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = colors[0];
        }
        else // colorCount > 1, so bitsPerPixel will be > 0
        {
            byte currentByte = 0;
            var bitsAvailable = 0;
            var pixelIndex = 0;

            while (pixelIndex < pixels.Length)
            {
                // Read a new byte if no bits are available
                if (bitsAvailable < bitsPerPixel) // Check if enough bits for the next pixel
                {
                    // Read the next byte from the stream
                    var nextByte = fileStream.ReadByte();
                    if (nextByte == -1) // End of stream reached unexpectedly
                    {
                        // This indicates corrupt or truncated file
                        throw new EndOfStreamException("Unexpected end of pixel data stream.");
                    }
                    currentByte |= (byte)(nextByte << bitsAvailable); // Append new byte's bits
                    bitsAvailable += 8;
                }

                var colorIndex = 0;
                // Read bitsPerPixel bits for the current pixel's color index
                for (var i = 0; i < bitsPerPixel; i++)
                {
                    colorIndex |= ((currentByte & 1) << i); // Read LSB first
                    currentByte >>= 1;
                }
                bitsAvailable -= bitsPerPixel;

                if (colorIndex >= colors.Length)
                {
                    // This can happen if the bitsPerPixel is slightly off,
                    // or if the data is corrupted.
                    throw new InvalidDataException($"Color index {colorIndex} out of bounds for palette size {colors.Length}");
                }
                pixels[pixelIndex++] = colors[colorIndex];
            }
        }

        var image = new Image
        {
            PixelCountX = (int)imageWidth,
            PixelCountY = (int)imageHeight,
            Pixels = new Color[imageWidth, imageHeight],
            PixelCount = (int)(imageWidth * imageHeight)
        };
        
        // Populate the 2D array from the 1D array
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

    // ── SVG ──────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Save an image as an SVG file. Each pixel becomes a &lt;rect&gt; in viewBox coordinates.
    ///     The display size is scaled by 20 so the file looks good in browsers.
    /// </summary>
    private static void SaveImageToSvg(Image image, string path)
    {
        var ns = XNamespace.Get("http://www.w3.org/2000/svg");
        var rects = new List<XElement>(image.PixelCountX * image.PixelCountY);

        for (var y = 0; y < image.PixelCountY; y++)
        for (var x = 0; x < image.PixelCountX; x++)
        {
            var c = image.Pixels[x, y];
            var fill = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            var rect = new XElement(ns + "rect",
                new XAttribute("x", x),
                new XAttribute("y", y),
                new XAttribute("width", 1),
                new XAttribute("height", 1),
                new XAttribute("fill", fill));
            if (c.A < 255)
                rect.Add(new XAttribute("fill-opacity",
                    (c.A / 255.0).ToString("F4", CultureInfo.InvariantCulture)));
            rects.Add(rect);
        }

        // Display size = pixelCount × 20 so SVG looks reasonable in a browser
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "svg",
                new XAttribute("xmlns", ns.NamespaceName),
                new XAttribute("width", image.PixelCountX * 20),
                new XAttribute("height", image.PixelCountY * 20),
                new XAttribute("viewBox", $"0 0 {image.PixelCountX} {image.PixelCountY}"),
                new XAttribute("shape-rendering", "crispEdges"),
                rects));

        doc.Save(path);
    }

    /// <summary>
    ///     Load an image from an SVG file.
    ///     Supports:
    ///     <list type="bullet">
    ///       <item>PixelPaint SVG (viewBox = pixel grid, rect width = 1)</item>
    ///       <item>Scaled pixel-art SVG (e.g. Aseprite: rect width = pixelSize &gt; 1)</item>
    ///     </list>
    ///     The pixel size is detected as the most frequently occurring rect width.
    /// </summary>
    private static (Image image, (uint width, uint height) editorSize) LoadImageFromSvg(string path)
    {
        var doc = XDocument.Load(path);
        var svg = doc.Root ?? throw new InvalidDataException("Invalid SVG: no root element");
        var ns = svg.Name.Namespace; // could be empty or the SVG namespace

        // Collect all <rect> elements anywhere in the tree
        var rectElements = svg.Descendants(ns + "rect").ToList();
        if (!rectElements.Any())
            throw new InvalidDataException("SVG contains no <rect> elements – cannot import as pixel art");

        // Parse each rect into a working struct
        var parsed = rectElements
            .Select(r => new
            {
                X      = ParseSvgDouble(r.Attribute("x")?.Value      ?? "0"),
                Y      = ParseSvgDouble(r.Attribute("y")?.Value      ?? "0"),
                Width  = ParseSvgDouble(r.Attribute("width")?.Value  ?? "1"),
                Height = ParseSvgDouble(r.Attribute("height")?.Value ?? "1"),
                Fill   = r.Attribute("fill")?.Value ?? "black",
                Opacity = r.Attribute("fill-opacity")?.Value
            })
            .Where(r => r.Width > 0 && r.Height > 0 && r.Fill != "none")
            .ToList();

        if (!parsed.Any())
            throw new InvalidDataException("SVG has no drawable rect elements");

        // Detect pixel size = most common rect width
        var pixelSize = parsed
            .Select(r => (int)Math.Round(r.Width))
            .Where(w => w > 0)
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .First().Key;

        // Determine grid dimensions
        int pixelCountX, pixelCountY;
        var viewBox = svg.Attribute("viewBox")?.Value;
        if (viewBox != null)
        {
            var parts = viewBox.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
            pixelCountX = (int)Math.Round(ParseSvgDouble(parts[2]) / pixelSize);
            pixelCountY = (int)Math.Round(ParseSvgDouble(parts[3]) / pixelSize);
        }
        else
        {
            pixelCountX = (int)Math.Round(parsed.Max(r => r.X) / pixelSize) + 1;
            pixelCountY = (int)Math.Round(parsed.Max(r => r.Y) / pixelSize) + 1;
        }

        if (pixelCountX <= 0 || pixelCountY <= 0)
            throw new InvalidDataException("Could not determine pixel grid dimensions from SVG");

        // Build pixel array (default: white)
        var pixels = new Color[pixelCountX, pixelCountY];
        for (var px = 0; px < pixelCountX; px++)
        for (var py = 0; py < pixelCountY; py++)
            pixels[px, py] = Colors.White;

        foreach (var r in parsed)
        {
            var px = (int)Math.Round(r.X / pixelSize);
            var py = (int)Math.Round(r.Y / pixelSize);
            if (px < 0 || px >= pixelCountX || py < 0 || py >= pixelCountY) continue;

            var color = ParseSvgColor(r.Fill);
            if (r.Opacity != null)
            {
                var alpha = (byte)(ParseSvgDouble(r.Opacity) * 255);
                color = Color.FromArgb(alpha, color.R, color.G, color.B);
            }
            pixels[px, py] = color;
        }

        var image = new Image
        {
            PixelCountX = pixelCountX,
            PixelCountY = pixelCountY,
            Pixels = pixels,
            PixelCount = pixelCountX * pixelCountY
        };

        // Determine a sensible editor display size (cap at 1400 × 900)
        var editorW = (uint)Math.Clamp(pixelCountX * 20, 400, 1400);
        var editorH = (uint)Math.Clamp(pixelCountY * 20, 300, 900);
        return (image, (editorW, editorH));
    }

    private static double ParseSvgDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        // Strip unit suffixes (px, pt, em …)
        var trimmed = value.TrimEnd('p', 'x', 't', 'e', 'm', 'c', 'i', 'n', '%');
        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    /// <summary>
    ///     Parse an SVG color string into an Avalonia <see cref="Color"/>.
    ///     Handles #rrggbb, #rgb, #aarrggbb, rgb(), rgba() and common named colors.
    /// </summary>
    private static Color ParseSvgColor(string fill)
    {
        fill = fill.Trim();

        if (fill.StartsWith('#'))
        {
            var hex = fill[1..];
            return hex.Length switch
            {
                3 => Color.FromRgb(
                        Convert.ToByte(new string(hex[0], 2), 16),
                        Convert.ToByte(new string(hex[1], 2), 16),
                        Convert.ToByte(new string(hex[2], 2), 16)),
                6 => Color.FromRgb(
                        Convert.ToByte(hex[..2], 16),
                        Convert.ToByte(hex[2..4], 16),
                        Convert.ToByte(hex[4..6], 16)),
                8 => Color.FromArgb(
                        Convert.ToByte(hex[..2], 16),
                        Convert.ToByte(hex[2..4], 16),
                        Convert.ToByte(hex[4..6], 16),
                        Convert.ToByte(hex[6..8], 16)),
                _ => Colors.Black
            };
        }

        if (fill.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase))
        {
            var inner = fill[5..^1].Split(',');
            if (inner.Length >= 4)
                return Color.FromArgb(
                    (byte)(ParseSvgDouble(inner[3].Trim()) * 255),
                    (byte)ParseSvgDouble(inner[0].Trim()),
                    (byte)ParseSvgDouble(inner[1].Trim()),
                    (byte)ParseSvgDouble(inner[2].Trim()));
        }

        if (fill.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase))
        {
            var inner = fill[4..^1].Split(',');
            if (inner.Length >= 3)
                return Color.FromRgb(
                    (byte)ParseSvgDouble(inner[0].Trim()),
                    (byte)ParseSvgDouble(inner[1].Trim()),
                    (byte)ParseSvgDouble(inner[2].Trim()));
        }

        // Named colors via Avalonia's built-in parser
        if (Color.TryParse(fill, out var namedColor)) return namedColor;

        return Colors.Black;
    }
}