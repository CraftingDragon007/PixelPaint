using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PixelPaint.Views;

public partial class PixelSizeDialog : Window
{
    public PixelSizeDialog() : this(32, 16)
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
        Close(((int)(WidthInput.Value ?? 32), (int)(HeightInput.Value ?? 16)));
    }

    private void CancelButtonOnClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}

