using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Image = PixelPaint.Models.Image;

namespace PixelPaint.Services;

public interface IDrawingService
{
    Grid? ImagePanel { get; set; }
    Color CurrentColor { get; set; }
    Tool CurrentTool { get; set; }
    
    void DrawImage(Image image);

    Color GetContrastColor(Color color);

    void OnPointerPressed(object? sender, PointerPressedEventArgs e);
    void OnPointerMoved(object? sender, PointerEventArgs e);
    void OnPointerReleased(object? sender, PointerReleasedEventArgs e);
    void DrawEmptyImage();
    event Action<object?, Color> OtherColorChanged;
}

public enum Tool
{
    Brush,
    Fill,
    Pipette
}

public class DrawingService : IDrawingService
{
    private Image? _image;
    private bool _pointerDown;

    public Color GetContrastColor(Color color)
    {
        // Calculate the luminance of the color
        var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;

        // If the luminance is high, return a dark color, otherwise return a light color
        return luminance > 0.5 ? Colors.Black : Colors.White;
    }

    public void DrawEmptyImage()
    {
        const int pixelCountX = 32;
        const int pixelCountY = 16;
        var image = new Image
        {
            PixelCountX = pixelCountX,
            PixelCountY = pixelCountY,
            Pixels = new Color[32, 16],
            PixelCount = pixelCountX * pixelCountY
        };

        for (var x = 0; x < image.PixelCountX; x++)
        {
            for (var y = 0; y < image.PixelCountY; y++)
            {
                image.Pixels[x, y] = Colors.White;
            }
        }
        
        DrawImage(image);
    }

    public void DrawImage(Image image)
    {
        if (ImagePanel is null) throw new NullReferenceException("ImagePanel is null");
        var size = ImagePanel.Bounds.Size;
        var pixelSize = new Size(size.Width / image.PixelCountX, size.Height / image.PixelCountY);
        ImagePanel.Children.Clear();
        var columnDefinitions = new ColumnDefinitions();
        var rowDefinitions = new RowDefinitions();

        for (var x = 0; x < image.PixelCountX; x++) columnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        for (var y = 0; y < image.PixelCountY; y++) rowDefinitions.Add(new RowDefinition(GridLength.Star));

        ImagePanel.ColumnDefinitions = columnDefinitions;
        ImagePanel.RowDefinitions = rowDefinitions;
        ImagePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        ImagePanel.VerticalAlignment = VerticalAlignment.Stretch;
        ImagePanel.Background = Brushes.White;
        ImagePanel.Margin = new Thickness(0);
        ImagePanel.UseLayoutRounding = true;

        for (var x = 0; x < image.PixelCountX; x++)
        for (var y = 0; y < image.PixelCountY; y++)
        {
            var pixel = image.Pixels[x, y];
            var rectangle = new Rectangle
            {
                Fill = new SolidColorBrush(pixel),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            Grid.SetColumn(rectangle, x);
            Grid.SetRow(rectangle, y);
            ImagePanel.Children.Add(rectangle);
        }

        _image = image;
    }

    private void Fill(Color color, (int x, int y) position)
    {
        if (_image is null || ImagePanel is null) return;
        var pixels = new Queue<(int x, int y)>();
        pixels.Enqueue(position);
        var targetColor = _image.Pixels[position.x, position.y];
        var brush = new SolidColorBrush(color);
        while (pixels.Count > 0)
        {
            var current = pixels.Dequeue();
            if (current.x < 0 || current.x >= _image.PixelCountX || current.y < 0 || current.y >= _image.PixelCountY) continue;
            if (_image.Pixels[current.x, current.y] != targetColor) continue;
            _image.Pixels[current.x, current.y] = color;
            var rectangle = ImagePanel.Children.Where(c => Grid.GetColumn(c) == current.x && Grid.GetRow(c) == current.y).OfType<Rectangle>().First();
            rectangle.Fill = brush;
            pixels.Enqueue((current.x + 1, current.y));
            pixels.Enqueue((current.x - 1, current.y));
            pixels.Enqueue((current.x, current.y + 1));
            pixels.Enqueue((current.x, current.y - 1));
        }
    }

    public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ImagePanel is null || _image is null) return;
        var position = e.GetPosition(ImagePanel);
        var x = (int)(position.X / ImagePanel.Bounds.Size.Width * _image.PixelCountX);
        var y = (int)(position.Y / ImagePanel.Bounds.Size.Height * _image.PixelCountY);

        switch (CurrentTool)
        {
            case Tool.Brush:
                _image.Pixels[x, y] = CurrentColor;
                ImagePanel.Children.Where(c => Grid.GetColumn(c) == x && Grid.GetRow(c) == y).OfType<Rectangle>().First().Fill =
                    new SolidColorBrush(CurrentColor);
                break;
            case Tool.Fill:
                Fill(CurrentColor, (x, y));
                break;
            case Tool.Pipette:
                CurrentColor = _image.Pixels[x, y];
                OtherColorChanged?.Invoke(this, CurrentColor);
                break;
            default:
                throw new InvalidEnumArgumentException("Invalid tool");
        }
        _pointerDown = true;
    }

    public void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_pointerDown || ImagePanel is null || _image is null || CurrentTool != Tool.Brush) return;
        var position = e.GetPosition(ImagePanel);
        // Check if the pointer is inside the image panel
        if (position.X < 0 || position.Y < 0 || position.X >= ImagePanel.Bounds.Size.Width ||
            position.Y >= ImagePanel.Bounds.Size.Height) return;
        var x = (int)(position.X / ImagePanel.Bounds.Size.Width * _image.PixelCountX);
        var y = (int)(position.Y / ImagePanel.Bounds.Size.Height * _image.PixelCountY);
        _image.Pixels[x, y] = CurrentColor;
        ImagePanel.Children.Where(c => Grid.GetColumn(c) == x && Grid.GetRow(c) == y).OfType<Rectangle>().First().Fill =
            new SolidColorBrush(CurrentColor);
    }

    public void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pointerDown = false;
    }
    
    
    public event Action<object?, Color>? OtherColorChanged;
    public Tool CurrentTool { get; set; } = Tool.Brush;
    public Grid? ImagePanel { get; set; }
    public Color CurrentColor { get; set; } = Colors.Black;
}