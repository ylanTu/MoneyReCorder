using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using MoneyReCorder.Data;
using MoneyReCorder.Services;
using MoneyReCorder.ViewModels;
using MoneyReCorder.Views;

namespace MoneyReCorder;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>打开主窗口的委托（供小组件 / 桌宠调用）</summary>
    public static Action? ShowMainWindow { get; set; }

    /// <summary>打开主窗口并直接进入设置页（浮窗双击 / 设置按钮调用）</summary>
    public static Action? OpenSettings { get; set; }

    private IThemeService? _themeService;
    private MainWindow? _mainWindow;
    private WidgetWindow? _widgetWindow;
    private PetWindow? _petWindow;
    private System.Windows.Forms.NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 全局异常处理 — 记录但不静默吞掉致命错误
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Debug.Fail($"CRASH: {args.ExceptionObject}");
        DispatcherUnhandledException += (_, args) =>
        {
            Debug.WriteLine($"UI ERROR: {args.Exception.Message}\n{args.Exception.StackTrace}");
            // 仅在运行时 XAML 资源缺失等非致命错误时继续，其他情况让异常传播
            if (args.Exception is System.Windows.Markup.XamlParseException)
            {
                Debug.Fail($"XAML Parse Error: {args.Exception.Message}");
            }
            args.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Debug.WriteLine($"TASK ERROR: {args.Exception.Message}");
            args.SetObserved();
        };

        // 注册 DI 容器
        var services = new ServiceCollection();

        // Data
        services.AddSingleton<DatabaseContext>();
        services.AddSingleton<IAccountRepository, AccountRepository>();
        services.AddSingleton<ICategoryRepository, CategoryRepository>();
        services.AddSingleton<ITransactionRepository, TransactionRepository>();

        // Services
        services.AddSingleton<IAccountService, AccountService>();
        services.AddSingleton<ICategoryService, CategoryService>();
        services.AddSingleton<ITransactionService, TransactionService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ISyncService, SyncService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<WidgetViewModel>();
        services.AddTransient<AccountViewModel>();
        services.AddTransient<TransactionViewModel>();
        services.AddTransient<StatisticsViewModel>();
        services.AddTransient<SettingsViewModel>();

        ServiceProvider = services.BuildServiceProvider();

        // 初始化数据库
        var db = ServiceProvider.GetRequiredService<DatabaseContext>();
        db.Initialize();

        // 外观 / 窗口设置
        _themeService = ServiceProvider.GetRequiredService<IThemeService>();

        // 注册打开主窗口回调
        ShowMainWindow = ShowMainWindowCore;
        OpenSettings = OpenSettingsCore;

        // 桌面浮窗（是否显示由设置决定，不再无条件弹出）
        _widgetWindow = new WidgetWindow();

        // 桌宠只有带 Spine 运行时的构建才有形象，否则就是一个和悬浮球重复的空壳
        if (ThemeService.PetAvailable) _petWindow = new PetWindow();

        _themeService.AppearanceChanged += (_, _) => SyncFloatingWindows();
        SyncFloatingWindows();

        // 托盘图标：隐藏全部浮窗后仍可找回入口
        CreateTrayIcon();

    }


    // ═══════════════ 主窗口 ═══════════════

    private void ShowMainWindowCore()
    {
        if (_mainWindow is null)
        {
            _mainWindow = new MainWindow();
            _mainWindow.StateChanged += (_, _) =>
            {
                // 最小化时浮窗重新出现，避免隐藏后无处查看余额
                if (_mainWindow?.WindowState == WindowState.Minimized)
                    RestoreFloatingWindows();
            };
        }

        if (!_mainWindow.IsVisible) _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();

        // 浮窗自动让位，避免遮挡正在使用的窗口
        if (_themeService?.AutoHideFloatingWindows == true)
            HideFloatingWindows();
    }

    /// <summary>打开主窗口并切到设置页（浮窗双击的落地页）</summary>
    private void OpenSettingsCore()
    {
        ShowMainWindowCore();

        if (_mainWindow?.DataContext is MainViewModel vm)
            vm.CurrentPage = "Settings";
    }

    /// <summary>主窗口关闭（隐藏）后由 MainWindow 调用，恢复浮窗</summary>
    public static void NotifyMainWindowHidden()
    {
        if (Current is App app)
            app.RestoreFloatingWindows();
    }

    /// <summary>请求退出应用（浮窗菜单 / 托盘菜单使用）</summary>
    public static void RequestExit()
    {
        if (Current is App app)
            app.ExitApplication();
    }

    // ═══════════════ 浮窗显隐 ═══════════════

    private void SyncFloatingWindows()
    {
        RestoreFloatingWindows();

        // 主窗口正在使用时，浮窗按设置让位
        if (_themeService?.AutoHideFloatingWindows == true && _mainWindow is { IsVisible: true })
            HideFloatingWindows();
    }

    private void RestoreFloatingWindows()
    {
        if (_themeService is null) return;

        SetWindowVisible(_petWindow, _themeService.ShowPet);
        SetWindowVisible(_widgetWindow, _themeService.ShowWidget);
    }

    private void HideFloatingWindows()
    {
        if (_widgetWindow?.IsVisible == true) _widgetWindow.Hide();
        if (_petWindow?.IsVisible == true) _petWindow.Hide();
    }

    private static void SetWindowVisible(Window? window, bool visible)
    {
        if (window is null) return;

        if (visible)
        {
            if (!window.IsVisible) window.Show();
        }
        else if (window.IsVisible)
        {
            window.Hide();
        }
    }

    // ═══════════════ 托盘图标 ═══════════════

    private void CreateTrayIcon()
    {
        try
        {
            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Opening += (_, _) => BuildTrayMenu(menu);

            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Text = "MoneyReCorder",
                ContextMenuStrip = menu,
                Visible = true,
            };
            _trayIcon.DoubleClick += (_, _) => ShowMainWindowCore();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"托盘图标创建失败: {ex.Message}");
        }
    }

    private void BuildTrayMenu(System.Windows.Forms.ContextMenuStrip menu)
    {
        menu.Items.Clear();

        var open = new System.Windows.Forms.ToolStripMenuItem("打开主窗口");
        open.Click += (_, _) => ShowMainWindowCore();
        menu.Items.Add(open);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var widget = new System.Windows.Forms.ToolStripMenuItem("显示桌面小组件")
        {
            Checked = _themeService?.ShowWidget ?? false,
        };
        widget.Click += (_, _) =>
        {
            if (_themeService is null) return;
            _themeService.ShowWidget = !_themeService.ShowWidget;
        };
        menu.Items.Add(widget);

        // 桌宠需要 Spine 运行时，没有资源时不显示这个入口，避免出现第二个悬浮球
        if (ThemeService.PetAvailable)
        {
            var pet = new System.Windows.Forms.ToolStripMenuItem("显示桌宠")
            {
                Checked = _themeService?.ShowPet ?? false,
            };
            pet.Click += (_, _) =>
            {
                if (_themeService is null) return;
                _themeService.ShowPet = !_themeService.ShowPet;
            };
            menu.Items.Add(pet);
        }

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var exit = new System.Windows.Forms.ToolStripMenuItem("退出");
        exit.Click += (_, _) => ExitApplication();
        menu.Items.Add(exit);
    }

    private void ExitApplication()
    {
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        ServiceProvider?.GetService<DatabaseContext>()?.Dispose();
        base.OnExit(e);
    }
}
