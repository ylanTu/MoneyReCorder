using Microsoft.Data.Sqlite;
using MoneyReCorder.Models;

namespace MoneyReCorder.Data;

public class AccountRepository : IAccountRepository
{
    private readonly DatabaseContext _context;

    public AccountRepository(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<List<Account>> GetAllAsync(bool includeDeleted = false)
    {
        var accounts = new List<Account>();
        var conn = _context.GetConnection();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = includeDeleted
            ? "SELECT * FROM Accounts ORDER BY SortOrder ASC, Id ASC"
            : "SELECT * FROM Accounts WHERE IsDeleted = 0 ORDER BY SortOrder ASC, Id ASC";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            accounts.Add(MapAccount(reader));
        }
        return accounts;
    }

    public async Task<List<Account>> GetWidgetAccountsAsync()
    {
        var accounts = new List<Account>();
        var conn = _context.GetConnection();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Accounts WHERE ShowInWidget = 1 AND IsDeleted = 0 ORDER BY SortOrder ASC, Id ASC";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            accounts.Add(MapAccount(reader));
        }
        return accounts;
    }

    public async Task<Account?> GetByIdAsync(int id)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Accounts WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapAccount(reader);
        return null;
    }

    public async Task<int> AddAsync(Account account)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Accounts (Name, Type, BalanceInCents, Currency, LastFourDigits, Icon, SortOrder, ShowInWidget, IsDeleted, CreatedAt, UpdatedAt)
            VALUES ($name, $type, $balance, $currency, $lastFour, $icon, $sort, $show, 0, $created, $updated);
            SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("$name", account.Name);
        cmd.Parameters.AddWithValue("$type", (int)account.Type);
        cmd.Parameters.AddWithValue("$balance", account.BalanceInCents);
        cmd.Parameters.AddWithValue("$currency", account.Currency);
        cmd.Parameters.AddWithValue("$lastFour", (object?)account.LastFourDigits ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$icon", (object?)account.Icon ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sort", account.SortOrder);
        cmd.Parameters.AddWithValue("$show", account.ShowInWidget ? 1 : 0);
        cmd.Parameters.AddWithValue("$created", account.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$updated", account.UpdatedAt.ToString("o"));

        var result = await cmd.ExecuteScalarAsync();
        account.Id = Convert.ToInt32(result);
        return account.Id;
    }

    public async Task UpdateAsync(Account account)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Accounts SET
                Name = $name, Type = $type, BalanceInCents = $balance,
                Currency = $currency, LastFourDigits = $lastFour,
                Icon = $icon, SortOrder = $sort, ShowInWidget = $show,
                UpdatedAt = $updated
            WHERE Id = $id";

        cmd.Parameters.AddWithValue("$id", account.Id);
        cmd.Parameters.AddWithValue("$name", account.Name);
        cmd.Parameters.AddWithValue("$type", (int)account.Type);
        cmd.Parameters.AddWithValue("$balance", account.BalanceInCents);
        cmd.Parameters.AddWithValue("$currency", account.Currency);
        cmd.Parameters.AddWithValue("$lastFour", (object?)account.LastFourDigits ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$icon", (object?)account.Icon ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sort", account.SortOrder);
        cmd.Parameters.AddWithValue("$show", account.ShowInWidget ? 1 : 0);
        cmd.Parameters.AddWithValue("$updated", DateTime.Now.ToString("o"));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Accounts SET IsDeleted = 1, UpdatedAt = $updated WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$updated", DateTime.Now.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<long> GetTotalBalanceAsync()
    {
        var conn = _context.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(BalanceInCents), 0) FROM Accounts WHERE IsDeleted = 0";
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private static Account MapAccount(SqliteDataReader reader)
    {
        return new Account
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Type = (AccountType)reader.GetInt32(2),
            BalanceInCents = reader.GetInt64(3),
            Currency = reader.GetString(4),
            LastFourDigits = reader.IsDBNull(5) ? null : reader.GetString(5),
            Icon = reader.IsDBNull(6) ? null : reader.GetString(6),
            SortOrder = reader.GetInt32(7),
            ShowInWidget = reader.GetInt32(8) == 1,
            IsDeleted = reader.GetInt32(9) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(10)),
            UpdatedAt = DateTime.Parse(reader.GetString(11)),
        };
    }
}
