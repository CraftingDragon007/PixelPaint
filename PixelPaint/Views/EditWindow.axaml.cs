using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using MsBox.Avalonia;
using PixelPaint.Services;
using Image = PixelPaint.Models.Image;

namespace PixelPaint.Views;

public partial class EditWindow : Window
{
    private readonly IDrawingService _drawingService;
    private readonly IFileService _fileService;
    private readonly EditorZoomController _zoomController;
    private string? _currentFilePath;
    private bool _isDrawing;
    private bool _fitZoomAfterRefresh = true;
    private double? _pinchStartZoom;

    public EditWindow() : this(new DrawingService(), new FileService(), new EditorZoomController())
    {
    }

    public EditWindow(IDrawingService drawingService, IFileService fileService, EditorZoomController zoomController)
    {
        InitializeComponent();
        _drawingService = drawingService;
        _fileService = fileService;
        _zoomController = zoomController;

        var random = new Random();
        var buffer = new byte[3];
        random.NextBytes(buffer);
        var color = new SolidColorBrush(Color.FromRgb(buffer[0], buffer[1], buffer[2]));
        var contrastColor = new SolidColorBrush(_drawingService.GetContrastColor(color.Color));
        OtherColorRadioButton.Background = color;
        OtherColorRadioButton.Foreground = contrastColor;
        RegisterEvents();
        _drawingService.DrawEmptyImage();
    }

    private void RegisterEvents()
    {
        Opened += (_, _) => ScheduleZoomToFit();
        KeyDown += EditWindowOnKeyDown;

        PixelCanvas.PointerPressed += PixelCanvasOnPointerPressed;
        PixelCanvas.PointerMoved += PixelCanvasOnPointerMoved;
        PixelCanvas.PointerReleased += PixelCanvasOnPointerReleased;
        PixelCanvas.PointerCaptureLost += PixelCanvasOnPointerCaptureLost;
        EditorScrollViewer.AddHandler(
            InputElement.PointerWheelChangedEvent,
            EditorScrollViewerOnPointerWheelChanged,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        EditorScrollViewer.AddHandler(Gestures.PinchEvent, OnPinchGesture);
        EditorScrollViewer.AddHandler(Gestures.PinchEndedEvent, OnPinchGestureEnded);
        EditorScrollViewer.ScrollChanged += (_, _) => SyncCanvasViewport();

        ShowGridLinesCheckBox.IsCheckedChanged += ShowGridLinesCheckBoxOnIsCheckedChanged;
        BrushSizeSlider.ValueChanged += BrushSizeSliderOnValueChanged;
        OpenMenuItem.Click += OpenMenuItemOnClick;
        ImportMenuItem.Click += ImportMenuItemOnClick;
        SaveMenuItem.Click += SaveMenuItemOnClick;
        SaveAsMenuItem.Click += SaveAsMenuItemOnClick;
        ResetMenuItem.Click += ResetMenuItemOnClick;
        PixelSizeMenuItem.Click += PixelSizeMenuItemOnClick;
        ExportMenuItem.Click += ExportMenuItemOnClick;
        UndoMenuItem.Click += UndoMenuItemOnClick;
        RedoMenuItem.Click += RedoMenuItemOnClick;
        BrowseColorsButton.Click += BrowseColorsButtonOnClick;
        ZoomInButton.Click += (_, _) => ZoomByFactor(1.25d);
        ZoomOutButton.Click += (_, _) => ZoomByFactor(1 / 1.25d);
        ZoomResetButton.Click += (_, _) => ApplyZoom(1d);
        ZoomFitButton.Click += (_, _) => ZoomToFit();

        _drawingService.ImageChanged += DrawingServiceOnImageChanged;
        _drawingService.OtherColorChanged += (sender, color) =>
        {
            OtherColorRadioButton.Background = new SolidColorBrush(color);
            OtherColorRadioButton.Foreground = new SolidColorBrush(_drawingService.GetContrastColor(color));
            OtherColorRadioButton.IsChecked = true;
        };

        BrushSizeSlider.Value = _drawingService.BrushSize;
        UpdateBrushSizeUi(_drawingService.BrushSize);
        PixelCanvas.ShowGridLines = ShowGridLinesCheckBox.IsChecked ?? true;
        UpdateCommandState();
        UpdateZoomUi();
    }

    private void EditWindowOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        // Support both common redo shortcuts.
        if ((e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Shift)) || e.Key == Key.Y)
        {
            if (_drawingService.CanRedo)
            {
                _drawingService.Redo();
                UpdateCommandState();
            }

            e.Handled = true;
            return;
        }

