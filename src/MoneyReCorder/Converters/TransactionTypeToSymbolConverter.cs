using System.Globalization;
using System.Windows.Data;
using MoneyReCorder.Models;

namespace MoneyReCorder.Converters;

/// <summary>
/// 交易类型转显示符号
/// 收入 → +, 支出 → -
/// </summary>
public class TransactionTypeToSymbolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type switch
            {
                TransactionType.Income => "+",
                TransactionType.Expense => "-",
                _ => "?"
            };
        }
        return "?";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
