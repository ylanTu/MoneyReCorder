using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MoneyReCorder.Services;

namespace MoneyReCorder.Views;

public partial class PetWindow : Window
{
    private readonly IThemeService _themeService;

    public PetWindow()
    {
        InitializeComponent();

        _themeService = App.ServiceProvider!.GetRequiredService<IThemeService>();
        ApplyAppearance();
        _themeService.AppearanceChanged += (_, _) => ApplyAppearance();

        Loaded += (_, _) => PlaceAtBottomLeft();

        MouseLeftButtonDown += OnPetMouseDown;
        MouseDoubleClick += OnPetDoubleClick;

#if SPINE_ENABLED
        MouseEnter += (_, _) =>
        {
            if (SpineControl.IsLoaded)
                SpineControl.Renderer.TransitionToAnimation("happy", false, 0.2f);
        };
        MouseDown += (_, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed && SpineControl.IsLoaded)
                SpineControl.Renderer.TransitionToAnimation("touch", false, 0.1f);
        };
#endif
    }

    /// <summary>跟随设置：透明度、是否置顶</summary>
    private void ApplyAppearance()
    {
        double opacity = Math.Clamp(_themeService.PetOpacity, 0.2, 1.0);
        if (Math.Abs(Opacity - opacity) > 0.001)
            Opacity = opacity;

        if (Topmost != _themeService.PetTopmost)
            Topmost = _themeService.PetTopmost;
    }

    private void PlaceAtBottomLeft()
    {
        var workArea = SystemParameters.WorkArea;
        Left = 20;
        Top = workArea.Bottom - ActualHeight - 40;
    }

    private void OnPetMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (e.ClickCount >= 2) return; // 双击交给 MouseDoubleClick

        try { DragMove(); }
        catch (InvalidOperationException) { /* 忽略拖动期间的窗口状态变化 */ }
    }

    private void OnPetDoubleClick(object sender, MouseButtonEventArgs e)
    {
        App.ShowMainWindow?.Invoke();
    }

    private void OpenMainWindow_Click(object sender, RoutedEventArgs e)
        => App.ShowMainWindow?.Invoke();

    private void HidePet_Click(object sender, RoutedEventArgs e)
        => _themeService.ShowPet = false;

    private void ExitApp_Click(object sender, RoutedEventArgs e)
        => App.RequestExit();

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
#if SPINE_ENABLED
        SpineControl.Stop();
#endif
    }
}
