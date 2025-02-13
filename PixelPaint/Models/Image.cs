using Avalonia.Media;

namespace PixelPaint.Models;

/// <summary>
///     Represents an image
/// </summary>
public class Image
{
    public int PixelCount { get; set; }
    public int PixelCountX { get; set; }
    public int PixelCountY { get; set; }

    public required Color[,] Pixels { get; set; }
}