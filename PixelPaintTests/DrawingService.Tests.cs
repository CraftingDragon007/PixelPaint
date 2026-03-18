using Avalonia.Media;
using PixelPaint.Models;
using PixelPaint.Services;

namespace PixelPaintTests;

public class DrawingServiceTests
{
    private DrawingService _drawingService = null!;

    [SetUp]
    public void Setup()
    {
        _drawingService = new DrawingService();
        _drawingService.NewImage(12, 12);
    }

    [Test]
    public void DrawEmptyImage_Uses720pDefaults()
    {
        _drawingService.DrawEmptyImage();

        var image = _drawingService.CurrentImage;
        Assert.That(image, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(image!.PixelCountX, Is.EqualTo(1280));
            Assert.That(image.PixelCountY, Is.EqualTo(720));
            Assert.That(image.PixelCount, Is.EqualTo(1280 * 720));
        }
    }

    [Test]
    public void NewDrawingService_UsesBrushSizeTenByDefault()
    {
        Assert.That(_drawingService.BrushSize, Is.EqualTo(10));
    }

    [Test]
    public void BrushStroke_WithLargerBrush_PaintsArea_AndUndoRedoWorksAsSingleOperation()
    {
        _drawingService.CurrentTool = Tool.Brush;
        _drawingService.CurrentColor = Colors.Red;
        _drawingService.BrushSize = 3;

        _drawingService.BeginInteraction(5, 5);
        _drawingService.ContinueInteraction(7, 5);
        _drawingService.EndInteraction();

        var image = _drawingService.CurrentImage!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(image.Pixels[5, 5], Is.EqualTo(Colors.Red));
            Assert.That(image.Pixels[6, 5], Is.EqualTo(Colors.Red));
            Assert.That(image.Pixels[7, 5], Is.EqualTo(Colors.Red));
            Assert.That(image.Pixels[6, 4], Is.EqualTo(Colors.Red));
            Assert.That(_drawingService.CanUndo, Is.True);
            Assert.That(_drawingService.CanRedo, Is.False);
        }

        _drawingService.Undo();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(image.Pixels[5, 5], Is.EqualTo(Colors.White));
            Assert.That(image.Pixels[6, 5], Is.EqualTo(Colors.White));
            Assert.That(image.Pixels[7, 5], Is.EqualTo(Colors.White));
            Assert.That(_drawingService.CanRedo, Is.True);
        }

        _drawingService.Redo();
        Assert.That(image.Pixels[6, 5], Is.EqualTo(Colors.Red));
    }

    [Test]
    public void Fill_FillsOnlyConnectedPixels()
    {
        var image = CreateImage(5, 5, (_, _) => Colors.White);
        image.Pixels[2, 0] = Colors.Black;
        image.Pixels[2, 1] = Colors.Black;
        image.Pixels[2, 2] = Colors.Black;
        image.Pixels[2, 3] = Colors.Black;
        image.Pixels[2, 4] = Colors.Black;
        _drawingService.LoadImage(image);

        _drawingService.CurrentTool = Tool.Fill;
        _drawingService.CurrentColor = Colors.Blue;
        _drawingService.BeginInteraction(0, 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(image.Pixels[0, 0], Is.EqualTo(Colors.Blue));
            Assert.That(image.Pixels[1, 4], Is.EqualTo(Colors.Blue));
            Assert.That(image.Pixels[3, 0], Is.EqualTo(Colors.White));
            Assert.That(image.Pixels[2, 2], Is.EqualTo(Colors.Black));
            Assert.That(_drawingService.CanUndo, Is.True);
        }
    }

    [Test]
    public void Pipette_SetsCurrentColor_AndRaisesNotification()
    {
        var image = CreateImage(2, 2, (_, _) => Colors.White);
        image.Pixels[1, 1] = Colors.Green;
        _drawingService.LoadImage(image);
        _drawingService.CurrentTool = Tool.Pipette;

        Color? observedColor = null;
        _drawingService.OtherColorChanged += (_, color) => observedColor = color;

        _drawingService.BeginInteraction(1, 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(_drawingService.CurrentColor, Is.EqualTo(Colors.Green));
            Assert.That(observedColor, Is.EqualTo(Colors.Green));
            Assert.That(_drawingService.CanUndo, Is.False);
        }
    }

    [Test]
    public void LargeBrush_AtImageEdge_IsClampedToBounds()
    {
        _drawingService.CurrentTool = Tool.Brush;
        _drawingService.CurrentColor = Colors.Black;
        _drawingService.BrushSize = 6;

        _drawingService.BeginInteraction(0, 0);
        _drawingService.EndInteraction();

        var image = _drawingService.CurrentImage!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(image.Pixels[0, 0], Is.EqualTo(Colors.Black));
            Assert.That(image.Pixels[1, 0], Is.EqualTo(Colors.Black));
            Assert.That(image.Pixels[0, 1], Is.EqualTo(Colors.Black));
            Assert.That(image.Pixels[11, 11], Is.EqualTo(Colors.White));
        }
    }

    private static Image CreateImage(int width, int height, Func<int, int, Color> colorFactory)
    {
        var image = new Image
        {
            PixelCountX = width,
            PixelCountY = height,
            PixelCount = width * height,
            Pixels = new Color[width, height]
        };

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            image.Pixels[x, y] = colorFactory(x, y);

        return image;
    }
}

