using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace PixelPaint.Views;

public partial class ColorDialog : Window
{
    public ColorDialog() : this(null)
    {
    }

    public ColorDialog(Color? defaultColor = null)
    {
        InitializeComponent();
        if (defaultColor is not null)
            ColorPicker.Color = defaultColor.Value;
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