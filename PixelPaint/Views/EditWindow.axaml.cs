using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
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
        BrowseColorsButton.Click += BrowseColorsButtonOnClick;
        _drawingService.OtherColorChanged += (sender, color) =>
        {
            OtherColorRadioButton.Background = new SolidColorBrush(color);
            OtherColorRadioButton.Foreground = new SolidColorBrush(_drawingService.GetContrastColor(color));
            OtherColorRadioButton.IsChecked = true;
        };
    }

    private async void SaveMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        if (_drawingService.ImagePanel is null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "No image to save").ShowAsPopupAsync(this);
            return;
        }
        
        var fileService = _services.GetRequiredService<IFileService>();
        var dialogResult = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "axp", FileTypeChoices = fileService.FileTypeFilter, Title = "Save Image"
        });
        if (dialogResult is null) return;
        var localPath = dialogResult.TryGetLocalPath() ?? dialogResult.Path.ToString();
        // Calculate the size of the editor without accessing the size of the grid
        var margin = EditorBorder.Margin;
        var borderSize = EditorBorder.BorderThickness;
        var windowSize = new Size(Width - margin.Left - margin.Right - borderSize.Left - borderSize.Right,
            Height - margin.Top - margin.Bottom - borderSize.Top - borderSize.Bottom);
        fileService.SaveImage(_drawingService.ImagePanel.ToImage(), localPath, ((uint)windowSize.Width, (uint)windowSize.Height));
    }

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
            { FileTypeFilter = fileService.FileTypeFilter, AllowMultiple = false, Title = "Open Image" });
        if (dialogResult.Count == 0) return;
        var file = dialogResult[0];

        var localPath = file.TryGetLocalPath() ?? file.Path.ToString();
        var (image, editorSize) = fileService.LoadImage(localPath);
        // calculate the size of the window without setting the size of the editor
        var margin = EditorBorder.Margin;
        var borderSize = EditorBorder.BorderThickness;
        var windowSize = new Size(editorSize.width + margin.Left + margin.Right + borderSize.Left + borderSize.Right,
            editorSize.height + margin.Top + margin.Bottom + borderSize.Top + borderSize.Bottom);
        Width = windowSize.Width;
        Height = windowSize.Height;
        _drawingService.DrawImage(image);
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