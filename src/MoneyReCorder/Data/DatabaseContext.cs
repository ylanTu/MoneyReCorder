using System.IO;
using Microsoft.Data.Sqlite;
using MoneyReCorder.Models;

namespace MoneyReCorder.Data;

/// <summary>
/// SQLite 数据库上下文。
/// 负责数据库初始化和提供原始连接。
/// 未来可扩展为支持云端同步的接口实现。
/// </summary>
public class DatabaseContext : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public DatabaseContext(string? dbPath = null)
    {
        dbPath ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MoneyReCorder",
            "data.db");

        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _connectionString = $"Data Source={dbPath}";
    }

    public SqliteConnection GetConnection()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }
        return _connection;
    }

    /// <summary>初始化数据库，创建表结构</summary>
    public void Initialize()
    {
        var conn = GetConnection();

        // 开启 WAL 模式提升并发性能
        using var walCmd = conn.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        walCmd.ExecuteNonQuery();

        // 开启外键约束
        using var fkCmd = conn.CreateCommand();
        fkCmd.CommandText = "PRAGMA foreign_keys=ON;";
        fkCmd.ExecuteNonQuery();

        CreateTables(conn);
        SeedDefaultCategories(conn);
    }

    private static void CreateTables(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Accounts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Type INTEGER NOT NULL DEFAULT 0,
                BalanceInCents INTEGER NOT NULL DEFAULT 0,
                Currency TEXT NOT NULL DEFAULT 'CNY',
                LastFourDigits TEXT,
                Icon TEXT,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                ShowInWidget INTEGER NOT NULL DEFAULT 1,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS TransactionCategories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Icon TEXT,
                ParentId INTEGER,
                TransactionType INTEGER,
                SortOrder INTEGER NOT NULL DEFAULT 0,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (ParentId) REFERENCES TransactionCategories(Id)
            );

            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AccountId INTEGER NOT NULL,
                CategoryId INTEGER NOT NULL,
                Type INTEGER NOT NULL,
                AmountInCents INTEGER NOT NULL,
                TransactionDate TEXT NOT NULL,
                Description TEXT,
                Tags TEXT,
                RepeatType INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (AccountId) REFERENCES Accounts(Id),
                FOREIGN KEY (CategoryId) REFERENCES TransactionCategories(Id)
            );

            CREATE INDEX IF NOT EXISTS IX_Transactions_Date ON Transactions(TransactionDate);
            CREATE INDEX IF NOT EXISTS IX_Transactions_Account ON Transactions(AccountId);
            CREATE INDEX IF NOT EXISTS IX_Transactions_Category ON Transactions(CategoryId);
            CREATE INDEX IF NOT EXISTS IX_Accounts_Widget ON Accounts(ShowInWidget, IsDeleted);
        ";
        cmd.ExecuteNonQuery();
    }

    private static void SeedDefaultCategories(SqliteConnection conn)
    {
        // 检查是否已有数据
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(1) FROM TransactionCategories";
        var count = Convert.ToInt64(checkCmd.ExecuteScalar());
        if (count > 0) return;

        var categories = new (string Name, string? Icon, TransactionType? Type, int Sort)[]
        {
            // 支出分类
            ("餐饮", "🍜",  Models.TransactionType.Expense, 1),
            ("交通", "🚗",  Models.TransactionType.Expense, 2),
            ("购物", "🛒",  Models.TransactionType.Expense, 3),
            ("住房", "🏠",  Models.TransactionType.Expense, 4),
            ("娱乐", "🎮",  Models.TransactionType.Expense, 5),
            ("医疗", "💊",  Models.TransactionType.Expense, 6),
            ("教育", "📚",  Models.TransactionType.Expense, 7),
            ("通讯", "📱",  Models.TransactionType.Expense, 8),
            ("服饰", "👔",  Models.TransactionType.Expense, 9),
            ("日用", "🧴",  Models.TransactionType.Expense, 10),
            ("其他支出", "📌", Models.TransactionType.Expense, 99),

            // 收入分类
            ("工资", "💰",  Models.TransactionType.Income, 1),
            ("奖金", "🎯",  Models.TransactionType.Income, 2),
            ("理财", "📈",  Models.TransactionType.Income, 3),
            ("红包", "🧧",  Models.TransactionType.Income, 4),
            ("兼职", "💼",  Models.TransactionType.Income, 5),
            ("其他收入", "📌", Models.TransactionType.Income, 99),
        };

        foreach (var (name, icon, type, sort) in categories)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TransactionCategories (Name, Icon, TransactionType, SortOrder)
                VALUES ($name, $icon, $type, $sort)";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$icon", icon ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("$type", (int?)type);
            cmd.Parameters.AddWithValue("$sort", sort);
            cmd.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }
}
