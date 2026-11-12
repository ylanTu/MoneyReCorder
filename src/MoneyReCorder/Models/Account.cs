namespace MoneyReCorder.Models;

/// <summary>账户模型（银行卡、微信、支付宝等）</summary>
public class Account
{
    /// <summary>账户ID</summary>
    public int Id { get; set; }

    /// <summary>账户名称，如"招商银行"、"微信零钱"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>账户类型</summary>
    public AccountType Type { get; set; } = AccountType.BankCard;

    /// <summary>当前余额（单位：分，避免浮点数精度问题）</summary>
    public long BalanceInCents { get; set; }

    /// <summary>显示用的余额（由 BalanceInCents 计算）</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public decimal Balance => BalanceInCents / 100m;

    /// <summary>货币代码，当前仅支持 CNY</summary>
    public string Currency { get; set; } = "CNY";

    /// <summary>账户尾号（银行卡后4位）</summary>
    public string? LastFourDigits { get; set; }

    /// <summary>账户图标</summary>
    public string? Icon { get; set; }

    /// <summary>排序序号</summary>
    public int SortOrder { get; set; }

    /// <summary>是否在小组件中显示</summary>
    public bool ShowInWidget { get; set; } = true;

    /// <summary>是否已隐藏（软删除）</summary>
    public bool IsDeleted { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>最后更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // 导航属性
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
