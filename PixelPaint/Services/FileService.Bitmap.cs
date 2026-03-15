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
    private static unsafe Image ImportImageFromBitmap(string path)
    {
        using var sourceBitmap = new Bitmap(path);

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
                    image.Pixels[x, y] = Color.FromArgb(pixelPtr[3], pixelPtr[2], pixelPtr[1], pixelPtr[0]);
                }
            }
        }

        return image;
    }
}
