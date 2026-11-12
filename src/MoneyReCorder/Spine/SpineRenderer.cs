#if SPINE_ENABLED
using Spine;
using SkiaSharp;

namespace MoneyReCorder.Spine;

/// <summary>
/// Spine 骨骼渲染器（基于 SkiaSharp）
/// 负责将 Spine 骨骼数据绘制到 SKCanvas 上
/// </summary>
public class SpineRenderer
{
    private readonly SkeletonRenderer _renderer;
    private Skeleton? _skeleton;
    private AnimationState? _animationState;
    private AnimationStateData? _animationStateData;
    private Atlas? _atlas;
    private SkeletonData? _skeletonData;
    private bool _isLoaded;

    /// <summary>当前是否已加载</summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>骨骼对象</summary>
    public Skeleton? Skeleton => _skeleton;

    /// <summary>动画状态</summary>
    public AnimationState? AnimationState => _animationState;

    /// <summary>可用动画名称列表</summary>
    public List<string> AnimationNames { get; } = new();

    /// <summary>当前正在播放的动画名称</summary>
    public string? CurrentAnimation { get; private set; }

    public SpineRenderer()
    {
        _renderer = new SkeletonRenderer();
    }

    /// <summary>
    /// 加载 Spine 资源
    /// </summary>
    /// <param name="atlasPath">.atlas 文件路径</param>
    /// <param name="skeletonPath">.skel（二进制）或 .json 文件路径</param>
    public bool Load(string atlasPath, string skeletonPath)
    {
        try
        {
            if (!File.Exists(atlasPath) || !File.Exists(skeletonPath))
                return false;

            // 加载 Atlas
            _atlas = new Atlas(atlasPath, new TextureLoader());

            // 加载骨骼数据
            var binary = new SkeletonBinary(_atlas);
            if (skeletonPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var json = new SkeletonJson(_atlas);
                _skeletonData = json.ReadSkeletonData(skeletonPath);
            }
            else
            {
                _skeletonData = binary.ReadSkeletonData(skeletonPath);
            }

            // 创建骨骼实例
            _skeleton = new Skeleton(_skeletonData);
            _skeleton.SetToSetupPose();

            // 创建动画状态
            _animationStateData = new AnimationStateData(_skeletonData);
            _animationState = new AnimationState(_animationStateData);

            // 收集可用动画
            AnimationNames.Clear();
            foreach (var anim in _skeletonData.Animations)
                AnimationNames.Add(anim.Name);

            // 自动缩放以适应窗口
            _skeleton.ScaleX = 0.5f;
            _skeleton.ScaleY = 0.5f;

            // 居中
            _skeleton.X = 150;
            _skeleton.Y = 300;

            _isLoaded = true;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Spine load error: {ex.Message}");
            _isLoaded = false;
            return false;
        }
    }

    /// <summary>播放动画</summary>
    public void PlayAnimation(string name, bool loop = true)
    {
        if (_animationState == null || _skeletonData == null) return;

        if (_skeletonData.FindAnimation(name) != null)
        {
            _animationState.SetAnimation(0, name, loop);
            CurrentAnimation = name;
        }
    }

    /// <summary>混合过渡到动画</summary>
    public void TransitionToAnimation(string name, bool loop = true, float transitionTime = 0.3f)
    {
        if (_animationState == null || _skeletonData == null) return;

        if (_skeletonData.FindAnimation(name) != null)
        {
            _animationState.AddAnimation(0, name, loop, transitionTime);
            CurrentAnimation = name;
        }
    }

    /// <summary>更新动画</summary>
    public void Update(float deltaTime)
    {
        if (!_isLoaded || _skeleton == null || _animationState == null) return;

        _animationState.Update(deltaTime);
        _animationState.Apply(_skeleton);
    }