        if (e.Key != Key.Z)
            return;

        if (_drawingService.CanUndo)
        {
            _drawingService.Undo();
            UpdateCommandState();
        }

        e.Handled = true;
    }

    // ── Save ────────────────────────────────────────────────────────────────

    private async void SaveMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        _drawingService.EndInteraction();
        var image = _drawingService.CurrentImage;
        if (image is null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Fehler", "Kein Bild zum Speichern").ShowAsPopupAsync(this);
            return;
        }

        if (_currentFilePath is not null)
        {
            if (!await ConfirmLargeSvgSaveAsync(image, _currentFilePath))
                return;

            _fileService.SaveImage(image, _currentFilePath, GetEditorSize());
            return;
        }

        await DoSaveAs();
    }

    private async void SaveAsMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        await DoSaveAs();
    }

    private async Task DoSaveAs()
    {
        _drawingService.EndInteraction();
        var image = _drawingService.CurrentImage;
        if (image is null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Fehler", "Kein Bild zum Speichern").ShowAsPopupAsync(this);
            return;
        }

        var dialogResult = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "axp",
            FileTypeChoices = _fileService.FileTypeFilter,
            Title = "Bild speichern"
        });
        if (dialogResult is null) return;
        var localPath = dialogResult.TryGetLocalPath() ?? dialogResult.Path.ToString();

        if (!await ConfirmLargeSvgSaveAsync(image, localPath))
            return;

        _fileService.SaveImage(image, localPath, GetEditorSize());
        _currentFilePath = localPath;
    }

    private (uint width, uint height) GetEditorSize()
    {
        var viewport = GetViewportSize();
        return ((uint)Math.Max(1, Math.Round(viewport.Width)),
                (uint)Math.Max(1, Math.Round(viewport.Height)));
    }

    // ── Reset ───────────────────────────────────────────────────────────────

    private async void ResetMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(
            "Zurücksetzen",
            "Möchtest du das Bild wirklich zurücksetzen? Alle Änderungen gehen verloren.",
            MsBox.Avalonia.Enums.ButtonEnum.YesNo);
        var result = await box.ShowAsPopupAsync(this);
        if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
        {
            _fitZoomAfterRefresh = true;
            _drawingService.DrawEmptyImage();
            _currentFilePath = null;
        }
    }

    // ── Pixel Size ──────────────────────────────────────────────────────────

    private async void PixelSizeMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        var currentImage = _drawingService.CurrentImage;
        var currentCols = currentImage?.PixelCountX ?? 32;
        var currentRows = currentImage?.PixelCountY ?? 16;

        var dialog = new PixelSizeDialog(currentCols, currentRows);
        var result = await dialog.ShowDialog<(int width, int height)?>(this);
        if (result is null) return;

        _fitZoomAfterRefresh = true;
        _drawingService.NewImage(result.Value.width, result.Value.height);
        _currentFilePath = null;
    }

    // ── Export ──────────────────────────────────────────────────────────────

    private async void ExportMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        _drawingService.EndInteraction();
        var image = _drawingService.CurrentImage;
        if (image is null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Fehler", "Kein Bild zum Exportieren").ShowAsPopupAsync(this);
            return;
        }

        var dialogResult = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "png",
            FileTypeChoices =
            [
                new FilePickerFileType("PNG-Bild") { Patterns = ["*.png"] },
                new FilePickerFileType("SVG-Bild")  { Patterns = ["*.svg"], MimeTypes = ["image/svg+xml"] },
                new FilePickerFileType("BMP-Bild") { Patterns = ["*.bmp"] }
            ],
            Title = "Bild exportieren"
        });
        if (dialogResult is null) return;

        var path = dialogResult.TryGetLocalPath() ?? dialogResult.Path.ToString();

        if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            if (!await ConfirmLargeSvgSaveAsync(image, path))
                return;

            _fileService.SaveImage(image, path, GetEditorSize());
        }
        else
        {
            ExportToBitmap(image, path);
        }
    }

    private static void ExportToBitmap(Image image, string path)
    {
        using var bitmap = new WriteableBitmap(
            new PixelSize(image.PixelCountX, image.PixelCountY),
            new Vector(96, 96),
            PixelFormats.Bgra8888,
            AlphaFormat.Unpremul);

        using (var buf = bitmap.Lock())
        {
            for (var y = 0; y < image.PixelCountY; y++)
            for (var x = 0; x < image.PixelCountX; x++)
            {
                var color = image.Pixels[x, y];
                var offset = y * buf.RowBytes + x * 4;
                Marshal.WriteByte(buf.Address, offset,     color.B);
                Marshal.WriteByte(buf.Address, offset + 1, color.G);
                Marshal.WriteByte(buf.Address, offset + 2, color.R);
                Marshal.WriteByte(buf.Address, offset + 3, color.A);
            }
        }

        bitmap.Save(path);
    }

    private async Task<bool> ConfirmLargeSvgSaveAsync(Image image, string path)
    {
        if (!path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            return true;

        var (estimatedSizeBytes, shouldWarn) = _fileService.GetSvgSaveWarning(image);
        if (!shouldWarn)
            return true;

        var estimatedSizeMb = estimatedSizeBytes / (1024d * 1024d);
        var dialog = MessageBoxManager.GetMessageBoxStandard(
            "Grosse SVG-Datei",
            $"Dieses Bild wird als SVG voraussichtlich etwa {estimatedSizeMb:0.0} MB gross.\n" +
            "Das Speichern kann länger dauern und die Datei kann unhandlich werden.\n\n" +
            "Trotzdem als SVG speichern?",
            MsBox.Avalonia.Enums.ButtonEnum.YesNo);

        var result = await dialog.ShowAsPopupAsync(this);
        return result == MsBox.Avalonia.Enums.ButtonResult.Yes;
    }

    // ── Undo / Redo ─────────────────────────────────────────────────────────

    private void UndoMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        _drawingService.Undo();
        UpdateCommandState();
    }

    private void RedoMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        _drawingService.Redo();
        UpdateCommandState();
    }

    // ── Existing handlers ───────────────────────────────────────────────────

    private async void BrowseColorsButtonOnClick(object? sender, RoutedEventArgs e)
    {
        var colorDialog = new ColorDialog(_drawingService.CurrentColor);
        var color = await colorDialog.ShowDialog<Color?>(this);
        if (!color.HasValue) return;
        var contrastColor = _drawingService.GetContrastColor(color.Value);
        OtherColorRadioButton.Background = new SolidColorBrush(color.Value);
        OtherColorRadioButton.Foreground = new SolidColorBrush(contrastColor);
        if (OtherColorRadioButton.IsChecked ?? false) _drawingService.CurrentColor = color.Value;
    }

    private void ShowGridLinesCheckBoxOnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
            PixelCanvas.ShowGridLines = checkBox.IsChecked ?? false;
    }

    private async void OpenMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        var dialogResult = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            { FileTypeFilter = _fileService.FileTypeFilter, AllowMultiple = false, Title = "Bild öffnen" });
        if (dialogResult.Count == 0) return;
        var file = dialogResult[0];

        var localPath = file.TryGetLocalPath() ?? file.Path.ToString();
        var (image, editorSize) = _fileService.LoadImage(localPath);
        RestoreWindowSize(editorSize);
        _fitZoomAfterRefresh = true;
        _drawingService.LoadImage(image);
        _currentFilePath = localPath;
    }

    private async void ImportMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        var dialogResult = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            FileTypeFilter = _fileService.ImportFileTypeFilter,
            AllowMultiple = false,
            Title = "Bild importieren (PNG, BMP)"
        });

        if (dialogResult.Count == 0) return;

        var file = dialogResult[0];
        var localPath = file.TryGetLocalPath() ?? file.Path.ToString();

        try
        {
            var image = _fileService.ImportImage(localPath);
            _fitZoomAfterRefresh = true;
            _drawingService.LoadImage(image);
            _currentFilePath = null;
        }
        catch (Exception ex)
        {
            await MessageBoxManager
                .GetMessageBoxStandard("Fehler beim Importieren", ex.Message)
                .ShowAsPopupAsync(this);
        }
    }

    private void OnColorRadioButtonIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { IsChecked: true } radioButton) return;
        var color = (ISolidColorBrush?)radioButton.Background;
        if (color is null) return;
        _drawingService.CurrentColor = color.Color;
    }

    private void OnBrushToolRadioButtonIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { IsChecked: true }) return;
        _drawingService.CurrentTool = Tool.Brush;
    }

    private void OnFillToolRadioButtonIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { IsChecked: true }) return;
        _drawingService.CurrentTool = Tool.Fill;
    }

    private void OnPipetteToolRadioButtonIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { IsChecked: true }) return;
        _drawingService.CurrentTool = Tool.Pipette;
    }

    private void DrawingServiceOnImageChanged(object? sender, ImageChangedEventArgs e)
    {
        if (e.RequiresFullRefresh || !ReferenceEquals(PixelCanvas.CurrentImage, e.Image))
            PixelCanvas.SetImage(e.Image);
        else
            PixelCanvas.ApplyPixelChanges(e.PixelChanges);

        UpdateImageSizeUi(e.Image);
        ClampBrushSizeToImage(e.Image);
        UpdateCommandState();

        if (_fitZoomAfterRefresh)
        {
            _fitZoomAfterRefresh = false;
            ScheduleZoomToFit();
        }
        else
        {
            UpdateZoomUi();
        }
    }

    private void PixelCanvasOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(PixelCanvas).Properties.IsLeftButtonPressed)
            return;

        var pixel = PixelCanvas.TryGetPixel(e.GetPosition(PixelCanvas));
        if (pixel is null)
            return;

        _isDrawing = true;
        e.Pointer.Capture(PixelCanvas);
        _drawingService.BeginInteraction(pixel.Value.x, pixel.Value.y);
        UpdateCommandState();
        e.Handled = true;
    }

    private void PixelCanvasOnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDrawing)
            return;

        var pixel = PixelCanvas.TryGetPixel(e.GetPosition(PixelCanvas));
        if (pixel is null)
            return;

        _drawingService.ContinueInteraction(pixel.Value.x, pixel.Value.y);
        e.Handled = true;
    }

    private void PixelCanvasOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDrawing)
            return;

        FinishDrawingInteraction(e.Pointer);
        e.Handled = true;
    }

    private void PixelCanvasOnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        FinishDrawingInteraction(e.Pointer);
    }

    private void FinishDrawingInteraction(IPointer? pointer)
    {
        if (!_isDrawing)
            return;

        _isDrawing = false;
        _drawingService.EndInteraction();
        if (pointer is not null)
            pointer.Capture(null);

        UpdateCommandState();
    }

    private void EditorScrollViewerOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        var zoomFactor = Math.Pow(1.2d, e.Delta.Y);
        ApplyZoom(PixelCanvas.Zoom * zoomFactor, GetZoomAnchorPoint(e));
        e.Handled = true;
    }

    private Point GetZoomAnchorPoint(PointerEventArgs e)
    {
        var canvasPosition = e.GetPosition(PixelCanvas);
        var canvasBounds = PixelCanvas.Bounds;

        if (canvasPosition.X >= 0 && canvasPosition.Y >= 0 &&
            canvasPosition.X <= canvasBounds.Width && canvasPosition.Y <= canvasBounds.Height)
        {
            return new Point(
                canvasPosition.X - EditorScrollViewer.Offset.X,
                canvasPosition.Y - EditorScrollViewer.Offset.Y);
        }

        return e.GetPosition(EditorScrollViewer);
    }

    private void OnPinchGesture(object? sender, PinchEventArgs e)
    {
        _pinchStartZoom ??= PixelCanvas.Zoom;
        ApplyZoom(_pinchStartZoom.Value * e.Scale, e.ScaleOrigin);
        e.Handled = true;
    }

    private void OnPinchGestureEnded(object? sender, PinchEndedEventArgs e)
    {
        _pinchStartZoom = null;
        e.Handled = true;
    }

    private void BrushSizeSliderOnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        var brushSize = Math.Max(1, (int)Math.Round(e.NewValue));
        _drawingService.BrushSize = brushSize;
        UpdateBrushSizeUi(_drawingService.BrushSize);
    }

    private void ZoomByFactor(double factor)
    {
        ApplyZoom(PixelCanvas.Zoom * factor);
    }

    private void ApplyZoom(double requestedZoom, Point? anchorPoint = null)
    {
        var image = _drawingService.CurrentImage;
        if (image is null)
            return;

        var viewport = GetViewportSize();
        if (viewport.Width <= 0 || viewport.Height <= 0)
            return;

        var anchor = anchorPoint ?? new Point(viewport.Width / 2, viewport.Height / 2);
        var adjustment = _zoomController.ZoomAtPoint(
            PixelCanvas.Zoom,
            requestedZoom,
            EditorScrollViewer.Offset,
            anchor,
            viewport,
            new Size(image.PixelCountX, image.PixelCountY));

        PixelCanvas.Zoom = adjustment.Zoom;
        EditorScrollViewer.Offset = adjustment.Offset;
        UpdateZoomUi();
    }

    private void ZoomToFit()
    {
        var image = _drawingService.CurrentImage;
        if (image is null)
            return;

        var viewport = GetViewportSize();
        if (viewport.Width <= 0 || viewport.Height <= 0)
            return;

        PixelCanvas.Zoom = _zoomController.CalculateFitZoom(viewport, image.PixelCountX, image.PixelCountY);
        EditorScrollViewer.Offset = default;
        UpdateZoomUi();
    }

    private void ScheduleZoomToFit()
    {
        Dispatcher.UIThread.Post(ZoomToFit, DispatcherPriority.Loaded);
    }

    private Size GetViewportSize()
    {
        var viewport = EditorScrollViewer.Viewport;
        if (viewport.Width > 0 && viewport.Height > 0)
            return viewport;

        return EditorScrollViewer.Bounds.Size;
    }

    private void UpdateImageSizeUi(Image image)
    {
        ImageSizeTextBlock.Text = $"{image.PixelCountX} × {image.PixelCountY}";
    }

    private void UpdateBrushSizeUi(int brushSize)
    {
        BrushSizeTextBlock.Text = $"{brushSize} px";
    }

    private void UpdateZoomUi()
    {
        ZoomTextBlock.Text = $"{PixelCanvas.Zoom * 100:0} %";
    }

    private void ClampBrushSizeToImage(Image image)
    {
        var maxBrushSize = Math.Max(1, Math.Min(256, Math.Max(image.PixelCountX, image.PixelCountY)));
        BrushSizeSlider.Maximum = maxBrushSize;
        if (_drawingService.BrushSize > maxBrushSize)
            _drawingService.BrushSize = maxBrushSize;

        BrushSizeSlider.Value = _drawingService.BrushSize;
        UpdateBrushSizeUi(_drawingService.BrushSize);
    }

    private void UpdateCommandState()
    {
        UndoMenuItem.IsEnabled = _drawingService.CanUndo;
        RedoMenuItem.IsEnabled = _drawingService.CanRedo;
    }

    private void RestoreWindowSize((uint width, uint height) editorSize)
    {
        var desiredWidth = Math.Max(816, editorSize.width + 180);
        var desiredHeight = Math.Max(538, editorSize.height + 120);
        var screen = Screens.ScreenFromVisual(this);
        if (screen is not null)
        {
            desiredWidth = Math.Min(desiredWidth, (uint)(screen.WorkingArea.Width * 0.95));
            desiredHeight = Math.Min(desiredHeight, (uint)(screen.WorkingArea.Height * 0.95));
        }

        Width = desiredWidth;
        Height = desiredHeight;
    }

    private void SyncCanvasViewport()
    {
        PixelCanvas.ScrollViewportOffset = EditorScrollViewer.Offset;
        PixelCanvas.ScrollViewportSize   = GetViewportSize();
    }
}

