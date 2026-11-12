using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MoneyReCorder.Converters;

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolValue = value switch
        {
            true => true,
            false => false,
            string s => !string.IsNullOrEmpty(s),
            null => false,
            _ => false
        };
        bool invert = parameter is "invert";
        return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool v = value switch
        {
            true => true,
            false => false,
            string s => !string.IsNullOrEmpty(s),
            _ => false
        };
        return !v;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// 将 bool 转换为 HorizontalAlignment（用于 ToggleButton 滑块）
/// true=Right, false=Left
/// </summary>
public class BoolToHorizontalAlignmentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BooleanToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? 1.0 : 0.5;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class AccountTypeToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Models.AccountType type)
        {
            return type switch
            {
                Models.AccountType.BankCard => "银行卡",
                Models.AccountType.CreditCard => "信用卡",
                Models.AccountType.WeChat => "微信",
                Models.AccountType.Alipay => "支付宝",
                Models.AccountType.Cash => "现金",
                Models.AccountType.Other => "其他",
                _ => type.ToString()
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TransactionTypeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Models.TransactionType type)
        {
            return type switch
            {
                Models.TransactionType.Income => "#4CAF50",
                Models.TransactionType.Expense => "#FF6B6B",
                _ => "#999999"
            };
        }
        return "#999999";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class AmountDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return $"¥{d:N2}";
        if (value is long l)
            return $"¥{(l / 100m):N2}";
        return "¥0.00";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool v = value is true;
        string? prefix = parameter as string;
        return v ? (prefix + "visibility") : (prefix + "visibility_off");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
