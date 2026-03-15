using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PixelPaint.Services;
using Image = PixelPaint.Models.Image;

namespace PixelPaint.Controls;

public sealed class PixelCanvas : Control
{
    private const double TransparencyTileSize = 12d;

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<PixelCanvas, double>(nameof(Zoom), 1d);

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<PixelCanvas, IBrush?>(nameof(Background));

    public static readonly StyledProperty<bool> ShowGridLinesProperty =
        AvaloniaProperty.Register<PixelCanvas, bool>(nameof(ShowGridLines), true);

    private static readonly Pen GridPen = new(new SolidColorBrush(Color.FromArgb(96, 128, 128, 128)));
    private static readonly SolidColorBrush TransparencyLightBrush = new(Color.FromRgb(236, 236, 236));
    private static readonly SolidColorBrush TransparencyDarkBrush = new(Color.FromRgb(208, 208, 208));

    private Image? _image;
    private WriteableBitmap? _bitmap;

    static PixelCanvas()
    {
        AffectsMeasure<PixelCanvas>(ZoomProperty);
        AffectsRender<PixelCanvas>(ZoomProperty, BackgroundProperty, ShowGridLinesProperty);
    }

    public PixelCanvas()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
        ClipToBounds = true;
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        UseLayoutRounding = true;
    }

    public double Zoom
    {
        get => Math.Max(GetValue(ZoomProperty), 0.05d);
        set => SetValue(ZoomProperty, Math.Clamp(value, 0.05d, 64d));
    }

    public bool ShowGridLines
    {
        get => GetValue(ShowGridLinesProperty);
        set => SetValue(ShowGridLinesProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public Image? CurrentImage => _image;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var contentBounds = new Rect(GetContentSize());

        if (Background is not null)
            context.FillRectangle(Background, contentBounds);
        else
            DrawTransparencyBackground(context, contentBounds);

        if (_bitmap is null)
            return;

        context.DrawImage(
            _bitmap,
            new Rect(0, 0, _bitmap.PixelSize.Width, _bitmap.PixelSize.Height),
            contentBounds);

        if (ShowGridLines && Zoom >= 8)
            DrawGridLines(context, contentBounds.Size);
    }

    private static void DrawTransparencyBackground(DrawingContext context, Rect bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        for (var y = 0d; y < bounds.Height; y += TransparencyTileSize)
        {
            var rowIndex = (int)(y / TransparencyTileSize);
            var tileHeight = Math.Min(TransparencyTileSize, bounds.Height - y);

            for (var x = 0d; x < bounds.Width; x += TransparencyTileSize)
            {
                var columnIndex = (int)(x / TransparencyTileSize);
                var tileWidth = Math.Min(TransparencyTileSize, bounds.Width - x);
                var brush = (rowIndex + columnIndex) % 2 == 0
                    ? TransparencyLightBrush
                    : TransparencyDarkBrush;

                context.FillRectangle(brush, new Rect(x, y, tileWidth, tileHeight));
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return GetContentSize();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return GetContentSize();
    }

    public void SetImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _image = image;
        RebuildBitmap();
        InvalidateMeasure();
        InvalidateVisual();
    }

    public void ClearImage()
    {
        _image = null;
        _bitmap?.Dispose();
        _bitmap = null;
        InvalidateMeasure();
        InvalidateVisual();
    }

    public void RefreshImage()
    {
        if (_image is null)
        {
            ClearImage();
            return;
        }

        EnsureBitmapMatchesImage();
        WriteEntireImageToBitmap();
        InvalidateVisual();
    }

    public void ApplyPixelChanges(IReadOnlyList<PixelChange> pixelChanges)
    {
        if (_image is null || pixelChanges.Count == 0)
            return;

        EnsureBitmapMatchesImage();
        if (_bitmap is null)
            return;

        using var buffer = _bitmap.Lock();
        foreach (var pixelChange in pixelChanges)
            WritePixel(buffer, pixelChange.X, pixelChange.Y, pixelChange.Color);

        InvalidateVisual();
    }

    public (int x, int y)? TryGetPixel(Point point)
    {
        if (_image is null || Zoom <= 0)
            return null;

        var x = (int)(point.X / Zoom);
        var y = (int)(point.Y / Zoom);
        if (x < 0 || x >= _image.PixelCountX || y < 0 || y >= _image.PixelCountY)
            return null;

        return (x, y);
    }

    private void EnsureBitmapMatchesImage()
    {
        if (_image is null)
            return;

        if (_bitmap is not null &&
            _bitmap.PixelSize.Width == _image.PixelCountX &&
            _bitmap.PixelSize.Height == _image.PixelCountY)
        {
            return;
        }

        RebuildBitmap();
    }

    private void RebuildBitmap()
    {
        _bitmap?.Dispose();
        _bitmap = null;

        if (_image is null)
            return;

        _bitmap = new WriteableBitmap(
            new PixelSize(_image.PixelCountX, _image.PixelCountY),
            new Vector(96, 96),
            PixelFormats.Bgra8888,
            AlphaFormat.Unpremul);

        WriteEntireImageToBitmap();
    }

    private void WriteEntireImageToBitmap()
    {
        if (_image is null || _bitmap is null)
            return;

        using var buffer = _bitmap.Lock();
        for (var y = 0; y < _image.PixelCountY; y++)
        for (var x = 0; x < _image.PixelCountX; x++)
            WritePixel(buffer, x, y, _image.Pixels[x, y]);
    }

    private static void WritePixel(ILockedFramebuffer buffer, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= buffer.Size.Width || y >= buffer.Size.Height)
            return;

        var offset = y * buffer.RowBytes + x * 4;
        Marshal.WriteByte(buffer.Address, offset, color.B);
        Marshal.WriteByte(buffer.Address, offset + 1, color.G);
        Marshal.WriteByte(buffer.Address, offset + 2, color.R);
        Marshal.WriteByte(buffer.Address, offset + 3, color.A);
    }

    private Size GetContentSize()
    {
        if (_image is null)
            return default;

        return new Size(_image.PixelCountX * Zoom, _image.PixelCountY * Zoom);
    }

    private void DrawGridLines(DrawingContext context, Size contentSize)
    {
        if (_image is null)
            return;

        for (var x = 0; x <= _image.PixelCountX; x++)
        {
            var position = x * Zoom;
            context.DrawLine(GridPen, new Point(position, 0), new Point(position, contentSize.Height));
        }

        for (var y = 0; y <= _image.PixelCountY; y++)
        {
            var position = y * Zoom;
            context.DrawLine(GridPen, new Point(0, position), new Point(contentSize.Width, position));
        }
    }
}

