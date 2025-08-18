using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace PixRenderPipline
{
    public class EarlyZPass : PixPassBase
    {
        public EarlyZPass(PixRenderer renderer) : base("PixEarlyZPass", renderer) { }
        readonly ShaderTagId earlyZTag = new("PixEarlyZ");
        public static readonly int nameID = Shader.PropertyToID("_PixEarlyZDepth");
        public static readonly int DepthDownSample = Shader.PropertyToID("_PixDepthDownSample");
        public static readonly RenderTargetIdentifier depthID = new(nameID);
        public override void Execute()
        {
            TriggerEvent(PixRenderEventName.BeforeAll);

            base.Execute();

            // 创建深度缓冲区
            renderer.cmb.GetTemporaryRT(nameID, renderer.size.x, renderer.size.y, 32, FilterMode.Point, RenderTextureFormat.Depth);
            renderer.cmb.SetRenderTarget(depthID);
            renderer.cmb.ClearRenderTarget(true, true, Color.clear);

            TriggerEvent(PixRenderEventName.BeforeEarlyZ);

            PixInstance.DrawPass(renderer, 1, renderer.frustum);

            // 获取渲染列表并绘制
            RendererList list = GetRendererList(earlyZTag, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);
            if (list.isValid)
                renderer.cmb.DrawRendererList(list);

            // Depth经常拿来被采样，这里blit出来一个下采样的DepthTexture
            int2 size = renderer.size.zw;
            renderer.cmb.GetTemporaryRT(DepthDownSample, size.x, size.y, 0, FilterMode.Point, RenderTextureFormat.RFloat);
            renderer.cmb.SetRenderTarget(DepthDownSample);
            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, blitMat, 0, 0);

            TriggerEvent(PixRenderEventName.AfterEarlyZ);

            renderer.cmb.SetGlobalTexture(DepthDownSample, DepthDownSample);

            // 执行CommandBuffer
            // 把菜的配方和工艺也写到菜单里
            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
    }
}