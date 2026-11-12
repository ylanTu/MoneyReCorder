namespace MoneyReCorder.Models;

/// <summary>交易分类</summary>
public class TransactionCategory
{
    public int Id { get; set; }

    /// <summary>分类名称，如"餐饮"、"交通"、"购物"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>图标（Emoji 或图标名称）</summary>
    public string? Icon { get; set; }

    /// <summary>父分类ID（支持二级分类）</summary>
    public int? ParentId { get; set; }

    /// <summary>父分类</summary>
    public TransactionCategory? Parent { get; set; }

    /// <summary>子分类列表</summary>
    public ICollection<TransactionCategory> Children { get; set; } = new List<TransactionCategory>();

    /// <summary>交易类型（收入/支出/通用）</summary>
    public TransactionType? TransactionType { get; set; }

    /// <summary>排序序号</summary>
    public int SortOrder { get; set; }

    /// <summary>是否已隐藏</summary>
    public bool IsDeleted { get; set; }

    // 导航属性
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
