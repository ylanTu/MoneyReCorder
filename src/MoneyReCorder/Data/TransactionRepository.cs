using Microsoft.Data.Sqlite;
using MoneyReCorder.Models;

namespace MoneyReCorder.Data;

public class TransactionRepository : ITransactionRepository
{
    private readonly DatabaseContext _context;

    public TransactionRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<List<Transaction>> GetAllAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? accountId = null,
        int? categoryId = null,
        bool includeDeleted = false)
    {
        var transactions = new List<Transaction>();
        var conn = _context.GetConnection();

        var sql = "SELECT * FROM Transactions WHERE 1=1";
        if (!includeDeleted) sql += " AND IsDeleted = 0";
        if (startDate.HasValue) sql += " AND TransactionDate >= $startDate";
        if (endDate.HasValue) sql += " AND TransactionDate <= $endDate";
        if (accountId.HasValue) sql += " AND AccountId = $accountId";
        if (categoryId.HasValue) sql += " AND CategoryId = $categoryId";
        sql += " ORDER BY TransactionDate DESC, Id DESC";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (startDate.HasValue) cmd.Parameters.AddWithValue("$startDate", startDate.Value.ToString("yyyy-MM-dd"));
        if (endDate.HasValue) cmd.Parameters.AddWithValue("$endDate", endDate.Value.ToString("yyyy-MM-dd"));
        if (accountId.HasValue) cmd.Parameters.AddWithValue("$accountId", accountId.Value);
        if (categoryId.HasValue) cmd.Parameters.AddWithValue("$categoryId", categoryId.Value);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            transactions.Add(MapTransaction(reader));

        return transactions;
    }

    public async Task<Transaction?> GetByIdAsync(int id)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Transactions WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapTransaction(reader);
        return null;
    }

    public async Task<int> AddAsync(Transaction transaction)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Transactions (AccountId, CategoryId, Type, AmountInCents, TransactionDate, Description, Tags, RepeatType, CreatedAt, UpdatedAt, IsDeleted)
            VALUES ($accountId, $categoryId, $type, $amount, $date, $desc, $tags, $repeat, $created, $updated, 0);
            SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("$accountId", transaction.AccountId);
        cmd.Parameters.AddWithValue("$categoryId", transaction.CategoryId);
        cmd.Parameters.AddWithValue("$type", (int)transaction.Type);
        cmd.Parameters.AddWithValue("$amount", transaction.AmountInCents);
        cmd.Parameters.AddWithValue("$date", transaction.TransactionDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$desc", (object?)transaction.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tags", (object?)transaction.Tags ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$repeat", (int)transaction.RepeatType);
        cmd.Parameters.AddWithValue("$created", transaction.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$updated", transaction.UpdatedAt.ToString("o"));

        var result = await cmd.ExecuteScalarAsync();
        transaction.Id = Convert.ToInt32(result);

        // 更新账户余额
        await UpdateAccountBalance(transaction.AccountId);
        return transaction.Id;
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        var originalAccountId = await GetOriginalAccountId(transaction.Id);

        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Transactions SET
                AccountId = $accountId, CategoryId = $categoryId, Type = $type,
                AmountInCents = $amount, TransactionDate = $date,
                Description = $desc, Tags = $tags, RepeatType = $repeat,
                UpdatedAt = $updated
            WHERE Id = $id";

        cmd.Parameters.AddWithValue("$id", transaction.Id);
        cmd.Parameters.AddWithValue("$accountId", transaction.AccountId);
        cmd.Parameters.AddWithValue("$categoryId", transaction.CategoryId);
        cmd.Parameters.AddWithValue("$type", (int)transaction.Type);
        cmd.Parameters.AddWithValue("$amount", transaction.AmountInCents);
        cmd.Parameters.AddWithValue("$date", transaction.TransactionDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$desc", (object?)transaction.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tags", (object?)transaction.Tags ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$repeat", (int)transaction.RepeatType);
        cmd.Parameters.AddWithValue("$updated", DateTime.Now.ToString("o"));

        await cmd.ExecuteNonQueryAsync();

        // 更新新旧账户余额
        await UpdateAccountBalance(transaction.AccountId);
        if (originalAccountId.HasValue && originalAccountId != transaction.AccountId)
            await UpdateAccountBalance(originalAccountId.Value);
    }

    public async Task DeleteAsync(int id)
    {
        var originalAccountId = await GetOriginalAccountId(id);

        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Transactions SET IsDeleted = 1, UpdatedAt = $updated WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$updated", DateTime.Now.ToString("o"));
        await cmd.ExecuteNonQueryAsync();

        if (originalAccountId.HasValue)
            await UpdateAccountBalance(originalAccountId.Value);
    }

    public async Task<long> GetTotalIncomeAsync(DateTime start, DateTime end)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COALESCE(SUM(AmountInCents), 0) FROM Transactions
            WHERE Type = 0 AND IsDeleted = 0
            AND TransactionDate >= $start AND TransactionDate <= $end";
        cmd.Parameters.AddWithValue("$start", start.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$end", end.ToString("yyyy-MM-dd"));
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    public async Task<long> GetTotalExpenseAsync(DateTime start, DateTime end)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COALESCE(SUM(AmountInCents), 0) FROM Transactions
            WHERE Type = 1 AND IsDeleted = 0
            AND TransactionDate >= $start AND TransactionDate <= $end";
        cmd.Parameters.AddWithValue("$start", start.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$end", end.ToString("yyyy-MM-dd"));
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    public async Task<List<(int CategoryId, long Total)>> GetGroupedByCategoryAsync(DateTime start, DateTime end, TransactionType type)
    {
        var results = new List<(int, long)>();
        var conn = _context.GetConnection();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT CategoryId, SUM(AmountInCents) as Total
            FROM Transactions
            WHERE Type = $type AND IsDeleted = 0
            AND TransactionDate >= $start AND TransactionDate <= $end
            GROUP BY CategoryId
            ORDER BY Total DESC";
        cmd.Parameters.AddWithValue("$type", (int)type);
        cmd.Parameters.AddWithValue("$start", start.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$end", end.ToString("yyyy-MM-dd"));

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            results.Add((reader.GetInt32(0), reader.GetInt64(1)));

        return results;
    }

    public async Task<List<(int Year, int Month, long Income, long Expense)>> GetMonthlySummaryAsync(int year)
    {
        var results = new List<(int, int, long, long)>();
        var conn = _context.GetConnection();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT CAST(strftime('%m', TransactionDate) AS INTEGER) as Month,
                   COALESCE(SUM(CASE WHEN Type = 0 THEN AmountInCents ELSE 0 END), 0) as Income,
                   COALESCE(SUM(CASE WHEN Type = 1 THEN AmountInCents ELSE 0 END), 0) as Expense
            FROM Transactions
            WHERE IsDeleted = 0 AND strftime('%Y', TransactionDate) = $year
            GROUP BY Month
            ORDER BY Month";
        cmd.Parameters.AddWithValue("$year", year.ToString());

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            results.Add((year, reader.GetInt32(0), reader.GetInt64(1), reader.GetInt64(2)));

        return results;
    }

    private static Transaction MapTransaction(SqliteDataReader reader)
    {
        return new Transaction
        {
            Id = reader.GetInt32(0),
            AccountId = reader.GetInt32(1),
            CategoryId = reader.GetInt32(2),
            Type = (TransactionType)reader.GetInt32(3),
            AmountInCents = reader.GetInt64(4),
            TransactionDate = DateTime.Parse(reader.GetString(5)),
            Description = reader.IsDBNull(6) ? null : reader.GetString(6),
            Tags = reader.IsDBNull(7) ? null : reader.GetString(7),
            RepeatType = (RepeatType)reader.GetInt32(8),
            CreatedAt = DateTime.Parse(reader.GetString(9)),
            UpdatedAt = DateTime.Parse(reader.GetString(10)),
            IsDeleted = reader.GetInt32(11) == 1,
        };
    }

    private async Task<int?> GetOriginalAccountId(int transactionId)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT AccountId FROM Transactions WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", transactionId);
        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : null;
    }

    private async Task UpdateAccountBalance(int accountId)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Accounts SET BalanceInCents = (
                SELECT COALESCE(SUM(CASE WHEN Type = 0 THEN AmountInCents ELSE -AmountInCents END), 0)
                FROM Transactions
                WHERE AccountId = $accountId AND IsDeleted = 0
            ), UpdatedAt = $updated
            WHERE Id = $accountId";
        cmd.Parameters.AddWithValue("$accountId", accountId);
        cmd.Parameters.AddWithValue("$updated", DateTime.Now.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }
}
