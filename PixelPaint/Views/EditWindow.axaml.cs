using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
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
    }

    private async void SaveMenuItemOnClick(object? sender, RoutedEventArgs e)
    {
        var fileService = _services.GetRequiredService<IFileService>();
        var dialogResult = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "axp", FileTypeChoices = fileService.FileTypeFilter, Title = "Save Image"
        });
        if (dialogResult is null) return;
        var file = dialogResult;
        var localPath = file.TryGetLocalPath() ?? file.Path.ToString();
        fileService.SaveImage(_drawingService.ImagePanel.ToImage(), localPath);
    }

    private async void BrowseColorsButtonOnClick(object? sender, RoutedEventArgs e)
    {
        var colorDialog = new ColorDialog();
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
        var image = fileService.LoadImage(localPath);
        _drawingService.DrawImage(image);
    }

    private void OnColorRadioButtonIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { IsChecked: true } radioButton) return;
        var color = (ISolidColorBrush?)radioButton.Background;
        if (color is null) return;
        _drawingService.CurrentColor = color.Color;
    }
}