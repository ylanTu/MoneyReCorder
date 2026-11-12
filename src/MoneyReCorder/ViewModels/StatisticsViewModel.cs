using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using MoneyReCorder.Services;
using SkiaSharp;

namespace MoneyReCorder.ViewModels;

public partial class StatisticsViewModel : ObservableObject
{
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;
    private readonly IThemeService? _themeService;

    public StatisticsViewModel(ITransactionService transactionService, ICategoryService categoryService,
                               IThemeService themeService)
    {
        _transactionService = transactionService;
        _categoryService = categoryService;
        _themeService = themeService;

        ApplyChartTheme();
        if (_themeService is not null)
            _themeService.AppearanceChanged += (_, _) => ApplyChartTheme();
    }

    // ═══════════ 图表配色（跟随深浅色，避免深色皮肤里出现白底图表） ═══════════

    [ObservableProperty]
    private Axis[] _xAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _yAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private SolidColorPaint? _chartLegendPaint;

    /// <summary>图表本体透明，让卡片背景透出来</summary>
    public System.Windows.Media.Brush ChartBackground => System.Windows.Media.Brushes.Transparent;

    private void ApplyChartTheme()
    {
        bool dark = _themeService?.IsDarkMode ?? false;

        var labelColor = SKColor.Parse(dark ? "#8C97A8" : "#6B7280");
        var gridColor = SKColor.Parse(dark ? "#2A3242" : "#E5E7EB");
        var textColor = SKColor.Parse(dark ? "#D6DCE6" : "#374151");

        var labels = new SolidColorPaint(labelColor) { SKTypeface = SKTypeface.Default };
        var separators = new SolidColorPaint(gridColor) { StrokeThickness = 1 };

        XAxes = new[]
        {
            new Axis
            {
                LabelsPaint = labels,
                SeparatorsPaint = separators,
                TextSize = 11,
            },
        };

        YAxes = new[]
        {
            new Axis
            {
                LabelsPaint = labels,
                SeparatorsPaint = separators,
                TextSize = 11,
            },
        };

        ChartLegendPaint = new SolidColorPaint(textColor) { SKTypeface = SKTypeface.Default };
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _selectedYear = DateTime.Now.Year;

    [ObservableProperty]
    private int _selectedMonth = DateTime.Now.Month;

    [ObservableProperty]
    private string _totalIncomeDisplay = "¥0.00";

    [ObservableProperty]
    private string _totalExpenseDisplay = "¥0.00";

    [ObservableProperty]
    private string _totalBalanceDisplay = "¥0.00";

    // 月度趋势图表
    public ObservableCollection<ISeries> MonthlySeries { get; } = new();

    // 分类饼图
    public ObservableCollection<ISeries> CategoryPieSeries { get; } = new();

    public List<int> AvailableYears { get; } = Enumerable.Range(2020, 10).ToList();

    // 图例
    public ObservableCollection<CategoryLegendItem> CategoryLegends { get; } = new();

    [RelayCommand]
    private async Task LoadStatisticsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await LoadMonthlyTrendAsync();
            await LoadCategoryPieAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载统计数据失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadMonthlyTrendAsync()
    {
        var summary = await _transactionService.GetMonthlySummaryAsync(SelectedYear);

        var months = summary.Select(s => s.Month).ToList();
        var incomeValues = summary.Select(s => (double)s.Income).ToArray();
        var expenseValues = summary.Select(s => (double)s.Expense).ToArray();

        var monthLabels = months.Select(m => $"{m}月").ToArray();

        MonthlySeries.Clear();
        MonthlySeries.Add(new ColumnSeries<double>
        {
            Values = incomeValues,
            Name = "收入",
            Fill = new SolidColorPaint(SKColors.MediumSeaGreen),
            Stroke = null,
        });
        MonthlySeries.Add(new ColumnSeries<double>
        {
            Values = expenseValues,
            Name = "支出",
            Fill = new SolidColorPaint(SKColors.IndianRed),
            Stroke = null,
        });
    }

    private async Task LoadCategoryPieAsync()
    {
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        // 支出分类统计
        var expenseByCategory = await _transactionService.GetGroupedByCategoryAsync(monthStart, monthEnd, Models.TransactionType.Expense);
        var totalExpense = expenseByCategory.Sum(e => (double)e.Total);

        CategoryPieSeries.Clear();
        CategoryLegends.Clear();

        if (totalExpense <= 0)
        {
            // 无数据时显示占位（用低对比度的主题灰，避免深色皮肤下出现刺眼白盘）
            CategoryPieSeries.Add(new PieSeries<double>
            {
                Values = new double[] { 1 },
                Fill = new SolidColorPaint(_themeService?.IsDarkMode == true
                    ? SKColor.Parse("#2A3242")
                    : SKColor.Parse("#E8EAEE")),
                Stroke = null,
                DataLabelsSize = 0,
            });
            return;
        }

        var colors = new SKColor[]
        {
            new(0xFF, 0x6B, 0x6B), // 红
            new(0x4E, 0xC9, 0xB5), // 青
            new(0xFF, 0xA5, 0x00), // 橙
            new(0x45, 0x6E, 0xDC), // 蓝
            new(0x98, 0x5D, 0xB3), // 紫
            new(0x2E, 0xCC, 0x71), // 绿
            new(0xF3, 0x9C, 0x12), // 黄
            new(0xE7, 0x4C, 0x3C), // 深红
            new(0x34, 0x95, 0xDB), // 天蓝
            new(0x1A, 0xBC, 0x9C), // 翡翠
        };

        var colorIndex = 0;
        foreach (var (category, total) in expenseByCategory)
        {
            if (total <= 0) continue;
            var percentage = totalExpense > 0 ? (double)total / totalExpense * 100 : 0;
            var color = colors[colorIndex % colors.Length];

            CategoryPieSeries.Add(new PieSeries<double>
            {
                Values = new double[] { (double)total },
                Name = category.Name,
                Fill = new SolidColorPaint(color),
                Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                DataLabelsSize = 0,
                HoverPushout = 5,
            });

            CategoryLegends.Add(new CategoryLegendItem
            {
                Name = category.Name,
                Color = color.ToString(),
                Percentage = percentage,
                Amount = $"¥{total:N2}",
            });

            colorIndex++;
        }

        TotalExpenseDisplay = $"¥{(decimal)totalExpense:N2}";
    }
}

public class CategoryLegendItem
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
    public double Percentage { get; set; }
    public string Amount { get; set; } = "¥0.00";
}
