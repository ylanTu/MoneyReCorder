using MoneyReCorder.Models;

namespace MoneyReCorder.Data;

/// <summary>账户仓储接口</summary>
public interface IAccountRepository
{
    Task<List<Account>> GetAllAsync(bool includeDeleted = false);
    Task<List<Account>> GetWidgetAccountsAsync();
    Task<Account?> GetByIdAsync(int id);
    Task<int> AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task DeleteAsync(int id);
    Task<long> GetTotalBalanceAsync();
}
