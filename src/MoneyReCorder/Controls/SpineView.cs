using System.Windows;
using System.Windows.Threading;
#if SPINE_ENABLED
using MoneyReCorder.Spine;
using SkiaSharp;
using SkiaSharp.Views.WPF;
#endif

namespace MoneyReCorder.Controls;

#if SPINE_ENABLED
/// <summary>
/// WPF Spine 动画显示控件
/// 基于 SkiaSharp 渲染 Spine 骨骼动画
/// </summary>
public class SpineView : SKElement
{
    private readonly SpineRenderer _renderer = new();
    private readonly DispatcherTimer _animationTimer;
    private DateTime _lastFrameTime;

    public SpineRenderer Renderer => _renderer;
    public new bool IsLoaded => _renderer.IsLoaded;

    public string? SpineDataFolder { get; set; }
    public string? SkeletonFileName { get; set; }
    public string? AtlasFileName { get; set; }
    public string? DefaultAnimation { get; set; }
    public bool AutoPlay { get; set; } = true;

    public SpineView()
    {
        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _animationTimer.Tick += OnAnimationTick;
        _lastFrameTime = DateTime.UtcNow;
        Width = 300; Height = 400;
        Loaded += (_, _) => LoadSpineData();
        Unloaded += (_, _) => Stop();
    }

    public void LoadSpineData()
    {
        if (string.IsNullOrEmpty(SpineDataFolder) || string.IsNullOrEmpty(SkeletonFileName) || string.IsNullOrEmpty(AtlasFileName))
            return;

        var atlasPath = System.IO.Path.Combine(SpineDataFolder, AtlasFileName);
        var skeletonPath = System.IO.Path.Combine(SpineDataFolder, SkeletonFileName);

        if (_renderer.Load(atlasPath, skeletonPath))
        {
            if (AutoPlay && !string.IsNullOrEmpty(DefaultAnimation))
                _renderer.PlayAnimation(DefaultAnimation, true);
            Start();
            InvalidateVisual();
        }
    }

    public void Start() { _lastFrameTime = DateTime.UtcNow; _animationTimer.Start(); }
    public void Stop() { _animationTimer.Stop(); }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        if (!_renderer.IsLoaded) return;
        var delta = (float)(DateTime.UtcNow - _lastFrameTime).TotalSeconds;
        _lastFrameTime = DateTime.UtcNow;
        if (delta > 0.1f) delta = 0.016f;
        _renderer.Update(delta);
        InvalidateVisual();
    }

    protected override void OnPaint(SKSurface surface, SKImageInfo info)
    {
        if (!_renderer.IsLoaded)
        {
            surface.Canvas.Clear(new SKColor(240, 240, 240, 30));
            return;
        }
        _renderer.Render(surface.Canvas, info.Width, info.Height);
    }
}
#else
/// <summary>
/// Spine 占位控件（Spine 运行时未安装时使用）
/// </summary>
public class SpineView : System.Windows.Controls.Control
{
    public string? SpineDataFolder { get; set; }
    public string? SkeletonFileName { get; set; }
    public string? AtlasFileName { get; set; }
    public string? DefaultAnimation { get; set; }
    public bool AutoPlay { get; set; } = true;
    public new bool IsLoaded => false;
    public void Start() { }
    public void Stop() { }
}
#endif
