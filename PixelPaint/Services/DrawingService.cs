using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media;
using Image = PixelPaint.Models.Image;

namespace PixelPaint.Services;

public interface IDrawingService
{
    Image? CurrentImage { get; }
    Color CurrentColor { get; set; }
    Tool CurrentTool { get; set; }
    int BrushSize { get; set; }
    bool CanUndo { get; }
    bool CanRedo { get; }

    void LoadImage(Image image);

    Color GetContrastColor(Color color);

    void BeginInteraction(int x, int y);
    void ContinueInteraction(int x, int y);
    void EndInteraction();
    void DrawEmptyImage();
    void NewImage(int pixelCountX, int pixelCountY);
    void Undo();
    void Redo();
    event Action<object?, Color> OtherColorChanged;
    event EventHandler<ImageChangedEventArgs> ImageChanged;
}

public readonly record struct PixelChange(int X, int Y, Color Color);

public sealed class ImageChangedEventArgs(Image image, IReadOnlyList<PixelChange> pixelChanges, bool requiresFullRefresh) : EventArgs
{
    public Image Image { get; } = image;
    public IReadOnlyList<PixelChange> PixelChanges { get; } = pixelChanges;
    public bool RequiresFullRefresh { get; } = requiresFullRefresh;
}

public enum Tool
{
    Brush,
    Fill,
    Pipette
}

public class DrawingService : IDrawingService
{
    private const int DefaultPixelCountX = 1280;
    private const int DefaultPixelCountY = 720;
    private const int MaxBrushSize = 256;

    private Image? _image;
    private StrokeSession? _currentStroke;
    private readonly Stack<PixelEditOperation> _undoStack = new();
    private readonly Stack<PixelEditOperation> _redoStack = new();
    private int _brushSize = 10;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public Image? CurrentImage => _image;

    public int BrushSize
    {
        get => _brushSize;
        set => _brushSize = Math.Clamp(value, 1, MaxBrushSize);
    }

    public Color GetContrastColor(Color color)
    {
        // Calculate the luminance of the color
        var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;

        // If the luminance is high, return a dark color, otherwise return a light color
        return luminance > 0.5 ? Colors.Black : Colors.White;
    }

    public void DrawEmptyImage()
    {
        LoadImage(CreateImage(DefaultPixelCountX, DefaultPixelCountY));
    }

    public void NewImage(int pixelCountX, int pixelCountY)
    {
        if (pixelCountX <= 0 || pixelCountY <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelCountX), "Image dimensions must be positive");

