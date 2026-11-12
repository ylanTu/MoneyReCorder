using System.Globalization;
using System.Windows.Data;

namespace MoneyReCorder.Converters;

/// <summary>
/// 字符串为空时回退到 ConverterParameter（用于账户图标缺失时显示默认 emoji）。
/// </summary>
public class StringFallbackConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && !string.IsNullOrWhiteSpace(s)) return s;
        return parameter as string ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
