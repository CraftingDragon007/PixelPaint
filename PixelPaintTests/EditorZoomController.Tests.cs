using Avalonia;
using PixelPaint.Services;

namespace PixelPaintTests;

public class EditorZoomControllerTests
{
    private readonly EditorZoomController _controller = new();

    [Test]
    public void Clamp_RestrictsZoomToConfiguredRange()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_controller.Clamp(0.001), Is.EqualTo(EditorZoomController.MinimumZoom));
            Assert.That(_controller.Clamp(200), Is.EqualTo(EditorZoomController.MaximumZoom));
            Assert.That(_controller.Clamp(2.5), Is.EqualTo(2.5));
        }
    }

    [Test]
    public void CalculateFitZoom_ReturnsSmallerViewportRatio()
    {
        var zoom = _controller.CalculateFitZoom(new Size(960, 540), 1920, 1080);

        Assert.That(zoom, Is.EqualTo(0.5).Within(0.0001));
    }

    [Test]
    public void ZoomAtPoint_PreservesContentUnderAnchor()
    {
        const double currentZoom = 8;
        var currentOffset = new Vector(120, 60);
        var anchor = new Point(150, 90);
        var viewport = new Size(640, 480);
        var imageSize = new Size(512, 256);

        var adjustment = _controller.ZoomAtPoint(currentZoom, 16, currentOffset, anchor, viewport, imageSize);

        var contentXBefore = (currentOffset.X + anchor.X) / currentZoom;
        var contentYBefore = (currentOffset.Y + anchor.Y) / currentZoom;
        var contentXAfter = (adjustment.Offset.X + anchor.X) / adjustment.Zoom;
        var contentYAfter = (adjustment.Offset.Y + anchor.Y) / adjustment.Zoom;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adjustment.Zoom, Is.EqualTo(16).Within(0.0001));
            Assert.That(contentXAfter, Is.EqualTo(contentXBefore).Within(0.0001));
            Assert.That(contentYAfter, Is.EqualTo(contentYBefore).Within(0.0001));
        }
    }

    [Test]
    public void ZoomAtPoint_ClampsOffsetWhenContentBecomesSmallerThanViewport()
    {
        var adjustment = _controller.ZoomAtPoint(
            currentZoom: 2,
            requestedZoom: 0.1,
            currentOffset: new Vector(300, 200),
            anchorPoint: new Point(100, 50),
            viewportSize: new Size(1000, 800),
            imagePixelSize: new Size(100, 100));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(adjustment.Zoom, Is.EqualTo(0.1).Within(0.0001));
            Assert.That(adjustment.Offset, Is.EqualTo(new Vector(0, 0)));
        }
    }
}

