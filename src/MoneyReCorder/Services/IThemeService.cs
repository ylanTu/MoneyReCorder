using MoneyReCorder.Models;

namespace MoneyReCorder.Services;

/// <summary>
/// 外观 / 窗口设置服务接口。
/// 所有属性修改都会即时生效、自动持久化，并触发 <see cref="AppearanceChanged"/>。
/// </summary>
public interface IThemeService
{
    /// <summary>外观或窗口行为发生变化（已应用到 Application.Resources）</summary>
    event EventHandler? AppearanceChanged;

    /// <summary>内置皮肤库</summary>
    IReadOnlyList<ThemePreset> Presets { get; }

    /// <summary>当前皮肤名称</summary>
    string CurrentTheme { get; }

    /// <summary>当前是否为深色皮肤</summary>
    bool IsDarkMode { get; }

    /// <summary>主题色（强调色）</summary>
    string AccentColor { get; }

    /// <summary>桌面小组件不透明度</summary>
    double WidgetOpacity { get; set; }

    /// <summary>主窗口整体不透明度</summary>
    double MainWindowOpacity { get; set; }

    /// <summary>桌宠不透明度</summary>
    double PetOpacity { get; set; }

    /// <summary>小组件是否总是置顶</summary>
    bool WidgetTopmost { get; set; }

    /// <summary>桌宠是否总是置顶</summary>
    bool PetTopmost { get; set; }

    /// <summary>打开主窗口时自动隐藏桌面浮窗</summary>
    bool AutoHideFloatingWindows { get; set; }

    /// <summary>是否显示桌面小组件</summary>
    bool ShowWidget { get; set; }

    /// <summary>是否显示桌宠</summary>
    bool ShowPet { get; set; }

    /// <summary>桌面浮窗的展示形态（小球 / 胶囊 / 完整卡片）</summary>
    WidgetDisplayMode WidgetMode { get; set; }

    /// <summary>浮窗锚点 X（屏幕坐标，收起状态下的窗口中心；null 表示自动放在右下角）</summary>
    double? WidgetAnchorX { get; }

    /// <summary>浮窗锚点 Y（屏幕坐标）</summary>
    double? WidgetAnchorY { get; }

    /// <summary>保存浮窗位置（拖动结束后调用，不触发外观变更通知）</summary>
    void SaveWidgetAnchor(double x, double y);

    /// <summary>应用整套皮肤</summary>
    void ApplyPreset(ThemePreset preset);

    /// <summary>按名称应用皮肤</summary>
    void ApplyPreset(string name);

    /// <summary>单独更换主题色（叠加在当前皮肤上）</summary>
    void SetAccent(string hexColor);

    /// <summary>切换深色模式（自动切换到配对的深/浅皮肤）</summary>
    void SetDarkMode(bool isDark);

    /// <summary>加载设置</summary>
    void Load();

    /// <summary>保存设置</summary>
    void Save();

    /// <summary>恢复默认外观与窗口设置</summary>
    void ResetToDefault();
}
