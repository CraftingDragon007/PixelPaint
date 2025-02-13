using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Image = PixelPaint.Models.Image;

namespace PixelPaint.Extensions;

public static class GridExtensions
{
    public static Image ToImage(this Grid grid)
    {
        var image = new Image
        {
            PixelCountX = grid.ColumnDefinitions.Count,
            PixelCountY = grid.RowDefinitions.Count,
            Pixels = new Color[grid.ColumnDefinitions.Count, grid.RowDefinitions.Count]
        };

        for (var x = 0; x < grid.ColumnDefinitions.Count; x++)
        {
            for (var y = 0; y < grid.RowDefinitions.Count; y++)
            {
                var pixel = grid.Children.Cast<Rectangle>().First(c => Grid.GetColumn(c) == x && Grid.GetRow(c) == y);
                image.Pixels[x, y] = (pixel.Fill as SolidColorBrush)?.Color ?? Colors.Transparent;
            }
        }

        return image;
    }
}