        LoadImage(CreateImage(pixelCountX, pixelCountY));
    }

    public void Undo()
    {
        EndInteraction();
        if (!CanUndo || _image is null)
            return;

        var operation = _undoStack.Pop();
        var changes = ApplyOperation(operation, undo: true);
        _redoStack.Push(operation);
        RaiseImageChanged(changes, requiresFullRefresh: false);
    }

    public void Redo()
    {
        EndInteraction();
        if (!CanRedo || _image is null)
            return;

        var operation = _redoStack.Pop();
        var changes = ApplyOperation(operation, undo: false);
        _undoStack.Push(operation);
        RaiseImageChanged(changes, requiresFullRefresh: false);
    }

    public void LoadImage(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        EndInteraction();
        _image = image;
        _undoStack.Clear();
        _redoStack.Clear();
        RaiseImageChanged([], requiresFullRefresh: true);
    }

    public void BeginInteraction(int x, int y)
    {
        if (!TryCreateCoordinate(x, y, out var pixel))
            return;

        switch (CurrentTool)
        {
            case Tool.Brush:
                StartBrushStroke(pixel);
                break;
            case Tool.Fill:
                ApplyFill(pixel);
                break;
            case Tool.Pipette:
                ApplyPipette(pixel);
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(CurrentTool), (int)CurrentTool, typeof(Tool));
        }
    }

    public void ContinueInteraction(int x, int y)
    {
        if (_currentStroke is null || _image is null || CurrentTool != Tool.Brush)
            return;
        if (!TryCreateCoordinate(x, y, out var pixel))
            return;
        if (pixel == _currentStroke.LastPoint)
            return;

        var changes = ApplyBrushLine(_currentStroke.LastPoint, pixel, _currentStroke);
        _currentStroke.LastPoint = pixel;
        if (changes.Count > 0)
            RaiseImageChanged(changes, requiresFullRefresh: false);
    }

    public void EndInteraction()
    {
        if (_currentStroke is null || _image is null)
            return;

        var operation = _currentStroke.CreateOperation(_image);
        _currentStroke = null;
        PushOperation(operation);
    }

    private static Image CreateImage(int pixelCountX, int pixelCountY)
    {
        var image = new Image
        {
            PixelCountX = pixelCountX,
            PixelCountY = pixelCountY,
            PixelCount = pixelCountX * pixelCountY,
            Pixels = new Color[pixelCountX, pixelCountY]
        };

        for (var y = 0; y < pixelCountY; y++)
        for (var x = 0; x < pixelCountX; x++)
            image.Pixels[x, y] = Colors.White;

        return image;
    }

    private void StartBrushStroke(PixelCoordinate startPixel)
    {
        _currentStroke = new StrokeSession(startPixel);
        var changes = ApplyBrushStamp(startPixel, _currentStroke);
        if (changes.Count > 0)
            RaiseImageChanged(changes, requiresFullRefresh: false);
    }

    private void ApplyFill(PixelCoordinate startPixel)
    {
        if (_image is null)
            return;

        var targetColor = _image.Pixels[startPixel.X, startPixel.Y];
        if (targetColor == CurrentColor)
            return;

        var changes = new List<PixelChange>();
        var originalColors = new Dictionary<int, Color>();
        var pendingPixels = new Queue<PixelCoordinate>();
        pendingPixels.Enqueue(startPixel);

        while (pendingPixels.Count > 0)
        {
            var current = pendingPixels.Dequeue();
            if (!IsInBounds(current))
                continue;
            if (_image.Pixels[current.X, current.Y] != targetColor)
                continue;

            var pixelIndex = GetPixelIndex(current.X, current.Y);
            originalColors.TryAdd(pixelIndex, targetColor);
            _image.Pixels[current.X, current.Y] = CurrentColor;
            changes.Add(new PixelChange(current.X, current.Y, CurrentColor));

            pendingPixels.Enqueue(current with { X = current.X + 1 });
            pendingPixels.Enqueue(current with { X = current.X - 1 });
            pendingPixels.Enqueue(current with { Y = current.Y + 1 });
            pendingPixels.Enqueue(current with { Y = current.Y - 1 });
        }

        PushOperation(CreateOperationFromOriginals(originalColors));
        if (changes.Count > 0)
            RaiseImageChanged(changes, requiresFullRefresh: false);
    }

    private void ApplyPipette(PixelCoordinate pixel)
    {
        if (_image is null)
            return;

        CurrentColor = _image.Pixels[pixel.X, pixel.Y];
        OtherColorChanged?.Invoke(this, CurrentColor);
    }

    private List<PixelChange> ApplyBrushLine(PixelCoordinate from, PixelCoordinate to, StrokeSession stroke)
    {
        var changes = new List<PixelChange>();
        foreach (var pixel in EnumerateLine(from, to))
            changes.AddRange(ApplyBrushStamp(pixel, stroke));

        return changes;
    }

    private List<PixelChange> ApplyBrushStamp(PixelCoordinate center, StrokeSession stroke)
    {
        var changes = new List<PixelChange>();
        if (_image is null)
            return changes;

        var offsetStart = -BrushSize / 2;
        var offsetEnd = BrushSize - 1 + offsetStart;

        for (var yOffset = offsetStart; yOffset <= offsetEnd; yOffset++)
        for (var xOffset = offsetStart; xOffset <= offsetEnd; xOffset++)
        {
            if (!IsInsideBrush(xOffset, yOffset))
                continue;

            var x = center.X + xOffset;
            var y = center.Y + yOffset;
            if (!IsInBounds(x, y))
                continue;

            TryPaintPixel(x, y, CurrentColor, stroke, changes);
        }

        return changes;
    }

    private bool TryPaintPixel(int x, int y, Color color, StrokeSession stroke, ICollection<PixelChange> changes)
    {
        if (_image is null)
            return false;

        var currentColor = _image.Pixels[x, y];
        if (currentColor == color)
            return false;

        stroke.RememberOriginalColor(GetPixelIndex(x, y), currentColor);
        _image.Pixels[x, y] = color;
        changes.Add(new PixelChange(x, y, color));
        return true;
    }

    private List<PixelChange> ApplyOperation(PixelEditOperation operation, bool undo)
    {
        var changes = new List<PixelChange>(operation.Changes.Count);
        if (_image is null)
            return changes;

        foreach (var change in operation.Changes)
        {
            var color = undo ? change.Before : change.After;
            _image.Pixels[change.X, change.Y] = color;
            changes.Add(new PixelChange(change.X, change.Y, color));
        }

        return changes;
    }

    private void PushOperation(PixelEditOperation operation)
    {
        if (operation.Changes.Count == 0)
            return;

        _undoStack.Push(operation);
        _redoStack.Clear();
    }

    private PixelEditOperation CreateOperationFromOriginals(IReadOnlyDictionary<int, Color> originalColors)
    {
        if (_image is null || originalColors.Count == 0)
            return PixelEditOperation.Empty;

        var changes = new List<PixelStateChange>(originalColors.Count);
        foreach (var (pixelIndex, originalColor) in originalColors)
        {
            var x = pixelIndex % _image.PixelCountX;
            var y = pixelIndex / _image.PixelCountX;
            var newColor = _image.Pixels[x, y];
            if (newColor == originalColor)
                continue;

            changes.Add(new PixelStateChange(x, y, originalColor, newColor));
        }

        return changes.Count == 0 ? PixelEditOperation.Empty : new PixelEditOperation(changes);
    }

    private bool TryCreateCoordinate(int x, int y, out PixelCoordinate pixel)
    {
        pixel = new PixelCoordinate(x, y);
        return _image is not null && IsInBounds(pixel);
    }

    private bool IsInBounds(PixelCoordinate pixel) => IsInBounds(pixel.X, pixel.Y);

    private bool IsInBounds(int x, int y) =>
        _image is not null &&
        x >= 0 && x < _image.PixelCountX &&
        y >= 0 && y < _image.PixelCountY;

    private int GetPixelIndex(int x, int y)
    {
        if (_image is null)
            throw new InvalidOperationException("No image is loaded.");

        return y * _image.PixelCountX + x;
    }

    private static IEnumerable<PixelCoordinate> EnumerateLine(PixelCoordinate start, PixelCoordinate end)
    {
        var x0 = start.X;
        var y0 = start.Y;
        var x1 = end.X;
        var y1 = end.Y;
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            yield return new PixelCoordinate(x0, y0);
            if (x0 == x1 && y0 == y1)
                yield break;

            var doubledError = error * 2;
            if (doubledError >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (doubledError <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    private bool IsInsideBrush(int xOffset, int yOffset)
    {
        if (BrushSize <= 1)
            return xOffset == 0 && yOffset == 0;

        var radius = BrushSize / 2d;
        var centerOffset = BrushSize % 2 == 0 ? 0.5 : 0d;
        var normalizedX = xOffset + centerOffset;
        var normalizedY = yOffset + centerOffset;
        return normalizedX * normalizedX + normalizedY * normalizedY <= radius * radius;
    }

    private void RaiseImageChanged(IReadOnlyList<PixelChange> changes, bool requiresFullRefresh)
    {
        if (_image is null)
            return;

        ImageChanged?.Invoke(this, new ImageChangedEventArgs(_image, changes, requiresFullRefresh));
    }

    public event Action<object?, Color>? OtherColorChanged;
    public event EventHandler<ImageChangedEventArgs>? ImageChanged;
    public Tool CurrentTool { get; set; } = Tool.Brush;
    public Color CurrentColor { get; set; } = Colors.Black;

    private readonly record struct PixelCoordinate(int X, int Y);

    private readonly record struct PixelStateChange(int X, int Y, Color Before, Color After);

    private sealed record PixelEditOperation(IReadOnlyList<PixelStateChange> Changes)
    {
        public static PixelEditOperation Empty { get; } = new([]);
    }

    private sealed class StrokeSession(PixelCoordinate startPixel)
    {
        private readonly Dictionary<int, Color> _originalColors = new();

        public PixelCoordinate LastPoint { get; set; } = startPixel;

        public void RememberOriginalColor(int pixelIndex, Color color) => _originalColors.TryAdd(pixelIndex, color);

        public PixelEditOperation CreateOperation(Image image)
        {
            if (_originalColors.Count == 0)
                return PixelEditOperation.Empty;

            var changes = new List<PixelStateChange>(_originalColors.Count);
            foreach (var (pixelIndex, originalColor) in _originalColors)
            {
                var x = pixelIndex % image.PixelCountX;
                var y = pixelIndex / image.PixelCountX;
                var currentColor = image.Pixels[x, y];
                if (currentColor == originalColor)
                    continue;

                changes.Add(new PixelStateChange(x, y, originalColor, currentColor));
            }

            return changes.Count == 0 ? PixelEditOperation.Empty : new PixelEditOperation(changes);
        }
    }
}