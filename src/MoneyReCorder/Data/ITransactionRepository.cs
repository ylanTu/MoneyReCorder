using MoneyReCorder.Models;

namespace MoneyReCorder.Data;

public interface ITransactionRepository
{
    Task<List<Transaction>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null, int? accountId = null, int? categoryId = null, bool includeDeleted = false);
    Task<Transaction?> GetByIdAsync(int id);
    Task<int> AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(int id);
    Task<long> GetTotalIncomeAsync(DateTime start, DateTime end);
    Task<long> GetTotalExpenseAsync(DateTime start, DateTime end);
    Task<List<(int CategoryId, long Total)>> GetGroupedByCategoryAsync(DateTime start, DateTime end, TransactionType type);
    Task<List<(int Year, int Month, long Income, long Expense)>> GetMonthlySummaryAsync(int year);
}
