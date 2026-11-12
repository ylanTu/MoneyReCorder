using MoneyReCorder.Models;

namespace MoneyReCorder.Services;

/// <summary>账户服务接口</summary>
public interface IAccountService
{
    Task<List<Account>> GetAllAccountsAsync();
    Task<List<Account>> GetWidgetAccountsAsync();
    Task<Account?> GetAccountByIdAsync(int id);
    Task<Account> CreateAccountAsync(string name, AccountType type, long initialBalanceInCents = 0, string? lastFourDigits = null, string? icon = null);
    Task UpdateAccountAsync(Account account);
    Task DeleteAccountAsync(int id);
    Task<decimal> GetTotalBalanceAsync();
    Task UpdateBalanceAsync(int accountId, long newBalanceInCents);
}
