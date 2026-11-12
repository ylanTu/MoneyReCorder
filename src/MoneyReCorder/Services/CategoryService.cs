using MoneyReCorder.Data;
using MoneyReCorder.Models;

namespace MoneyReCorder.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService(ICategoryRepository repository)
    {
        _repository = repository;
    }

    public Task<List<TransactionCategory>> GetAllCategoriesAsync() => _repository.GetAllAsync();
    public Task<List<TransactionCategory>> GetCategoriesByTypeAsync(TransactionType type) => _repository.GetByTypeAsync(type);
    public Task<TransactionCategory?> GetCategoryByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<TransactionCategory> CreateCategoryAsync(string name, TransactionType type, string? icon = null)
    {
        var category = new TransactionCategory
        {
            Name = name,
            Icon = icon,
            TransactionType = type,
            SortOrder = 0,
        };
        await _repository.AddAsync(category);
        return category;
    }

    public Task UpdateCategoryAsync(TransactionCategory category) => _repository.UpdateAsync(category);
    public Task DeleteCategoryAsync(int id) => _repository.DeleteAsync(id);
}
