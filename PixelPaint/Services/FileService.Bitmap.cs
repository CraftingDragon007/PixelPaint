using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using PixelPaint.Models;
using SixLabors.ImageSharp.PixelFormats;
using AvaloniaColor = Avalonia.Media.Color;
using ImportedImage = PixelPaint.Models.Image;

namespace PixelPaint.Services;

public partial class FileService
{
    /// <summary>
    ///     Imports a JPEG, BMP, GIF, PBM, PGM, PPM, PNG, TGA, TIFF, WebP or QOI file as pixel art by
    ///     mapping each source pixel directly to one art pixel in the resulting <see cref="Image"/>.
    /// </summary>
    /// <param name="path">Absolute path to a supported raster image file.</param>
    /// <returns>A new <see cref="ImportedImage"/> whose dimensions match the source file.</returns>
    private static ImportedImage ImportImageFromBitmap(string path)
    {
        try
        {
            using var sourceBitmap = new Bitmap(path);
            return ImportViaAvaloniaCopyPixels(sourceBitmap);
        }
        catch (NotSupportedException)
        {
            // Some palette/gray source formats cannot be copied by Avalonia directly.
            return ImportViaImageSharp(path);
        }
        catch (ArgumentException)
        {
            // Some formats are unsupported by the Avalonia decoder itself.
            return ImportViaImageSharp(path);
        }
    }

    private static unsafe ImportedImage ImportViaAvaloniaCopyPixels(Bitmap sourceBitmap)
    {
        var width = sourceBitmap.PixelSize.Width;
        var height = sourceBitmap.PixelSize.Height;

        // CopyPixels produces BGRA8888 (unpremultiplied) data.
        var stride = width * 4;
        var bufferSize = stride * height;
        var pixelBuffer = new byte[bufferSize];

        var handle = GCHandle.Alloc(pixelBuffer, GCHandleType.Pinned);
        try
        {
            sourceBitmap.CopyPixels(
                new PixelRect(0, 0, width, height),
                handle.AddrOfPinnedObject(),
                bufferSize,
                stride);
        }
        finally
        {
            handle.Free();
        }

        var image = CreateImage(width, height);

        fixed (byte* dataPtr = pixelBuffer)
        {
            for (var y = 0; y < height; y++)
            {
                var row = dataPtr + y * stride;
                for (var x = 0; x < width; x++)
                {
                    var pixelPtr = row + x * 4;
                    image.Pixels[x, y] = AvaloniaColor.FromArgb(pixelPtr[3], pixelPtr[2], pixelPtr[1], pixelPtr[0]);
                }
            }
        }

        return image;
    }

    private static ImportedImage ImportViaImageSharp(string path)
    {
        using var sourceImage = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
        var width = sourceImage.Width;
        var height = sourceImage.Height;
        var image = CreateImage(width, height);

        sourceImage.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < width; x++)
                {
                    var pixel = row[x];
                    image.Pixels[x, y] = AvaloniaColor.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B);
                }
            }
        });

        return image;
    }
}
