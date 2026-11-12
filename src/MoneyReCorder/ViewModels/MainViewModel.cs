using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyReCorder.Models;
using MoneyReCorder.Services;

namespace MoneyReCorder.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// 负责页面导航和全局状态
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IAccountService _accountService;
    private readonly ITransactionService _transactionService;
    private readonly ICategoryService _categoryService;

    public MainViewModel(
        IAccountService accountService,
        ITransactionService transactionService,
        ICategoryService categoryService)
    {
        _accountService = accountService;
        _transactionService = transactionService;
        _categoryService = categoryService;
    }

    [ObservableProperty]
    private string _currentPage = "Dashboard";

    /// <summary>当前页面中文标题（顶部栏显示）</summary>
    public string CurrentPageTitle => CurrentPage switch
    {
        "Dashboard" => "总览",
        "Account" => "账户",
        "Transaction" => "流水",
        "Statistics" => "统计",
        "Settings" => "设置",
        _ => string.Empty,
    };

    partial void OnCurrentPageChanged(string value) => OnPropertyChanged(nameof(CurrentPageTitle));

    [ObservableProperty]
    private string _totalBalanceDisplay = "¥0.00";

    [ObservableProperty]
    private string _monthlyIncomeDisplay = "¥0.00";

    [ObservableProperty]
    private string _monthlyExpenseDisplay = "¥0.00";

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<Transaction> RecentTransactions { get; } = new();

    [RelayCommand]
    private void NavigateTo(string page)
    {
        CurrentPage = page;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            Accounts.Clear();
            foreach (var a in accounts)
                Accounts.Add(a);

            var totalBalance = await _accountService.GetTotalBalanceAsync();
            TotalBalanceDisplay = $"¥{totalBalance:N2}";

            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var income = await _transactionService.GetTotalIncomeAsync(monthStart, monthEnd);
            var expense = await _transactionService.GetTotalExpenseAsync(monthStart, monthEnd);
            MonthlyIncomeDisplay = $"¥{income:N2}";
            MonthlyExpenseDisplay = $"¥{expense:N2}";

            var recent = await _transactionService.GetTransactionsAsync(
                monthStart, monthEnd);
            RecentTransactions.Clear();
            foreach (var t in recent.Take(10))
                RecentTransactions.Add(t);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
