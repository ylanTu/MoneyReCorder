using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using MoneyReCorder.Services;
using MoneyReCorder.ViewModels;

namespace MoneyReCorder.Views;

/// <summary>
/// 桌面浮窗。
/// 默认是一个小球，鼠标悬停展开为详情卡片，离开后自动收起，双击进入设置页。
/// 三种形态（小球 / 胶囊 / 完整卡片）由 WidgetViewModel.Mode 驱动，窗口尺寸随内容变化，
/// 但无论展开还是收起，小球在屏幕上的落点都保持不变。
/// </summary>
public partial class WidgetWindow : Window
{
    private readonly WidgetViewModel _viewModel;
    private readonly IThemeService? _themeService;
    private readonly DispatcherTimer _collapseTimer;

    /// <summary>收起状态下窗口中心点的屏幕坐标（拖动 / 展开都围绕它计算）</summary>
    private Point _anchor;

    /// <summary>收起状态的窗口尺寸，用于展开时对齐角落</summary>
    private Size _collapsedSize = new(68, 68);

    private Point _dragStart;
    private bool _dragPrimed;

    public WidgetWindow()
    {
        InitializeComponent();

        _viewModel = App.ServiceProvider!.GetRequiredService<WidgetViewModel>();
        _themeService = App.ServiceProvider!.GetService<IThemeService>();
        DataContext = _viewModel;

        _collapseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        _collapseTimer.Tick += OnCollapseTimerTick;

        // 先放到屏幕外，避免首次显示时闪一下左上角
        Left = -10000;
        Top = -10000;

        MouseEnter += OnWindowMouseEnter;
        MouseLeave += OnWindowMouseLeave;
        MouseLeftButtonDown += OnWindowMouseLeftButtonDown;
        MouseMove += OnWindowMouseMove;
        IsVisibleChanged += OnIsVisibleChanged;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        Loaded += async (_, _) =>
        {
            RestoreAnchor();
            ApplyLayout();
            await _viewModel.LoadWidgetDataCommand.ExecuteAsync(null);
        };
    }

    // ═══════════════ 定位 ═══════════════

    /// <summary>恢复上次的落点；没有记录时放在右下角</summary>
    private void RestoreAnchor()
    {
        if (_themeService?.WidgetAnchorX is double x && _themeService.WidgetAnchorY is double y)
        {
            _anchor = new Point(x, y);
            return;
        }

        var workArea = SystemParameters.WorkArea;
        _anchor = new Point(workArea.Right - 26 - 34, workArea.Bottom - 64 - 34);
    }

    /// <summary>
    /// 按当前尺寸摆放窗口：收起时窗口中心对准锚点；
    /// 展开时让收起状态的矩形（即小球）贴在最近的屏幕角落，视觉上小球不动、卡片从它旁边长出来。
    /// </summary>
    private void ApplyLayout()
    {
        UpdateLayout();

        double w = ActualWidth > 0 ? ActualWidth : DesiredSize.Width;
        double h = ActualHeight > 0 ? ActualHeight : DesiredSize.Height;
        if (w <= 0 || h <= 0) return;

        var workArea = SystemParameters.WorkArea;

        double left, top;
        if (_viewModel.IsExpanded)
        {
            bool rightSide = _anchor.X >= workArea.Left + workArea.Width / 2;
            bool bottomSide = _anchor.Y >= workArea.Top + workArea.Height / 2;

            double ballLeft = _anchor.X - _collapsedSize.Width / 2;
            double ballTop = _anchor.Y - _collapsedSize.Height / 2;

            left = rightSide ? ballLeft + _collapsedSize.Width - w : ballLeft;
            top = bottomSide ? ballTop + _collapsedSize.Height - h : ballTop;
        }
        else
        {
            left = _anchor.X - w / 2;
            top = _anchor.Y - h / 2;
            _collapsedSize = new Size(w, h);
        }

        left = Math.Clamp(left, workArea.Left + 2, Math.Max(workArea.Left + 2, workArea.Right - w - 2));
        top = Math.Clamp(top, workArea.Top + 2, Math.Max(workArea.Top + 2, workArea.Bottom - h - 2));

        Left = left;
        Top = top;
    }

