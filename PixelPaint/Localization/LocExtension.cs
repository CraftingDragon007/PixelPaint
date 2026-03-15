using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace PixelPaint.Localization;

public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
        Key = string.Empty;
    }

    public LocExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance
        };
    }
}

