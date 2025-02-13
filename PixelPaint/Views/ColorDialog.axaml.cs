using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PixelPaint.Views;

public partial class ColorDialog : Window
{
    public ColorDialog()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(ColorPicker.Color);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}