    // ═══════════════ 悬停展开 / 收起 ═══════════════

    private void OnWindowMouseEnter(object sender, MouseEventArgs e)
    {
        _collapseTimer.Stop();
        if (!_viewModel.CanExpand) return;
        SetExpanded(true);
    }

    private void OnWindowMouseLeave(object sender, MouseEventArgs e)
    {
        if (!_viewModel.CanExpand) return;

        _collapseTimer.Stop();
        _collapseTimer.Start();
    }

    private void OnCollapseTimerTick(object? sender, EventArgs e)
    {
        _collapseTimer.Stop();

        // 鼠标可能仍停在卡片上（例如移到了按钮之间），此时不收起
        if (IsMouseOver) return;
        SetExpanded(false);
    }

    private void SetExpanded(bool expanded)
    {
        if (_viewModel.IsExpanded == expanded) return;
        if (expanded && !_viewModel.CanExpand) return;

        _viewModel.IsExpanded = expanded;
        ApplyLayout();

        if (expanded)
        {
            CardHost.BeginAnimation(OpacityProperty, null);
            CardHost.Opacity = 0;
            CardHost.BeginAnimation(OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(130))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                });
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // 形态切换（小球↔胶囊↔卡片）后尺寸变了，需要重新摆位
        if (e.PropertyName is nameof(WidgetViewModel.ShowCard)
            or nameof(WidgetViewModel.ShowBall)
            or nameof(WidgetViewModel.ShowCapsule))
        {
            Dispatcher.BeginInvoke(new Action(ApplyLayout), DispatcherPriority.Loaded);
        }
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            Dispatcher.BeginInvoke(new Action(ApplyLayout), DispatcherPriority.Loaded);
        }
        else
        {
            _collapseTimer.Stop();
            _viewModel.IsExpanded = false;
        }
    }

    // ═══════════════ 拖动 / 双击 ═══════════════

    private void OnWindowMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 双击 → 打开设置页
        if (e.ClickCount >= 2)
        {
            _dragPrimed = false;
            e.Handled = true;
            _collapseTimer.Stop();
            _viewModel.OpenSettingsCommand.Execute(null);
            return;
        }

        _dragPrimed = false;

        if (_viewModel.IsLocked) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        // 点在按钮/菜单上时交给控件自己处理
        if (e.OriginalSource is DependencyObject source && FindAncestor<ButtonBase>(source) is not null)
            return;

        _dragStart = e.GetPosition(this);
        _dragPrimed = true;
    }

    /// <summary>移动超过阈值才真正拖动，这样单击不会和双击冲突</summary>
    private void OnWindowMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragPrimed) return;

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            _dragPrimed = false;
            return;
        }

        var p = e.GetPosition(this);
        if (Math.Abs(p.X - _dragStart.X) < 4 && Math.Abs(p.Y - _dragStart.Y) < 4) return;

        _dragPrimed = false;

        double beforeLeft = Left;
        double beforeTop = Top;

        try { DragMove(); }
        catch (InvalidOperationException) { return; }

        double dx = Left - beforeLeft;
        double dy = Top - beforeTop;
        if (Math.Abs(dx) < 0.5 && Math.Abs(dy) < 0.5) return;

        _anchor = new Point(_anchor.X + dx, _anchor.Y + dy);
        ApplyLayout();
        _themeService?.SaveWidgetAnchor(_anchor.X, _anchor.Y);
    }

    private void OpenMenu_Click(object sender, RoutedEventArgs e)
    {
        if (RootGrid.ContextMenu is not { } menu) return;

        menu.PlacementTarget = sender as UIElement ?? RootGrid;
        menu.Placement = PlacementMode.Bottom;
        menu.DataContext = DataContext;
        menu.IsOpen = true;
    }

    private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node is not null)
        {
            if (node is T target) return target;

            node = node is System.Windows.Media.Visual || node is System.Windows.Media.Media3D.Visual3D
                ? System.Windows.Media.VisualTreeHelper.GetParent(node)
                : LogicalTreeHelper.GetParent(node);
        }
        return null;
    }
}
