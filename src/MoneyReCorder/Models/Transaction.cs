namespace MoneyReCorder.Models;

/// <summary>交易记录（一笔收入或支出）</summary>
public class Transaction
{
    public int Id { get; set; }

    /// <summary>所属账户ID</summary>
    public int AccountId { get; set; }

    /// <summary>所属账户</summary>
    public Account? Account { get; set; }

    /// <summary>分类ID</summary>
    public int CategoryId { get; set; }

    /// <summary>交易分类</summary>
    public TransactionCategory? Category { get; set; }

    /// <summary>交易类型（收入/支出）</summary>
    public TransactionType Type { get; set; }

    /// <summary>金额（单位：分）</summary>
    public long AmountInCents { get; set; }

    /// <summary>显示用金额</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public decimal Amount => AmountInCents / 100m;

    /// <summary>交易日期</summary>
    public DateTime TransactionDate { get; set; } = DateTime.Today;

    /// <summary>备注</summary>
    public string? Description { get; set; }

    /// <summary>标签（JSON 数组字符串）</summary>
    public string? Tags { get; set; }

    /// <summary>是否已重复（用于周期性账单）</summary>
    public RepeatType RepeatType { get; set; } = RepeatType.None;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>最后修改时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>是否已删除（软删除）</summary>
    public bool IsDeleted { get; set; }
}
