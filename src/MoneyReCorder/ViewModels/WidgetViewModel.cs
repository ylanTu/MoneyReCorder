using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyReCorder.Models;
using MoneyReCorder.Services;

namespace MoneyReCorder.ViewModels;

/// <summary>
/// 桌面浮窗 ViewModel。
/// 形态有三种：小球（默认）/ 胶囊 / 完整卡片；
/// 小球与胶囊在鼠标悬停时展开为详情卡片，双击则进入设置页。
/// </summary>
public partial class WidgetViewModel : ObservableObject
{
    private readonly IAccountService _accountService;
    private readonly IThemeService _themeService;
    private readonly DispatcherTimer _refreshTimer;

    public WidgetViewModel(IAccountService accountService, IThemeService themeService)
    {
        _accountService = accountService;
        _themeService = themeService;

        // 读取当前外观设置
        WidgetOpacity = _themeService.WidgetOpacity;
        IsTopmost = _themeService.WidgetTopmost;
        Mode = _themeService.WidgetMode;

        // 设置页修改后同步到浮窗
        _themeService.AppearanceChanged += (_, _) =>
        {
            WidgetOpacity = _themeService.WidgetOpacity;
            IsTopmost = _themeService.WidgetTopmost;
            Mode = _themeService.WidgetMode;
        };

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _refreshTimer.Tick += async (_, _) => await SafeRefresh();
        _refreshTimer.Start();

        _ = SafeRefresh();
    }

    // ═══════════════ 数据 ═══════════════

    private decimal _totalBalance;

    [ObservableProperty]
    private string _totalBalanceDisplay = "¥0.00";

    public ObservableCollection<Account> Accounts { get; } = new();

    /// <summary>小球/胶囊上显示的超短金额（如 ¥1.2万），避免窄容器里文字溢出</summary>
    public string CompactBalance
    {
        get
        {
            var abs = Math.Abs(_totalBalance);
            string text = abs switch
            {
                >= 100_000_000m => (_totalBalance / 100_000_000m).ToString("0.#") + "亿",
                >= 10_000m => (_totalBalance / 10_000m).ToString("0.#") + "万",
                _ => _totalBalance.ToString("0"),
            };
            return "¥" + text;
        }
    }

    // ═══════════════ 形态 / 展开状态 ═══════════════

    /// <summary>当前展示形态</summary>
    [ObservableProperty]
    private WidgetDisplayMode _mode = WidgetDisplayMode.Ball;

    /// <summary>是否因鼠标悬停而展开详情</summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>锁定后窗口位置固定，不会再被误拖动</summary>
    [ObservableProperty]
    private bool _isLocked;

    /// <summary>浮窗不透明度（跟随设置页滑块）</summary>
    [ObservableProperty]
    private double _widgetOpacity = 0.92;

    /// <summary>是否始终置顶（跟随设置页开关）</summary>
    [ObservableProperty]
    private bool _isTopmost;

    /// <summary>小球 / 胶囊是否支持悬停展开（完整卡片模式不需要）</summary>
    public bool CanExpand => Mode != WidgetDisplayMode.Card;

    public bool ShowBall => Mode == WidgetDisplayMode.Ball && !IsExpanded;

    public bool ShowCapsule => Mode == WidgetDisplayMode.Capsule && !IsExpanded;

    public bool ShowCard => Mode == WidgetDisplayMode.Card || IsExpanded;

    public bool IsBallMode => Mode == WidgetDisplayMode.Ball;
    public bool IsCapsuleMode => Mode == WidgetDisplayMode.Capsule;
    public bool IsCardMode => Mode == WidgetDisplayMode.Card;

    public string ModeDisplayName => Mode switch
    {
        WidgetDisplayMode.Ball => "小球",
        WidgetDisplayMode.Capsule => "胶囊",
        _ => "完整卡片",
    };

    partial void OnModeChanged(WidgetDisplayMode value)
    {
        IsExpanded = false;
        RaiseLayoutFlags();
    }

    partial void OnIsExpandedChanged(bool value) => RaiseLayoutFlags();

    /// <summary>通知所有依赖「形态 + 展开状态」的派生属性</summary>
    private void RaiseLayoutFlags()
    {
        OnPropertyChanged(nameof(CanExpand));
        OnPropertyChanged(nameof(ShowBall));
        OnPropertyChanged(nameof(ShowCapsule));
        OnPropertyChanged(nameof(ShowCard));
        OnPropertyChanged(nameof(IsBallMode));
        OnPropertyChanged(nameof(IsCapsuleMode));
        OnPropertyChanged(nameof(IsCardMode));
        OnPropertyChanged(nameof(ModeDisplayName));
    }

    // ═══════════════ 命令 ═══════════════

    [RelayCommand]
    private void ToggleLock() => IsLocked = !IsLocked;

    [RelayCommand]
    private void ToggleTopmost() => _themeService.WidgetTopmost = !_themeService.WidgetTopmost;

    /// <summary>切换浮窗形态（右键菜单 / 卡片按钮使用）</summary>
    [RelayCommand]
    private void SetMode(string? mode)
    {
        if (!Enum.TryParse<WidgetDisplayMode>(mode, ignoreCase: true, out var parsed)) return;
        _themeService.WidgetMode = parsed;
    }

    /// <summary>在三种形态之间循环切换</summary>
    [RelayCommand]
    private void CycleMode()
    {
        var next = Mode switch
        {
            WidgetDisplayMode.Ball => WidgetDisplayMode.Capsule,
            WidgetDisplayMode.Capsule => WidgetDisplayMode.Card,
            _ => WidgetDisplayMode.Ball,
        };
        _themeService.WidgetMode = next;
    }

    [RelayCommand]
    private void HideWidget() => _themeService.ShowWidget = false;

    [RelayCommand]
    private async Task LoadWidgetDataAsync()
    {
        try
        {
            var accounts = await _accountService.GetWidgetAccountsAsync();
            Accounts.Clear();
            foreach (var a in accounts)
                Accounts.Add(a);

            var total = await _accountService.GetTotalBalanceAsync();
            _totalBalance = total;
            TotalBalanceDisplay = $"¥{total:N2}";
            OnPropertyChanged(nameof(CompactBalance));
        }
        catch
        {
            /* 静默处理，避免浮窗因数据异常崩溃 */
        }
    }

    private async Task SafeRefresh()
    {
        try { await LoadWidgetDataAsync(); }
        catch { /* 静默处理，防止定时器异常崩溃 */ }
    }

    [RelayCommand]
    private void OpenMainWindow()
    {
        App.ShowMainWindow?.Invoke();
    }

    /// <summary>打开主窗口并直接进入设置页（双击浮窗触发）</summary>
    [RelayCommand]
    private void OpenSettings()
    {
        App.OpenSettings?.Invoke();
    }

    [RelayCommand]
    private void ExitApp()
    {
        App.RequestExit();
    }
}
