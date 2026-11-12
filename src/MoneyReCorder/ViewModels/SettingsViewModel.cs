using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyReCorder.Models;
using MoneyReCorder.Services;

namespace MoneyReCorder.ViewModels;

/// <summary>
/// 设置页 ViewModel。
/// 所有选项都是「即时生效 + 自动保存」，不需要再点保存按钮。
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private bool _syncing;

    public SettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService;

        Presets = new ObservableCollection<ThemePreset>(themeService.Presets);

        SyncFromService();
    }

    /// <summary>皮肤列表（设置页左侧画廊）</summary>
    public ObservableCollection<ThemePreset> Presets { get; }

    /// <summary>桌宠是否可用（未带 Spine 运行时的构建不显示桌宠开关）</summary>
    public bool CanShowPet => ThemeService.PetAvailable;

    /// <summary>可选主题色</summary>
    public IReadOnlyList<string> PresetColors { get; } = new[]
    {
        "#4CAF50", "#2196F3", "#3F51B5", "#9C27B0",
        "#E91E63", "#FF5722", "#FF9800", "#FFC107",
        "#009688", "#00BCD4", "#607D8B", "#795548",
    };

    [ObservableProperty]
    private ThemePreset? _selectedPreset;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _accentColor = ThemePresets.Default.Accent;

    [ObservableProperty]
    private double _widgetOpacity = 0.92;

    [ObservableProperty]
    private double _mainWindowOpacity = 1.0;

    [ObservableProperty]
    private double _petOpacity = 1.0;

    [ObservableProperty]
    private bool _widgetTopmost;

    [ObservableProperty]
    private bool _petTopmost;

    [ObservableProperty]
    private bool _autoHideFloatingWindows = true;

    [ObservableProperty]
    private bool _showWidget = true;

    [ObservableProperty]
    private bool _showPet = true;

    [ObservableProperty]
    private string _statusMessage = "所有修改即时生效并自动保存";

    // ═══════════════ 悬浮窗形态 ═══════════════

    [ObservableProperty]
    private bool _isModeBall;

    [ObservableProperty]
    private bool _isModeCapsule;

    [ObservableProperty]
    private bool _isModeCard;

    partial void OnIsModeBallChanged(bool value)
    {
        if (value) ApplyMode(WidgetDisplayMode.Ball);
    }

    partial void OnIsModeCapsuleChanged(bool value)
    {
        if (value) ApplyMode(WidgetDisplayMode.Capsule);
    }

    partial void OnIsModeCardChanged(bool value)
    {
        if (value) ApplyMode(WidgetDisplayMode.Card);
    }

    private void ApplyMode(WidgetDisplayMode mode)
    {
        if (_syncing) return;
        if (_themeService.WidgetMode == mode) return;

        _themeService.WidgetMode = mode;
        StatusMessage = mode switch
        {
            WidgetDisplayMode.Ball => "悬浮窗已切换为小球形态（悬停展开，双击进设置）",
            WidgetDisplayMode.Capsule => "悬浮窗已切换为胶囊形态（悬停展开）",
            _ => "悬浮窗已切换为完整卡片形态",
        };
        SyncFromService();
    }


    /// <summary>当前皮肤名（供界面展示）</summary>
    public string CurrentSkinName => SelectedPreset?.Name ?? ThemePresets.Default.Name;

    // ═══════════════ 属性变更 → 写回服务 ═══════════════

    partial void OnSelectedPresetChanged(ThemePreset? value)
    {
        OnPropertyChanged(nameof(CurrentSkinName));

        if (_syncing || value is null) return;

        _themeService.ApplyPreset(value);
        SyncFromService();
        StatusMessage = $"已切换到皮肤「{value.Name}」";
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        if (_syncing) return;

        _themeService.SetDarkMode(value);
        SyncFromService();
        StatusMessage = value ? "已切换到深色皮肤" : "已切换到浅色皮肤";
    }

    partial void OnWidgetOpacityChanged(double value)
    {
        if (_syncing) return;
        _themeService.WidgetOpacity = value;
    }

    partial void OnMainWindowOpacityChanged(double value)
    {
        if (_syncing) return;
        _themeService.MainWindowOpacity = value;
    }

    partial void OnPetOpacityChanged(double value)
    {
        if (_syncing) return;
        _themeService.PetOpacity = value;
    }

    partial void OnWidgetTopmostChanged(bool value)
    {
        if (_syncing) return;
        _themeService.WidgetTopmost = value;
        StatusMessage = value ? "小组件已置顶显示" : "小组件已取消置顶，不再遮挡其他窗口";
    }

    partial void OnPetTopmostChanged(bool value)
    {
        if (_syncing) return;
        _themeService.PetTopmost = value;
    }

    partial void OnAutoHideFloatingWindowsChanged(bool value)
    {
        if (_syncing) return;
        _themeService.AutoHideFloatingWindows = value;
    }

    partial void OnShowWidgetChanged(bool value)
    {
        if (_syncing) return;
        _themeService.ShowWidget = value;
        StatusMessage = value ? "已显示桌面小组件" : "已隐藏桌面小组件";
    }

    partial void OnShowPetChanged(bool value)
    {
        if (_syncing) return;
        _themeService.ShowPet = value;
        StatusMessage = value ? "已显示桌宠" : "已隐藏桌宠";
    }

    // ═══════════════ 命令 ═══════════════

    [RelayCommand]
    private void SetAccentColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) return;

        AccentColor = color;
        _themeService.SetAccent(color);
        StatusMessage = "已更新主题色";
    }

    [RelayCommand]
    private void ResetToDefault()
    {
        _themeService.ResetToDefault();
        SyncFromService();
        StatusMessage = "已恢复默认外观";
    }

    [RelayCommand]
    private void ReloadSettings()
    {
        _themeService.Load();
        _themeService.ApplyPreset(_themeService.CurrentTheme);
        SyncFromService();
        StatusMessage = "已重新载入设置";
    }

    /// <summary>把服务中的真实状态同步到界面（避免循环触发写回）</summary>
    private void SyncFromService()
    {
        _syncing = true;
        try
        {
            SelectedPreset = ThemePresets.Find(_themeService.CurrentTheme) ?? ThemePresets.Default;
            IsDarkMode = _themeService.IsDarkMode;
            AccentColor = _themeService.AccentColor;
            WidgetOpacity = _themeService.WidgetOpacity;
            MainWindowOpacity = _themeService.MainWindowOpacity;
            PetOpacity = _themeService.PetOpacity;
            WidgetTopmost = _themeService.WidgetTopmost;
            PetTopmost = _themeService.PetTopmost;
            AutoHideFloatingWindows = _themeService.AutoHideFloatingWindows;
            ShowWidget = _themeService.ShowWidget;
            ShowPet = _themeService.ShowPet;

            var mode = _themeService.WidgetMode;
            IsModeBall = mode == WidgetDisplayMode.Ball;
            IsModeCapsule = mode == WidgetDisplayMode.Capsule;
            IsModeCard = mode == WidgetDisplayMode.Card;
        }
        finally
        {
            _syncing = false;
        }

        OnPropertyChanged(nameof(CurrentSkinName));
    }
}
