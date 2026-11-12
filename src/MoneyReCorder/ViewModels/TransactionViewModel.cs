using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyReCorder.Models;
using MoneyReCorder.Services;

namespace MoneyReCorder.ViewModels;

public partial class TransactionViewModel : ObservableObject
{
    private readonly ITransactionService _transactionService;
    private readonly IAccountService _accountService;
    private readonly ICategoryService _categoryService;

    public TransactionViewModel(
        ITransactionService transactionService,
        IAccountService accountService,
        ICategoryService categoryService)
    {
        _transactionService = transactionService;
        _accountService = accountService;
        _categoryService = categoryService;
    }

    public ObservableCollection<Transaction> Transactions { get; } = new();
    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<TransactionCategory> Categories { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _totalIncomeDisplay = "¥0.00";

    [ObservableProperty]
    private string _totalExpenseDisplay = "¥0.00";

    // 筛选条件
    [ObservableProperty]
    private DateTime _filterStartDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

    [ObservableProperty]
    private DateTime _filterEndDate = DateTime.Now;

    [ObservableProperty]
    private Account? _filterAccount;

    [ObservableProperty]
    private TransactionCategory? _filterCategory;

    // 新增交易表单
    [ObservableProperty]
    private bool _showAddForm;

    [ObservableProperty]
    private int _formAccountId;

    [ObservableProperty]
    private int _formCategoryId;

    [ObservableProperty]
    private TransactionType _formType = TransactionType.Expense;

    [ObservableProperty]
    private string _formAmountText = string.Empty;

    [ObservableProperty]
    private DateTime _formDate = DateTime.Today;

    [ObservableProperty]
    private string? _formDescription;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            Accounts.Clear();
            foreach (var a in accounts) Accounts.Add(a);

            await LoadCategoriesAsync();
            await LoadTransactionsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载数据失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        Categories.Clear();
        foreach (var c in categories) Categories.Add(c);
    }

    [RelayCommand]
    private async Task LoadTransactionsAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _transactionService.GetTransactionsAsync(
                FilterStartDate, FilterEndDate, FilterAccount?.Id, FilterCategory?.Id);

            Transactions.Clear();
            foreach (var t in list)
            {
                // 填充关联数据
                t.Account = Accounts.FirstOrDefault(a => a.Id == t.AccountId);
                t.Category = Categories.FirstOrDefault(c => c.Id == t.CategoryId);
                Transactions.Add(t);
            }

            var income = await _transactionService.GetTotalIncomeAsync(FilterStartDate, FilterEndDate);
            var expense = await _transactionService.GetTotalExpenseAsync(FilterStartDate, FilterEndDate);
            TotalIncomeDisplay = $"¥{income:N2}";
            TotalExpenseDisplay = $"¥{expense:N2}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载交易记录失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleAddForm()
    {
        ShowAddForm = !ShowAddForm;
        if (!ShowAddForm)
        {
            FormAmountText = string.Empty;
            FormDescription = null;
        }
        else if (Accounts.Count > 0 && FormAccountId == 0)
        {
            FormAccountId = Accounts[0].Id;
        }
    }

    [RelayCommand]
    private async Task AddTransactionAsync()
    {
        if (FormAccountId <= 0)
        {
            ErrorMessage = "请选择账户";
            return;
        }
        if (FormCategoryId <= 0)
        {
            ErrorMessage = "请选择分类";
            return;
        }
        if (!decimal.TryParse(FormAmountText, out var amount) || amount <= 0)
        {
            ErrorMessage = "请输入有效的金额";
            return;
        }

        try
        {
            await _transactionService.AddTransactionAsync(
                FormAccountId, FormCategoryId, FormType, amount, FormDate, FormDescription);

            ShowAddForm = false;
            FormAmountText = string.Empty;
            FormDescription = null;
            ErrorMessage = null;

            await LoadTransactionsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"添加交易失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteTransactionAsync(Transaction? transaction)
    {
        if (transaction == null) return;
        try
        {
            await _transactionService.DeleteTransactionAsync(transaction.Id);
            await LoadTransactionsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"删除失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SetFormType(TransactionType type)
    {
        FormType = type;
        FormCategoryId = 0;
        _ = LoadCategoriesAsync();
    }
}
