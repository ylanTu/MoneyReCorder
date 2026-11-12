using System.Globalization;
using System.Windows.Data;

namespace MoneyReCorder.Converters;

/// <summary>
/// 字符串相等比较：用于让侧边栏导航项跟随当前页面高亮。
/// </summary>
public class PageEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
