using UnityEngine;
using UnityEngine.Rendering;
public class EarlyZPass : PixPassBase
{
    public EarlyZPass(PixRenderer renderer) : base("PixEarlyZPass", renderer) { }
    readonly ShaderTagId earlyZTag = new("PixEarlyZ");
    public static readonly int nameID = Shader.PropertyToID("_PixEarlyZDepth");
    public static readonly RenderTargetIdentifier depthID = new(nameID);
    public override void Execute()
    {
        base.Execute();

        PixRenderEvent.TriggerEvent(PixRenderEventName.BeforeEarlyZ, renderer);

        // 创建深度缓冲区
        renderer.cmb.GetTemporaryRT(nameID, renderer.size.x, renderer.size.y, 32, FilterMode.Point, RenderTextureFormat.Depth);
        renderer.cmb.SetRenderTarget(depthID);
        renderer.cmb.ClearRenderTarget(true, true, black);

        // 获取渲染列表并绘制
        RendererList list = GetRendererList(earlyZTag, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);
        if (list.isValid)
            renderer.cmb.DrawRendererList(list);

        // 执行CommandBuffer
        // 把菜的配方和工艺也写到菜单里
        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();

        PixRenderEvent.TriggerEvent(PixRenderEventName.AfterEarlyZ, renderer);
    }
}