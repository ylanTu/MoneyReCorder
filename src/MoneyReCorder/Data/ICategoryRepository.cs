using MoneyReCorder.Models;

namespace MoneyReCorder.Data;

public interface ICategoryRepository
{
    Task<List<TransactionCategory>> GetAllAsync(bool includeDeleted = false);
    Task<List<TransactionCategory>> GetByTypeAsync(TransactionType? type);
    Task<TransactionCategory?> GetByIdAsync(int id);
    Task<int> AddAsync(TransactionCategory category);
    Task UpdateAsync(TransactionCategory category);
    Task DeleteAsync(int id);
}