    /// <summary>渲染到 SkiaSharp 画布</summary>
    public void Render(SKCanvas canvas, int width, int height)
    {
        if (!_isLoaded || _skeleton == null) return;

        // 清空背景
        canvas.Clear(SKColors.Transparent);

        // 更新骨骼位置（居中）
        _skeleton.X = width / 2f;
        _skeleton.Y = height * 0.75f;

        // 更新姿势
        _skeleton.UpdateWorldTransform();

        // 渲染
        _renderer.Draw(canvas, _skeleton);
    }

    /// <summary>释放资源</summary>
    public void Dispose()
    {
        _isLoaded = false;
        _atlas?.Dispose();
        _skeletonData = null;
        _skeleton = null;
        _animationState = null;
        _animationStateData = null;
    }
}

/// <summary>
/// 纹理加载器（从文件加载 Spine 所需的 PNG 纹理）
/// </summary>
public class TextureLoader : Spine.TextureLoader
{
    public void Load(AtlasPage page, string path)
    {
        if (!File.Exists(path))
        {
            // 尝试在和 atlas 同目录下查找
            path = Path.Combine(Path.GetDirectoryName(page.Name) ?? "", Path.GetFileName(path));
        }

        if (File.Exists(path))
        {
            var image = SKImage.FromEncodedData(path);
            var bitmap = SKBitmap.FromImage(image);
            page.RendererObject = bitmap;
            page.Width = bitmap.Width;
            page.Height = bitmap.Height;
        }
    }

    public void Unload(object texture)
    {
        if (texture is SKBitmap bitmap)
        {
            bitmap.Dispose();
        }
    }
}

/// <summary>
/// Spine 骨骼绘制器（SkiaSharp 实现）
/// </summary>
public class SkeletonRenderer
{
    public void Draw(SKCanvas canvas, Skeleton skeleton)
    {
        if (skeleton == null) return;

        var drawOrder = skeleton.DrawOrder;
        if (drawOrder == null) return;

        foreach (var slot in drawOrder)
        {
            var attachment = slot.Attachment;
            if (attachment == null) continue;

            if (attachment is RegionAttachment regionAttachment)
            {
                DrawRegion(canvas, slot, regionAttachment);
            }
            else if (attachment is MeshAttachment meshAttachment)
            {
                DrawMesh(canvas, slot, meshAttachment);
            }
        }
    }

    private static void DrawRegion(SKCanvas canvas, Slot slot, RegionAttachment attachment)
    {
        attachment.ComputeWorldVertices(slot.Bone, _worldVertices, 0);

        var atlasRegion = (AtlasRegion)attachment.RendererObject;
        if (atlasRegion?.Page.RendererObject is not SKBitmap bitmap) return;

        var vertices = _worldVertices;

        // 创建绘制顶点
        var points = new SKPoint[4];
        for (int i = 0; i < 4; i++)
        {
            points[i] = new SKPoint(vertices[i * 2], vertices[i * 2 + 1]);
        }

        // UV 坐标
        var uvs = attachment.UVs;
        var rect = new SKRect(
            uvs[0] * bitmap.Width,
            uvs[1] * bitmap.Height,
            uvs[4] * bitmap.Width,
            uvs[5] * bitmap.Height
        );

        using var paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.Medium,
        };

        // 颜色
        var color = slot.Color;
        paint.Color = new SKColor(
            (byte)(color.R * 255),
            (byte)(color.G * 255),
            (byte)(color.B * 255),
            (byte)(color.A * 255)
        );

        // 绘制
        using var src = new SKRectI(
            (int)rect.Left, (int)rect.Top,
            (int)rect.Right, (int)rect.Bottom);

        canvas.DrawBitmap(bitmap, src, new SKRect(points[0].X, points[0].Y, points[2].X, points[2].Y), paint);
    }

    private static void DrawMesh(SKCanvas canvas, Slot slot, MeshAttachment mesh)
    {
        // 简化版 Mesh 绘制
        // 完整实现需要处理三角化索引和 UV
    }

    private static readonly float[] _worldVertices = new float[8];
}
#endif
