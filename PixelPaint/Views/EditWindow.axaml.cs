using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using PixelPaint.Extensions;
using PixelPaint.Services;

namespace PixelPaint.Views;

public partial class EditWindow : Window
{
    private readonly IDrawingService _drawingService;
    private readonly IServiceProvider _services;
    private string? _currentFilePath;

    public EditWindow(IDrawingService drawingService, IServiceProvider services)
    {
        InitializeComponent();
        _drawingService = drawingService;
        _services = services;
        _drawingService.ImagePanel = ImagePanel;
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
        ImagePanel.PointerPressed += _drawingService.OnPointerPressed;
        ImagePanel.PointerMoved += _drawingService.OnPointerMoved;
        ImagePanel.PointerReleased += _drawingService.OnPointerReleased;
        ShowGridLinesCheckBox.IsCheckedChanged += ShowGridLinesCheckBoxOnIsCheckedChanged;
        OpenMenuItem.Click += OpenMenuItemOnClick;
        SaveMenuItem.Click += SaveMenuItemOnClick;
        SaveAsMenuItem.Click += SaveAsMenuItemOnClick;
        ResetMenuItem.Click += ResetMenuItemOnClick;
        PixelSizeMenuItem.Click += PixelSizeMenuItemOnClick;
        ExportMenuItem.Click += ExportMenuItemOnClick;
        UndoMenuItem.Click += UndoMenuItemOnClick;
        RedoMenuItem.Click += RedoMenuItemOnClick;
        BrowseColorsButton.Click += BrowseColorsButtonOnClick;
        _drawingService.OtherColorChanged += (sender, color) =>
        {
            OtherColorRadioButton.Background = new SolidColorBrush(color);
            OtherColorRadioButton.Foreground = new SolidColorBrush(_drawingService.GetContrastColor(color));
            OtherColorRadioButton.IsChecked = true;
        };
    }

    // ── Save ────────────────────────────────────────────────────────────────

    private async void SaveMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        if (_drawingService.ImagePanel is null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Fehler", "Kein Bild zum Speichern").ShowAsPopupAsync(this);
            return;
        }

        if (_currentFilePath is not null)
        {
            var fileService = _services.GetRequiredService<IFileService>();
            var editorSize = GetEditorSize();
            fileService.SaveImage(_drawingService.ImagePanel.ToImage(), _currentFilePath, editorSize);
            return;
        }

        await DoSaveAs();
    }

    private async void SaveAsMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        await DoSaveAs();
    }

    private async System.Threading.Tasks.Task DoSaveAs()
    {
        if (_drawingService.ImagePanel is null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Fehler", "Kein Bild zum Speichern").ShowAsPopupAsync(this);
            return;
        }

        var fileService = _services.GetRequiredService<IFileService>();
        var dialogResult = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "axp",
            FileTypeChoices = fileService.FileTypeFilter,
            Title = "Bild speichern"
        });
        if (dialogResult is null) return;
        var localPath = dialogResult.TryGetLocalPath() ?? dialogResult.Path.ToString();
        var editorSize = GetEditorSize();
        fileService.SaveImage(_drawingService.ImagePanel.ToImage(), localPath, editorSize);
        _currentFilePath = localPath;
    }

    private (uint width, uint height) GetEditorSize()
    {
        var margin = EditorBorder.Margin;
        var borderSize = EditorBorder.BorderThickness;
        return ((uint)(Width - margin.Left - margin.Right - borderSize.Left - borderSize.Right),
                (uint)(Height - margin.Top - margin.Bottom - borderSize.Top - borderSize.Bottom));
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
            _drawingService.DrawEmptyImage();
            _currentFilePath = null;
        }
    }

    // ── Pixel Size ──────────────────────────────────────────────────────────

    private async void PixelSizeMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        var currentCols = ImagePanel.ColumnDefinitions.Count > 0 ? ImagePanel.ColumnDefinitions.Count : 32;
        var currentRows = ImagePanel.RowDefinitions.Count > 0 ? ImagePanel.RowDefinitions.Count : 16;

        var dialog = new PixelSizeDialog(currentCols, currentRows);
        var result = await dialog.ShowDialog<(int width, int height)?>(this);
        if (result is null) return;

        _drawingService.NewImage(result.Value.width, result.Value.height);
        _currentFilePath = null;
    }

    // ── Export ──────────────────────────────────────────────────────────────

    private async void ExportMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        if (_drawingService.ImagePanel is null)
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
        var image = _drawingService.ImagePanel.ToImage();

        if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            var fileService = _services.GetRequiredService<IFileService>();
            fileService.SaveImage(image, path, GetEditorSize());
        }
        else
        {
            ExportToBitmap(image, path);
        }
    }

    private static void ExportToBitmap(PixelPaint.Models.Image image, string path)
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

    // ── Undo / Redo ─────────────────────────────────────────────────────────

    private void UndoMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        _drawingService.Undo();
    }

    private void RedoMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        _drawingService.Redo();
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
        if (sender is CheckBox checkBox) ImagePanel.ShowGridLines = checkBox.IsChecked ?? false;
    }

    private async void OpenMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        var fileService = _services.GetRequiredService<IFileService>();
        var dialogResult = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            { FileTypeFilter = fileService.FileTypeFilter, AllowMultiple = false, Title = "Bild öffnen" });
        if (dialogResult.Count == 0) return;
        var file = dialogResult[0];

        var localPath = file.TryGetLocalPath() ?? file.Path.ToString();
        var (image, editorSize) = fileService.LoadImage(localPath);
        var margin = EditorBorder.Margin;
        var borderSize = EditorBorder.BorderThickness;
        var windowSize = new Size(editorSize.width + margin.Left + margin.Right + borderSize.Left + borderSize.Right,
            editorSize.height + margin.Top + margin.Bottom + borderSize.Top + borderSize.Bottom);
        Width = windowSize.Width;
        Height = windowSize.Height;
        _drawingService.DrawImage(image);
        _currentFilePath = localPath;
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
}

