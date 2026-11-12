using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MoneyReCorder.Models;

namespace MoneyReCorder.Services;

/// <summary>
/// 皮肤 / 窗口设置服务。
/// 负责：皮肤配色热切换、主题色、窗口透明度与浮窗行为的持久化。
/// </summary>
public class ThemeService : IThemeService
{
    private readonly string _settingsPath;
    private readonly DispatcherTimer? _saveTimer;
    private SettingsData _data = new();

    public event EventHandler? AppearanceChanged;

    public IReadOnlyList<ThemePreset> Presets => ThemePresets.All;

    public string CurrentTheme => _data.Theme;

    public bool IsDarkMode => _data.IsDarkMode;

    public string AccentColor => _data.AccentColor;

    public double WidgetOpacity
    {
        get => _data.WidgetOpacity;
        set => SetDouble(() => _data.WidgetOpacity, v => _data.WidgetOpacity = v, value, 0.2, 1.0);
    }

    public double MainWindowOpacity
    {
        get => _data.MainWindowOpacity;
        set => SetDouble(() => _data.MainWindowOpacity, v => _data.MainWindowOpacity = v, value, 0.6, 1.0);
    }

    public double PetOpacity
    {
        get => _data.PetOpacity;
        set => SetDouble(() => _data.PetOpacity, v => _data.PetOpacity = v, value, 0.2, 1.0);
    }

    public bool WidgetTopmost
    {
        get => _data.WidgetTopmost;
        set => SetBool(() => _data.WidgetTopmost, v => _data.WidgetTopmost = v, value);
    }

    public bool PetTopmost
    {
        get => _data.PetTopmost;
        set => SetBool(() => _data.PetTopmost, v => _data.PetTopmost = v, value);
    }

    public bool AutoHideFloatingWindows
    {
        get => _data.AutoHideFloatingWindows;
        set => SetBool(() => _data.AutoHideFloatingWindows, v => _data.AutoHideFloatingWindows = v, value);
    }

    public bool ShowWidget
    {
        get => _data.ShowWidget;
        set => SetBool(() => _data.ShowWidget, v => _data.ShowWidget = v, value);
    }

    public bool ShowPet
    {
        get => _data.ShowPet && PetAvailable;
        set => SetBool(() => _data.ShowPet, v => _data.ShowPet = v && PetAvailable, value && PetAvailable);
    }

    public WidgetDisplayMode WidgetMode
    {
        get => _data.WidgetMode;
        set
        {
            if (_data.WidgetMode == value) return;
            _data.WidgetMode = value;
            NotifyChanged();
        }
    }

    public double? WidgetAnchorX => _data.WidgetAnchorX;

    public double? WidgetAnchorY => _data.WidgetAnchorY;

