namespace MoneyReCorder.Models;

/// <summary>账户类型</summary>
public enum AccountType
{
    /// <summary>银行卡（借记卡）</summary>
    BankCard = 0,

    /// <summary>信用卡</summary>
    CreditCard = 1,

    /// <summary>微信</summary>
    WeChat = 2,

    /// <summary>支付宝</summary>
    Alipay = 3,

    /// <summary>现金</summary>
    Cash = 4,

    /// <summary>其他</summary>
    Other = 5
}

/// <summary>交易类型</summary>
public enum TransactionType
{
    /// <summary>收入</summary>
    Income = 0,

    /// <summary>支出</summary>
    Expense = 1
}

/// <summary>桌面浮窗的展示形态</summary>
public enum WidgetDisplayMode
{
    /// <summary>小球（默认）：只显示总资产，鼠标悬停时展开完整详情</summary>
    Ball = 0,

    /// <summary>胶囊：横向小长条，同样悬停展开</summary>
    Capsule = 1,

    /// <summary>完整卡片：始终展示全部信息</summary>
    Card = 2
}

/// <summary>重复类型</summary>
public enum RepeatType
{
    /// <summary>不重复</summary>
    None = 0,

    /// <summary>每日</summary>
    Daily = 1,

    /// <summary>每周</summary>
    Weekly = 2,

    /// <summary>每月</summary>
    Monthly = 3,

    /// <summary>每年</summary>
    Yearly = 4
}
