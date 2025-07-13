using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 渲染管线主入口
/// 就干一个事儿：调度Renderer遍历渲染相机
/// </summary>
public class PixRenderPipline : RenderPipeline
{
    public PixRenderPiplineAsset asset { get; private set; }
    public PixRenderer renderer { get; private set; }

    public PixRenderPipline(PixRenderPiplineAsset asset)
    {
        this.asset = asset;
        renderer = new PixDeferredRenderer();
    }

    /// <summary>
    /// 渲染管线主入口
    /// </summary>
    /// <param name="context">厨房管事儿的给过来的空菜单</param>
    /// <param name="cameras">点菜的客人</param>
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        // 按照深度排序相机
        System.Array.Sort(cameras, (c1, c2) => c1.depth.CompareTo(c2.depth));

        foreach (var camera in cameras)
        {
            renderer.Setup(context, camera, asset);
            renderer.Render();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        PixRenderEvent.Dispose();
    }
}
