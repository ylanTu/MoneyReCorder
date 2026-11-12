using Microsoft.Data.Sqlite;
using MoneyReCorder.Models;

namespace MoneyReCorder.Data;

public class CategoryRepository : ICategoryRepository
{
    private readonly DatabaseContext _context;

    public CategoryRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<List<TransactionCategory>> GetAllAsync(bool includeDeleted = false)
    {
        var categories = new List<TransactionCategory>();
        var conn = _context.GetConnection();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = includeDeleted
            ? "SELECT * FROM TransactionCategories ORDER BY SortOrder ASC, Id ASC"
            : "SELECT * FROM TransactionCategories WHERE IsDeleted = 0 ORDER BY SortOrder ASC, Id ASC";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            categories.Add(MapCategory(reader));

        return categories;
    }

    public async Task<List<TransactionCategory>> GetByTypeAsync(TransactionType? type)
    {
        var categories = new List<TransactionCategory>();
        var conn = _context.GetConnection();

        using var cmd = conn.CreateCommand();
        if (type.HasValue)
        {
            cmd.CommandText = "SELECT * FROM TransactionCategories WHERE TransactionType = $type AND IsDeleted = 0 ORDER BY SortOrder ASC, Id ASC";
            cmd.Parameters.AddWithValue("$type", (int)type.Value);
        }
        else
        {
            cmd.CommandText = "SELECT * FROM TransactionCategories WHERE IsDeleted = 0 ORDER BY SortOrder ASC, Id ASC";
        }

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            categories.Add(MapCategory(reader));

        return categories;
    }

    public async Task<TransactionCategory?> GetByIdAsync(int id)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM TransactionCategories WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapCategory(reader);
        return null;
    }

    public async Task<int> AddAsync(TransactionCategory category)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO TransactionCategories (Name, Icon, ParentId, TransactionType, SortOrder, IsDeleted)
            VALUES ($name, $icon, $parentId, $type, $sort, 0);
            SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("$name", category.Name);
        cmd.Parameters.AddWithValue("$icon", (object?)category.Icon ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$parentId", (object?)category.ParentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$type", (int?)category.TransactionType);
        cmd.Parameters.AddWithValue("$sort", category.SortOrder);

        var result = await cmd.ExecuteScalarAsync();
        category.Id = Convert.ToInt32(result);
        return category.Id;
    }

    public async Task UpdateAsync(TransactionCategory category)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE TransactionCategories SET
                Name = $name, Icon = $icon, ParentId = $parentId,
                TransactionType = $type, SortOrder = $sort
            WHERE Id = $id";

        cmd.Parameters.AddWithValue("$id", category.Id);
        cmd.Parameters.AddWithValue("$name", category.Name);
        cmd.Parameters.AddWithValue("$icon", (object?)category.Icon ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$parentId", (object?)category.ParentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$type", (int?)category.TransactionType);
        cmd.Parameters.AddWithValue("$sort", category.SortOrder);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE TransactionCategories SET IsDeleted = 1 WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    private static TransactionCategory MapCategory(SqliteDataReader reader)
    {
        return new TransactionCategory
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Icon = reader.IsDBNull(2) ? null : reader.GetString(2),
            ParentId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            TransactionType = reader.IsDBNull(4) ? null : (TransactionType?)reader.GetInt32(4),
            SortOrder = reader.GetInt32(5),
            IsDeleted = reader.GetInt32(6) == 1,
        };
    }
}
