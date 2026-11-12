using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyReCorder.Models;
using MoneyReCorder.Services;

namespace MoneyReCorder.ViewModels;

public partial class AccountViewModel : ObservableObject
{
    private readonly IAccountService _accountService;

    public AccountViewModel(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public ObservableCollection<Account> Accounts { get; } = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    // 表单绑定
    [ObservableProperty]
    private string _newAccountName = string.Empty;

    [ObservableProperty]
    private AccountType _newAccountType = AccountType.BankCard;

    [ObservableProperty]
    private string _newBalanceText = "0.00";

    [ObservableProperty]
    private string? _newLastFourDigits;

    [ObservableProperty]
    private bool _showAddForm;

    [RelayCommand]
    private async Task LoadAccountsAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var accounts = await _accountService.GetAllAccountsAsync();
            Accounts.Clear();
            foreach (var a in accounts)
                Accounts.Add(a);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载账户失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAccountName))
        {
            ErrorMessage = "请输入账户名称";
            return;
        }

        if (!decimal.TryParse(NewBalanceText, out var balance))
        {
            ErrorMessage = "请输入有效的余额数字";
            return;
        }

        try
        {
            var initialCents = (long)(balance * 100);
            await _accountService.CreateAccountAsync(
                NewAccountName.Trim(),
                NewAccountType,
                initialCents,
                string.IsNullOrWhiteSpace(NewLastFourDigits) ? null : NewLastFourDigits.Trim());

            NewAccountName = string.Empty;
            NewBalanceText = "0.00";
            NewLastFourDigits = null;
            ShowAddForm = false;
            ErrorMessage = null;

            await LoadAccountsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"添加账户失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteAccountAsync(Account? account)
    {
        if (account == null) return;
        try
        {
            await _accountService.DeleteAccountAsync(account.Id);
            await LoadAccountsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"删除账户失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ToggleWidgetVisibilityAsync(Account? account)
    {
        if (account == null) return;
        account.ShowInWidget = !account.ShowInWidget;
        await _accountService.UpdateAccountAsync(account);
    }

    [RelayCommand]
    private void ToggleAddForm()
    {
        ShowAddForm = !ShowAddForm;
        ErrorMessage = null;
        if (!ShowAddForm)
        {
            NewAccountName = string.Empty;
            NewBalanceText = "0.00";
            NewLastFourDigits = null;
        }
    }
}