    /// <summary>拖动结束后记录浮窗位置：直接写盘，不触发外观变更（避免浮窗被反复重建）</summary>
    public void SaveWidgetAnchor(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y)) return;

        bool unchanged = _data.WidgetAnchorX is double ox && _data.WidgetAnchorY is double oy
                         && Math.Abs(ox - x) < 0.5 && Math.Abs(oy - y) < 0.5;
        if (unchanged) return;

        _data.WidgetAnchorX = x;
        _data.WidgetAnchorY = y;
        Save();
    }

    public ThemeService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "MoneyReCorder");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");

        // 滑块连续拖动时避免频繁写盘：350ms 防抖
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null)
        {
            _saveTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(350),
            };
            _saveTimer.Tick += (_, _) =>
            {
                _saveTimer.Stop();
                Save();
            };
        }

        Load();
        ApplyAllColors();
    }

    // ══════════════════════ 皮肤 / 配色 ══════════════════════

    public void ApplyPreset(ThemePreset preset)
    {
        if (preset is null) return;

        _data.Theme = preset.Name;
        _data.IsDarkMode = preset.IsDark;
        _data.AccentColor = preset.Accent;

        ApplyAllColors();
        NotifyChanged();
    }

    public void ApplyPreset(string name)
    {
        var preset = ThemePresets.Find(name) ?? ThemePresets.Default;
        ApplyPreset(preset);
    }

    public void SetAccent(string hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor)) return;

        _data.AccentColor = hexColor;
        ApplyAllColors();
        NotifyChanged();
    }

    public void SetDarkMode(bool isDark)
    {
        if (_data.IsDarkMode == isDark) return;

        // 优先切换到成对的深/浅皮肤，保证背景与文字仍然协调
        var partner = ThemePresets.PartnerOf(_data.Theme);
        if (partner is not null && partner.IsDark == isDark)
        {
            ApplyPreset(partner);
            return;
        }

        var fallback = isDark
            ? ThemePresets.All.FirstOrDefault(p => p.IsDark) ?? ThemePresets.Default
            : ThemePresets.All.FirstOrDefault(p => !p.IsDark) ?? ThemePresets.Default;

        ApplyPreset(fallback);
    }

    public void ResetToDefault()
    {
        _data = new SettingsData();
        ApplyAllColors();
        NotifyChanged();
        Save();
    }

    // ══════════════════════ 持久化 ══════════════════════

    public void Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;

            var json = File.ReadAllText(_settingsPath);
            var loaded = JsonSerializer.Deserialize<SettingsData>(json);
            if (loaded is null) return;

            _data = loaded;
            Normalize();
        }
        catch
        {
            _data = new SettingsData();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            /* 设置保存失败不应影响主流程 */
        }
    }

    /// <summary>修正历史配置（旧版本皮肤名、越界数值等）</summary>
    private void Normalize()
    {
        // 旧版本的皮肤名（薄荷绿/海洋蓝…）不在新皮肤库中 → 回退默认皮肤，但保留用户自定义的主题色
        var preset = ThemePresets.Find(_data.Theme);
        if (preset is null)
        {
            var keepAccent = IsValidColor(_data.AccentColor);
            _data.Theme = ThemePresets.Default.Name;
            _data.IsDarkMode = ThemePresets.Default.IsDark;
            if (!keepAccent) _data.AccentColor = ThemePresets.Default.Accent;
        }
        else
        {
            _data.IsDarkMode = preset.IsDark;
            if (!IsValidColor(_data.AccentColor)) _data.AccentColor = preset.Accent;
        }

        _data.WidgetOpacity = Math.Clamp(_data.WidgetOpacity, 0.2, 1.0);
        _data.MainWindowOpacity = Math.Clamp(_data.MainWindowOpacity, 0.6, 1.0);
        _data.PetOpacity = Math.Clamp(_data.PetOpacity, 0.2, 1.0);

        // 没有 Spine 运行时就没有桌宠形象，旧配置里残留的 ShowPet=true 会变成一个多余的小球
        if (!PetAvailable) _data.ShowPet = false;
    }

    /// <summary>当前构建是否带 Spine 运行时（决定桌宠能否显示）</summary>
    public static bool PetAvailable
    {
#if SPINE_ENABLED
        get => true;
#else
        get => false;
#endif
    }

    private static bool IsValidColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return false;
        try
        {
            ColorConverter.ConvertFromString(hex);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ══════════════════════ 应用配色到全局资源 ══════════════════════

    private void ApplyAllColors()
    {
        var dict = Application.Current?.Resources;
        if (dict is null) return;

        var preset = ThemePresets.Find(_data.Theme) ?? ThemePresets.Default;

        var accent = ParseColor(_data.AccentColor, ParseColor(preset.Accent, Colors.Green));
        var bgPrimary = ParseColor(preset.BgPrimary, Colors.White);
        var bgCard = ParseColor(preset.BgCard, Colors.White);
        var surface = ParseColor(preset.Surface, Colors.WhiteSmoke);
        var textPrimary = ParseColor(preset.TextPrimary, Colors.Black);
        var textSecondary = ParseColor(preset.TextSecondary, Colors.Gray);
        var border = ParseColor(preset.Border, Colors.LightGray);

        // 主题色派生：深色用于强调，浅色用于选中背景（向背景色靠拢，深浅主题都好看）
        var accentDark = Blend(accent, Colors.Black, 0.20);
        var accentLight = Blend(accent, WithOpaqueAlpha(bgPrimary), 0.86);
        var textTertiary = Blend(textSecondary, WithOpaqueAlpha(bgPrimary), 0.45);

        Apply(dict, "PrimaryColor", "PrimaryBrush", accent);
        Apply(dict, "PrimaryDarkColor", "PrimaryDarkBrush", accentDark);
        Apply(dict, "PrimaryLightColor", "PrimaryLightBrush", accentLight);
        Apply(dict, "IncomeColor", "IncomeBrush", accent);

        Apply(dict, "TextPrimaryColor", "TextPrimaryBrush", textPrimary);
        Apply(dict, "TextSecondaryColor", "TextSecondaryBrush", textSecondary);
        Apply(dict, "TextTertiaryColor", "TextTertiaryBrush", textTertiary);

        Apply(dict, "BgPrimaryColor", "BgPrimaryBrush", bgPrimary);
        Apply(dict, "BgCardColor", "BgCardBrush", bgCard);
        Apply(dict, "SurfaceColor", "SurfaceBrush", surface);
        Apply(dict, "BorderColor", "BorderBrush", border);
    }

    /// <summary>
    /// 同时更新 Color 资源与对应画刷的颜色。
    /// 只改 Color 资源是不够的：Themes.xaml 里的画刷是在资源字典内以
    /// DynamicResource 声明颜色的 Freezable，不会跟随资源变化重算，
    /// 因此必须直接改写画刷实例（StaticResource 引用也会一起生效）。
    /// </summary>
    private static void Apply(ResourceDictionary dict, string colorKey, string brushKey, Color color)
    {
        dict[colorKey] = color;

        if (dict[brushKey] is SolidColorBrush brush)
        {
            if (!brush.IsFrozen)
                brush.Color = color;
            else
                dict[brushKey] = new SolidColorBrush(color);
        }
    }

    private static Color WithOpaqueAlpha(Color color) => Color.FromRgb(color.R, color.G, color.B);

    private static Color ParseColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try
        {
            if (ColorConverter.ConvertFromString(hex) is Color c) return c;
        }
        catch
        {
            /* ignore */
        }
        return fallback;
    }

    private static Color Blend(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            from.A,
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));
    }

    // ══════════════════════ 变更通知 ══════════════════════

    private void SetDouble(Func<double> read, Action<double> write, double value, double min, double max)
    {
        var next = Math.Clamp(value, min, max);
        if (Math.Abs(next - read()) < 0.0005) return;

        write(next);
        NotifyChanged();
    }

    private void SetBool(Func<bool> read, Action<bool> write, bool value)
    {
        if (read() == value) return;

        write(value);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        if (_saveTimer is not null)
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }
        else
        {
            Save();
        }

        AppearanceChanged?.Invoke(this, EventArgs.Empty);
    }

    private class SettingsData
    {
        public string Theme { get; set; } = ThemePresets.Default.Name;
        public string AccentColor { get; set; } = ThemePresets.Default.Accent;
        public bool IsDarkMode { get; set; }
        public double WidgetOpacity { get; set; } = 0.92;
        public double MainWindowOpacity { get; set; } = 1.0;
        public double PetOpacity { get; set; } = 1.0;
        public bool WidgetTopmost { get; set; }
        public bool PetTopmost { get; set; }
        public bool AutoHideFloatingWindows { get; set; } = true;
        public bool ShowWidget { get; set; } = true;

        /// <summary>桌宠需要 Spine 运行时，默认关闭，避免和悬浮球重复</summary>
        public bool ShowPet { get; set; }

        /// <summary>浮窗形态（字符串形式写入，方便手动改配置）</summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public WidgetDisplayMode WidgetMode { get; set; } = WidgetDisplayMode.Ball;

        /// <summary>浮窗锚点（收起状态的窗口中心，屏幕坐标）</summary>
        public double? WidgetAnchorX { get; set; }
        public double? WidgetAnchorY { get; set; }
    }
}
