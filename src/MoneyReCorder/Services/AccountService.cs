using MoneyReCorder.Data;
using MoneyReCorder.Models;

namespace MoneyReCorder.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;

    public AccountService(IAccountRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Account>> GetAllAccountsAsync() => _repository.GetAllAsync();
    public Task<List<Account>> GetWidgetAccountsAsync() => _repository.GetWidgetAccountsAsync();
    public Task<Account?> GetAccountByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<Account> CreateAccountAsync(string name, AccountType type, long initialBalanceInCents = 0, string? lastFourDigits = null, string? icon = null)
    {
        var account = new Account
        {
            Name = name,
            Type = type,
            BalanceInCents = initialBalanceInCents,
            LastFourDigits = lastFourDigits,
            Icon = icon,
            SortOrder = 0,
            ShowInWidget = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        await _repository.AddAsync(account);
        return account;
    }

    public Task UpdateAccountAsync(Account account) => _repository.UpdateAsync(account);
    public Task DeleteAccountAsync(int id) => _repository.DeleteAsync(id);
    public async Task<decimal> GetTotalBalanceAsync()
    {
        var total = await _repository.GetTotalBalanceAsync();
        return total / 100m;
    }

    public async Task UpdateBalanceAsync(int accountId, long newBalanceInCents)
    {
        var account = await _repository.GetByIdAsync(accountId);
        if (account != null)
        {
            account.BalanceInCents = newBalanceInCents;
            account.UpdatedAt = DateTime.Now;
            await _repository.UpdateAsync(account);
        }
    }
}
