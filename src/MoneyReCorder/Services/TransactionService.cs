using MoneyReCorder.Data;
using MoneyReCorder.Models;

namespace MoneyReCorder.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;

    public TransactionService(ITransactionRepository transactionRepository, ICategoryRepository categoryRepository)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepository;
    }

    public Task<List<Transaction>> GetTransactionsAsync(DateTime? start = null, DateTime? end = null, int? accountId = null, int? categoryId = null)
        => _transactionRepository.GetAllAsync(start, end, accountId, categoryId);

    public Task<Transaction?> GetTransactionByIdAsync(int id)
        => _transactionRepository.GetByIdAsync(id);

    public async Task<Transaction> AddTransactionAsync(int accountId, int categoryId, TransactionType type, decimal amount, DateTime date, string? description = null)
    {
        var transaction = new Transaction
        {
            AccountId = accountId,
            CategoryId = categoryId,
            Type = type,
            AmountInCents = (long)(amount * 100),
            TransactionDate = date,
            Description = description,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
        };

        await _transactionRepository.AddAsync(transaction);
        return transaction;
    }

    public Task UpdateTransactionAsync(Transaction transaction)
        => _transactionRepository.UpdateAsync(transaction);

    public Task DeleteTransactionAsync(int id)
        => _transactionRepository.DeleteAsync(id);

    public async Task<decimal> GetTotalIncomeAsync(DateTime start, DateTime end)
        => (await _transactionRepository.GetTotalIncomeAsync(start, end)) / 100m;

    public async Task<decimal> GetTotalExpenseAsync(DateTime start, DateTime end)
        => (await _transactionRepository.GetTotalExpenseAsync(start, end)) / 100m;

    public async Task<List<(TransactionCategory Category, decimal Total)>> GetGroupedByCategoryAsync(DateTime start, DateTime end, TransactionType type)
    {
        var results = await _transactionRepository.GetGroupedByCategoryAsync(start, end, type);
        var list = new List<(TransactionCategory, decimal)>();
        foreach (var (catId, total) in results)
        {
            var category = await _categoryRepository.GetByIdAsync(catId);
            if (category != null)
                list.Add((category, total / 100m));
        }
        return list;
    }

    public async Task<List<(int Year, int Month, decimal Income, decimal Expense)>> GetMonthlySummaryAsync(int year)
    {
        var results = await _transactionRepository.GetMonthlySummaryAsync(year);
        return results.Select(r => (r.Year, r.Month, r.Income / 100m, r.Expense / 100m)).ToList();
    }
}
