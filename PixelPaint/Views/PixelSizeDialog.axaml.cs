using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PixelPaint.Views;

public partial class PixelSizeDialog : Window
{
    public PixelSizeDialog() : this(1280, 720)
    {
    }

    public PixelSizeDialog(int currentWidth, int currentHeight)
    {
        InitializeComponent();
        WidthInput.Value = currentWidth;
        HeightInput.Value = currentHeight;
        OkButton.Click += OkButtonOnClick;
        CancelButton.Click += CancelButtonOnClick;
    }

    private void OkButtonOnClick(object? sender, RoutedEventArgs e)
    {
        Close(((int)(WidthInput.Value ?? 1280), (int)(HeightInput.Value ?? 720)));
    }

    private void CancelButtonOnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}

