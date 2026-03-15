using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PixelPaint.Models;

namespace PixelPaint.Services;

public partial class FileService
{
    /// <summary>
    ///     Imports a PNG or BMP file as pixel art by mapping each source pixel
    ///     directly to one art pixel in the resulting <see cref="Image"/>.
    /// </summary>
    /// <param name="path">Absolute path to the PNG or BMP file.</param>
    /// <returns>A new <see cref="Image"/> whose dimensions match the source file.</returns>
    private static Image ImportImageFromBitmap(string path)
    {
        using var sourceBitmap = new Bitmap(path);

        var width = sourceBitmap.PixelSize.Width;
        var height = sourceBitmap.PixelSize.Height;

        // CopyPixels produces BGRA8888 (unpremultiplied) data.
        var stride = width * 4;
        var bufferSize = stride * height;
        var buffer = new byte[bufferSize];

        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
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

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var offset = y * stride + x * 4;
            var b = buffer[offset];
            var g = buffer[offset + 1];
            var r = buffer[offset + 2];
            var a = buffer[offset + 3];

            image.Pixels[x, y] = Color.FromArgb(a, r, g, b);
        }

        return image;
    }
}

