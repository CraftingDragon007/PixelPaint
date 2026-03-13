using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Avalonia.Media;
using PixelPaint.Models;

namespace PixelPaint.Services;

public partial class FileService
{
    /// <summary>
    ///     Save an image as an SVG file. Each pixel becomes a &lt;rect&gt; in viewBox coordinates.
    ///     The display size is scaled by 20 so the file looks good in browsers.
    /// </summary>
    private static void SaveImageToSvg(Image image, string path)
    {
        var ns = XNamespace.Get(SvgNamespace);
        var rects = CreateSvgRectangles(image, ns);

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "svg",
                new XAttribute("xmlns", ns.NamespaceName),
                new XAttribute("width", image.PixelCountX * SvgPreviewScale),
                new XAttribute("height", image.PixelCountY * SvgPreviewScale),
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
        var ns = svg.Name.Namespace;
        var rectElements = svg.Descendants(ns + "rect").ToList();
        if (!rectElements.Any())
            throw new InvalidDataException("SVG contains no <rect> elements – cannot import as pixel art");

        var rectangles = rectElements
            .Select(ParseSvgRectangle)
            .Where(IsDrawableSvgRectangle)
            .ToList();

        if (!rectangles.Any())
            throw new InvalidDataException("SVG has no drawable rect elements");

        var pixelSize = DetectSvgPixelSize(rectangles);
        var (pixelCountX, pixelCountY) = DetermineSvgGridSize(svg, rectangles, pixelSize);
        if (pixelCountX <= 0 || pixelCountY <= 0)
            throw new InvalidDataException("Could not determine pixel grid dimensions from SVG");

        var pixels = CreateSvgPixelBuffer(pixelCountX, pixelCountY);
        ApplySvgRectanglesToPixels(rectangles, pixels, pixelSize, pixelCountX, pixelCountY);

        var image = CreateImage(pixelCountX, pixelCountY);
        image.Pixels = pixels;

        var editorW = (uint)Math.Clamp(pixelCountX * SvgPreviewScale, 400, 1400);
        var editorH = (uint)Math.Clamp(pixelCountY * SvgPreviewScale, 300, 900);
        return (image, (editorW, editorH));
    }

    private static List<XElement> CreateSvgRectangles(Image image, XNamespace svgNamespace)
    {
        var rects = new List<XElement>(image.PixelCountX * image.PixelCountY);

        for (var y = 0; y < image.PixelCountY; y++)
        for (var x = 0; x < image.PixelCountX; x++)
        {
            var color = image.Pixels[x, y];
            var rect = new XElement(svgNamespace + "rect",
                new XAttribute("x", x),
                new XAttribute("y", y),
                new XAttribute("width", 1),
                new XAttribute("height", 1),
                new XAttribute("fill", $"#{color.R:X2}{color.G:X2}{color.B:X2}"));

            if (color.A < 255)
            {
                rect.Add(new XAttribute(
                    "fill-opacity",
                    (color.A / 255.0).ToString("F4", CultureInfo.InvariantCulture)));
            }

            rects.Add(rect);
        }

        return rects;
    }

    private static SvgRectangle ParseSvgRectangle(XElement element)
    {
        return new SvgRectangle(
            ParseSvgDouble(element.Attribute("x")?.Value ?? "0"),
            ParseSvgDouble(element.Attribute("y")?.Value ?? "0"),
            ParseSvgDouble(element.Attribute("width")?.Value ?? "1"),
            ParseSvgDouble(element.Attribute("height")?.Value ?? "1"),
            element.Attribute("fill")?.Value ?? "black",
            element.Attribute("fill-opacity")?.Value);
    }

    private static bool IsDrawableSvgRectangle(SvgRectangle rectangle) =>
        rectangle.Width > 0 && rectangle.Height > 0 && rectangle.Fill != "none";

    private static int DetectSvgPixelSize(IEnumerable<SvgRectangle> rectangles)
    {
        return rectangles
            .Select(rectangle => (int)Math.Round(rectangle.Width))
            .Where(width => width > 0)
            .GroupBy(width => width)
            .OrderByDescending(group => group.Count())
            .First().Key;
    }

    private static (int pixelCountX, int pixelCountY) DetermineSvgGridSize(
        XElement svg,
        IReadOnlyCollection<SvgRectangle> rectangles,
        int pixelSize)
    {
        var viewBox = svg.Attribute("viewBox")?.Value;
        if (viewBox != null)
        {
            var parts = viewBox.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
            return (
                (int)Math.Round(ParseSvgDouble(parts[2]) / pixelSize),
                (int)Math.Round(ParseSvgDouble(parts[3]) / pixelSize));
        }

        return (
            (int)Math.Round(rectangles.Max(rectangle => rectangle.X) / pixelSize) + 1,
            (int)Math.Round(rectangles.Max(rectangle => rectangle.Y) / pixelSize) + 1);
    }

    private static Color[,] CreateSvgPixelBuffer(int pixelCountX, int pixelCountY)
    {
        var pixels = new Color[pixelCountX, pixelCountY];
        for (var x = 0; x < pixelCountX; x++)
        for (var y = 0; y < pixelCountY; y++)
            pixels[x, y] = Colors.White;

        return pixels;
    }

    private static void ApplySvgRectanglesToPixels(
        IEnumerable<SvgRectangle> rectangles,
        Color[,] pixels,
        int pixelSize,
        int pixelCountX,
        int pixelCountY)
    {
        foreach (var rectangle in rectangles)
        {
            var px = (int)Math.Round(rectangle.X / pixelSize);
            var py = (int)Math.Round(rectangle.Y / pixelSize);
            if (px < 0 || px >= pixelCountX || py < 0 || py >= pixelCountY)
                continue;

            var color = ParseSvgColor(rectangle.Fill);
            if (rectangle.Opacity != null)
            {
                var alpha = (byte)(ParseSvgDouble(rectangle.Opacity) * 255);
                color = Color.FromArgb(alpha, color.R, color.G, color.B);
            }

            pixels[px, py] = color;
        }
    }

    private static double ParseSvgDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var trimmed = value.TrimEnd('p', 'x', 't', 'e', 'm', 'c', 'i', 'n', '%');
        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
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
            {
                return Color.FromArgb(
                    (byte)(ParseSvgDouble(inner[3].Trim()) * 255),
                    (byte)ParseSvgDouble(inner[0].Trim()),
                    (byte)ParseSvgDouble(inner[1].Trim()),
                    (byte)ParseSvgDouble(inner[2].Trim()));
            }
        }

        if (fill.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase))
        {
            var inner = fill[4..^1].Split(',');
            if (inner.Length >= 3)
            {
                return Color.FromRgb(
                    (byte)ParseSvgDouble(inner[0].Trim()),
                    (byte)ParseSvgDouble(inner[1].Trim()),
                    (byte)ParseSvgDouble(inner[2].Trim()));
            }
        }

        return Color.TryParse(fill, out var namedColor)
            ? namedColor
            : Colors.Black;
    }

    private sealed record SvgRectangle(
        double X,
        double Y,
        double Width,
        double Height,
        string Fill,
        string? Opacity);
}

