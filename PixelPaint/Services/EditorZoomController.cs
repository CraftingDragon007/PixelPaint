using System;
using Avalonia;

namespace PixelPaint.Services;

public sealed class EditorZoomController
{
    public const double MinimumZoom = 0.05d;
    public const double MaximumZoom = 64d;

    public double Clamp(double zoom) => Math.Clamp(zoom, MinimumZoom, MaximumZoom);

    public double CalculateFitZoom(Size viewportSize, int pixelWidth, int pixelHeight)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0)
            return 1d;
        if (viewportSize.Width <= 0 || viewportSize.Height <= 0)
            return 1d;

        var scaleX = viewportSize.Width / pixelWidth;
        var scaleY = viewportSize.Height / pixelHeight;
        return Clamp(Math.Min(scaleX, scaleY));
    }

    public ZoomAdjustment ZoomAtPoint(
        double currentZoom,
        double requestedZoom,
        Vector currentOffset,
        Point anchorPoint,
        Size viewportSize,
        Size imagePixelSize)
    {
        var safeCurrentZoom = Clamp(currentZoom);
        var newZoom = Clamp(requestedZoom);
        if (Math.Abs(newZoom - safeCurrentZoom) < double.Epsilon)
            return new ZoomAdjustment(newZoom, currentOffset);

        var contentX = (currentOffset.X + anchorPoint.X) / safeCurrentZoom;
        var contentY = (currentOffset.Y + anchorPoint.Y) / safeCurrentZoom;

        var desiredOffset = new Vector(
            contentX * newZoom - anchorPoint.X,
            contentY * newZoom - anchorPoint.Y);

        var maxOffset = new Vector(
            Math.Max(0, imagePixelSize.Width * newZoom - viewportSize.Width),
            Math.Max(0, imagePixelSize.Height * newZoom - viewportSize.Height));

        return new ZoomAdjustment(
            newZoom,
            new Vector(
                Math.Clamp(desiredOffset.X, 0, maxOffset.X),
                Math.Clamp(desiredOffset.Y, 0, maxOffset.Y)));
    }
}

public readonly record struct ZoomAdjustment(double Zoom, Vector Offset);

