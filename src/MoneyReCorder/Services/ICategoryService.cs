using MoneyReCorder.Models;

namespace MoneyReCorder.Services;

public interface ICategoryService
{
    Task<List<TransactionCategory>> GetAllCategoriesAsync();
    Task<List<TransactionCategory>> GetCategoriesByTypeAsync(TransactionType type);
    Task<TransactionCategory?> GetCategoryByIdAsync(int id);
    Task<TransactionCategory> CreateCategoryAsync(string name, TransactionType type, string? icon = null);
    Task UpdateCategoryAsync(TransactionCategory category);
    Task DeleteCategoryAsync(int id);
}
