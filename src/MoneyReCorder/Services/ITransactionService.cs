using MoneyReCorder.Models;

namespace MoneyReCorder.Services;

public interface ITransactionService
{
    Task<List<Transaction>> GetTransactionsAsync(DateTime? start = null, DateTime? end = null, int? accountId = null, int? categoryId = null);
    Task<Transaction?> GetTransactionByIdAsync(int id);
    Task<Transaction> AddTransactionAsync(int accountId, int categoryId, TransactionType type, decimal amount, DateTime date, string? description = null);
    Task UpdateTransactionAsync(Transaction transaction);
    Task DeleteTransactionAsync(int id);
    Task<decimal> GetTotalIncomeAsync(DateTime start, DateTime end);
    Task<decimal> GetTotalExpenseAsync(DateTime start, DateTime end);
    Task<List<(TransactionCategory Category, decimal Total)>> GetGroupedByCategoryAsync(DateTime start, DateTime end, TransactionType type);
    Task<List<(int Year, int Month, decimal Income, decimal Expense)>> GetMonthlySummaryAsync(int year);
}
