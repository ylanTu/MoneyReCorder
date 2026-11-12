using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using MoneyReCorder.Services;
using MoneyReCorder.ViewModels;

namespace MoneyReCorder.Views;

public partial class MainWindow : Window
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private readonly MainViewModel _viewModel;
    private readonly IThemeService? _themeService;
    private readonly Dictionary<string, UserControl> _pages = new();

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = App.ServiceProvider!.GetRequiredService<MainViewModel>();
        DataContext = _viewModel;

        _themeService = App.ServiceProvider!.GetService<IThemeService>();
        if (_themeService is not null)
        {
            ApplyWindowOpacity();
            _themeService.AppearanceChanged += (_, _) =>
            {
                ApplyWindowOpacity();
                ApplyTitleBarTheme();
            };
        }

        // 注册页面
        _pages["Dashboard"] = PageDashboard;
        _pages["Account"] = PageAccount;
        _pages["Transaction"] = PageTransaction;
        _pages["Statistics"] = PageStatistics;
        _pages["Settings"] = PageSettings;

        // 监听页面切换
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentPage))
                UpdatePageVisibility();
        };

        Loaded += async (_, _) =>
        {
            UpdatePageVisibility();
            try { await _viewModel.LoadDataCommand.ExecuteAsync(null); }
            catch { /* 防止初始化时崩溃 */ }
        };
    }

    /// <summary>主窗口整体透明度（设置页可调，最低 60%）</summary>
    private void ApplyWindowOpacity()
    {
        if (_themeService is null) return;

        double value = Math.Clamp(_themeService.MainWindowOpacity, 0.6, 1.0);
        if (Math.Abs(Opacity - value) > 0.001)
            Opacity = value;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyTitleBarTheme();
    }

    /// <summary>让系统标题栏跟随深浅色，避免深色界面顶上顶一条白条</summary>
    private void ApplyTitleBarTheme()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            int dark = _themeService?.IsDarkMode == true ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeLegacy, ref dark, sizeof(int));
        }
        catch
        {
            /* 老版本 Windows 不支持，忽略即可 */
        }
    }

    private void UpdatePageVisibility()
    {
        foreach (var (key, page) in _pages)
            page.Visibility = key == _viewModel.CurrentPage
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Hide(); // 关闭时隐藏而非退出
        App.NotifyMainWindowHidden();
    }
}
