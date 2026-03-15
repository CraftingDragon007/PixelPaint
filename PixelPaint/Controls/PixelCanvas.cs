using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private const int ParallelPixelThreshold = 65_536;

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<PixelCanvas, double>(nameof(Zoom), 1d);

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<PixelCanvas, IBrush?>(nameof(Background));

    public static readonly StyledProperty<bool> ShowGridLinesProperty =
        AvaloniaProperty.Register<PixelCanvas, bool>(nameof(ShowGridLines), true);

    private static readonly Pen GridPen = new(new SolidColorBrush(Color.FromArgb(96, 128, 128, 128)));
    private static readonly SolidColorBrush TransparencyLightBrush = new(Color.FromRgb(236, 236, 236));
    private static readonly SolidColorBrush TransparencyDarkBrush = new(Color.FromRgb(208, 208, 208));
    private static readonly IBrush TransparencyTileBrush = CreateTransparencyTileBrush();

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
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
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

    /// <summary>Current scroll offset of the host ScrollViewer in canvas (zoomed) coordinates.</summary>
    public Vector ScrollViewportOffset { get; set; }

    /// <summary>Visible area of the host ScrollViewer in canvas (zoomed) coordinates.</summary>
    public Size ScrollViewportSize { get; set; }

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

    protected override Size MeasureOverride(Size availableSize) => GetContentSize();

    protected override Size ArrangeOverride(Size finalSize) => GetContentSize();

    private Size GetContentSize()
    {
        if (_image is null)
            return default;

        return new Size(_image.PixelCountX * Zoom, _image.PixelCountY * Zoom);
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

    private unsafe void WriteEntireImageToBitmap()
    {
        if (_image is null || _bitmap is null)
            return;

        using var buffer = _bitmap.Lock();
        var basePtr = (byte*)buffer.Address;
        var rowBytes = buffer.RowBytes;
        var width = _image.PixelCountX;
        var height = _image.PixelCountY;
        var pixels = _image.Pixels;

        if (width * height >= ParallelPixelThreshold)
        {
            Parallel.For(0, height, y => WriteImageRow(basePtr + y * rowBytes, pixels, y, width));
        }
        else
        {
            for (var y = 0; y < height; y++)
                WriteImageRow(basePtr + y * rowBytes, pixels, y, width);
        }
    }

    private static unsafe void WriteImageRow(byte* rowPtr, Color[,] pixels, int y, int width)
    {
        for (var x = 0; x < width; x++)
        {
            var color = pixels[x, y];
            var pixel = rowPtr + x * 4;
            pixel[0] = color.B;
            pixel[1] = color.G;
            pixel[2] = color.R;
            pixel[3] = color.A;
        }
    }

    private static unsafe void WritePixel(ILockedFramebuffer buffer, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= buffer.Size.Width || y >= buffer.Size.Height)
            return;

        var pixel = (byte*)buffer.Address + y * buffer.RowBytes + x * 4;
        pixel[0] = color.B;
        pixel[1] = color.G;
        pixel[2] = color.R;
        pixel[3] = color.A;
    }

    private static void DrawTransparencyBackground(DrawingContext context, Rect bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        context.FillRectangle(TransparencyTileBrush, bounds);
    }

    private void DrawGridLines(DrawingContext context, Size contentSize)
    {
        if (_image is null)
            return;

        var visibleBounds = GetVisibleGridBounds(contentSize);

        for (var col = visibleBounds.firstCol; col <= visibleBounds.lastCol; col++)
        {
            var x = col * Zoom;
            context.DrawLine(GridPen,
                new Point(x, visibleBounds.firstRow * Zoom),
                new Point(x, visibleBounds.lastRow * Zoom));
        }

        for (var row = visibleBounds.firstRow; row <= visibleBounds.lastRow; row++)
        {
            var y = row * Zoom;
            context.DrawLine(GridPen,
                new Point(visibleBounds.firstCol * Zoom, y),
                new Point(visibleBounds.lastCol * Zoom, y));
        }
    }

    private (int firstCol, int lastCol, int firstRow, int lastRow) GetVisibleGridBounds(Size contentSize)
    {
        var viewportWidth = ScrollViewportSize.Width > 0 ? ScrollViewportSize.Width : contentSize.Width;
        var viewportHeight = ScrollViewportSize.Height > 0 ? ScrollViewportSize.Height : contentSize.Height;
        var scrollX = ScrollViewportOffset.X;
        var scrollY = ScrollViewportOffset.Y;

        var firstCol = Math.Max(0, (int)Math.Floor(scrollX / Zoom));
        var lastCol = Math.Min(_image!.PixelCountX, (int)Math.Ceiling((scrollX + viewportWidth) / Zoom));
        var firstRow = Math.Max(0, (int)Math.Floor(scrollY / Zoom));
        var lastRow = Math.Min(_image.PixelCountY, (int)Math.Ceiling((scrollY + viewportHeight) / Zoom));

        return (firstCol, lastCol, firstRow, lastRow);
    }

    private static IBrush CreateTransparencyTileBrush()
    {
        var tileSize = TransparencyTileSize;
        var patternSize = tileSize * 2;

        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing
        {
            Brush = TransparencyLightBrush,
            Geometry = new RectangleGeometry(new Rect(0, 0, patternSize, patternSize))
        });
        drawing.Children.Add(new GeometryDrawing
        {
            Brush = TransparencyDarkBrush,
            Geometry = new RectangleGeometry(new Rect(0, tileSize, tileSize, tileSize))
        });
        drawing.Children.Add(new GeometryDrawing
        {
            Brush = TransparencyDarkBrush,
            Geometry = new RectangleGeometry(new Rect(tileSize, 0, tileSize, tileSize))
        });

        return new DrawingBrush(drawing)
        {
            TileMode = TileMode.Tile,
            DestinationRect = new RelativeRect(0, 0, patternSize, patternSize, RelativeUnit.Absolute),
            SourceRect = new RelativeRect(0, 0, 1, 1, RelativeUnit.Relative)
        };
    }
}
