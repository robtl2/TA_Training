using UnityEngine;
using UnityEngine.Rendering;

namespace PixRenderPipline
{

    /// <summary>
    /// 现在的EarlyZPass已经不干净了，本来这个Pass是尽量不进FS阶段的
    /// 因为我想在Shading前就能在下采样里把SSAO画好，那么我就得有DepthNormal
    /// 所以只好在EarlyZPass里随便把DepthNormal给准备好了
    /// 还有另一个好处就是现在既然提前有了DepthNormal，所以Forward渲染流程里对应效果也能顺利进行了
    /// </summary>
    public class EarlyZPass : PixPassBase
    {
        public EarlyZPass(PixRenderer renderer) : base("PixEarlyZPass", renderer) { }
        static ShaderTagId depthNormalTag = new("PixForwardEarlyZ");
        public static readonly int buffName = Shader.PropertyToID("_PixDepthNormal");
        public static readonly int downSample = Shader.PropertyToID("_PixDepthNormalDownSample");

        public static readonly RenderTargetIdentifier buffID = new(buffName);
        public override void Execute()
        {
            TriggerEvent(PixRenderEventName.BeforeAll);

            base.Execute();
            renderer.cmb.GetTemporaryRT(buffName, renderer.size.x, renderer.size.y, 32, FilterMode.Point, RenderTextureFormat.ARGB32, renderer.colorSpace);
            renderer.cmb.SetRenderTarget(buffName);
            renderer.cmb.ClearRenderTarget(true, true, Color.clear);
            RendererList list = GetRendererList(depthNormalTag, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);

            TriggerEvent(PixRenderEventName.BeforeEarlyZ);

            if (list.isValid)
                renderer.cmb.DrawRendererList(list);

            PixInstance.DrawPass(renderer, 3, renderer.frustum);

            TriggerEvent(PixRenderEventName.AfterEarlyZ);

            renderer.cmb.GetTemporaryRT(downSample, renderer.size.z, renderer.size.w, 0, FilterMode.Point, RenderTextureFormat.ARGB32, renderer.colorSpace);
            renderer.cmb.SetRenderTarget(downSample);
            renderer.cmb.SetGlobalTexture(MainTex, buffName);
            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, blitMat, 0, 1);
            renderer.cmb.SetGlobalTexture(downSample, downSample);
            renderer.cmb.SetGlobalTexture(buffName, buffName);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
    }
}