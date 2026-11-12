using System.Windows.Media;

namespace MoneyReCorder.Services;

/// <summary>
/// 皮肤预设：一套完整的配色方案（主题色 + 背景 + 卡片 + 文字 + 边框）。
/// 与「主题色」不同，皮肤会同时改变明暗基调，保证整体观感统一。
/// </summary>
public sealed class ThemePreset
{
    private static readonly BrushConverter BrushFactory = new();

    private Brush? _accentBrush;
    private Brush? _backgroundBrush;
    private Brush? _cardBrush;
    private Brush? _surfaceBrush;
    private Brush? _textBrush;
    private Brush? _borderBrush;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>是否为深色系皮肤</summary>
    public bool IsDark { get; init; }

    /// <summary>明暗配对的皮肤名（用于深色模式开关联动）</summary>
    public string PartnerName { get; init; } = string.Empty;

    public string Accent { get; init; } = "#4CAF50";

    /// <summary>窗口背景色（可含 Alpha）</summary>
    public string BgPrimary { get; init; } = "#F8F9FB";

    /// <summary>卡片/面板背景色（可含 Alpha，用于玻璃质感）</summary>
    public string BgCard { get; init; } = "#FFFFFF";

    /// <summary>列表行、悬停等次级表面色</summary>
    public string Surface { get; init; } = "#F3F5F7";

    public string TextPrimary { get; init; } = "#1A1A2E";

    public string TextSecondary { get; init; } = "#6B7280";

    public string Border { get; init; } = "#E5E7EB";

    // ── 供 XAML 直接绑定的画刷（用于皮肤选择卡片的预览） ──
    public Brush AccentBrush => _accentBrush ??= Create(Accent);
    public Brush BackgroundBrush => _backgroundBrush ??= Create(BgPrimary);
    public Brush CardBrush => _cardBrush ??= Create(BgCard);
    public Brush SurfaceBrush => _surfaceBrush ??= Create(Surface);
    public Brush TextBrush => _textBrush ??= Create(TextPrimary);
    public Brush BorderBrush => _borderBrush ??= Create(Border);

    private static Brush Create(string hex)
    {
        try
        {
            if (BrushFactory.ConvertFromString(hex) is SolidColorBrush brush)
            {
                brush.Freeze();
                return brush;
            }
        }
        catch
        {
            // 忽略非法色值，退回透明
        }

        var fallback = new SolidColorBrush(Colors.Transparent);
        fallback.Freeze();
        return fallback;
    }

    public override string ToString() => Name;
}

/// <summary>内置皮肤库</summary>
public static class ThemePresets
{
    /// <summary>全部皮肤（浅色在前、深色在后，按明暗成对排列）</summary>
    public static IReadOnlyList<ThemePreset> All { get; } = new List<ThemePreset>
    {
        new()
        {
            Name = "薄荷晨光", Description = "清爽绿意 · 浅色", IsDark = false, PartnerName = "森林夜曲",
            Accent = "#4CAF50",
            BgPrimary = "#F6FAF7", BgCard = "#FCFFFFFF", Surface = "#EDF4EF",
            TextPrimary = "#16241B", TextSecondary = "#64756A", Border = "#DEE9E1",
        },
        new()
        {
            Name = "森林夜曲", Description = "清爽绿意 · 深色", IsDark = true, PartnerName = "薄荷晨光",
            Accent = "#57D68A",
            BgPrimary = "#121614", BgCard = "#FA1A211D", Surface = "#222B25",
            TextPrimary = "#E6EDE8", TextSecondary = "#93A79B", Border = "#2B3630",
        },
        new()
        {
            Name = "海洋微澜", Description = "沉静蓝调 · 浅色", IsDark = false, PartnerName = "深海夜蓝",
            Accent = "#2196F3",
            BgPrimary = "#F4F8FD", BgCard = "#FCFFFFFF", Surface = "#E9F1FA",
            TextPrimary = "#14213A", TextSecondary = "#5E6E86", Border = "#D9E5F3",
        },
        new()
        {
            Name = "深海夜蓝", Description = "沉静蓝调 · 深色", IsDark = true, PartnerName = "海洋微澜",
            Accent = "#3DA5F5",
            BgPrimary = "#0F1620", BgCard = "#FA17202C", Surface = "#1F2937",
            TextPrimary = "#E3EBF5", TextSecondary = "#93A2B8", Border = "#26313E",
        },
        new()
        {
            Name = "樱花初雪", Description = "柔和粉白 · 浅色", IsDark = false, PartnerName = "玫瑰暮色",
            Accent = "#E91E63",
            BgPrimary = "#FDF5F8", BgCard = "#FCFFFFFF", Surface = "#FAEAF0",
            TextPrimary = "#2B1820", TextSecondary = "#7C6570", Border = "#F2DDE5",
        },
        new()
        {
            Name = "玫瑰暮色", Description = "柔和粉白 · 深色", IsDark = true, PartnerName = "樱花初雪",
            Accent = "#F06292",
            BgPrimary = "#191114", BgCard = "#FA22181C", Surface = "#2C2025",
            TextPrimary = "#F4E8EE", TextSecondary = "#B19AA4", Border = "#372830",
        },
        new()
        {
            Name = "日暮琥珀", Description = "温暖橘调 · 浅色", IsDark = false, PartnerName = "琥珀余晖",
            Accent = "#FF9800",
            BgPrimary = "#FDF8F2", BgCard = "#FCFFFFFF", Surface = "#F8EFE2",
            TextPrimary = "#251C12", TextSecondary = "#7C6B56", Border = "#F0E1CC",
        },
        new()
        {
            Name = "琥珀余晖", Description = "温暖橘调 · 深色", IsDark = true, PartnerName = "日暮琥珀",
            Accent = "#FFB74D",
            BgPrimary = "#17130E", BgCard = "#FA201B14", Surface = "#2A231A",
            TextPrimary = "#F5EDE1", TextSecondary = "#B3A38C", Border = "#352C21",
        },
        new()
        {
            Name = "极简纸白", Description = "极简素白 · 浅色", IsDark = false, PartnerName = "玄墨石墨",
            Accent = "#3F51B5",
            BgPrimary = "#FBFBFD", BgCard = "#FCFFFFFF", Surface = "#F1F2F7",
            TextPrimary = "#14161F", TextSecondary = "#666B7A", Border = "#E3E5EC",
        },
        new()
        {
            Name = "玄墨石墨", Description = "极简素白 · 深色", IsDark = true, PartnerName = "极简纸白",
            Accent = "#6366F1",
            BgPrimary = "#0E1014", BgCard = "#FA16181E", Surface = "#1E2129",
            TextPrimary = "#E7EAF0", TextSecondary = "#98A0B0", Border = "#23262F",
        },
    };

    public static ThemePreset Default => All[0];

    public static ThemePreset? Find(string? name)
        => string.IsNullOrWhiteSpace(name)
            ? null
            : All.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.Ordinal));

    /// <summary>取明暗配对的皮肤（不存在则返回 null）</summary>
    public static ThemePreset? PartnerOf(string? name)
    {
        var current = Find(name);
        return current is null ? null : Find(current.PartnerName);
    }
}
