using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace PixRenderPipline
{
    public class ForwardEarlyZPass : PixPassBase
    {
        public ForwardEarlyZPass(PixRenderer renderer) : base("PixForwardEarlyZPass", renderer) { }
        static ShaderTagId depthNormalTag = new("PixForwardEarlyZ");
        public static readonly int buffName = Shader.PropertyToID("_PixDepthNormal");
        public static readonly int downSample = Shader.PropertyToID("_PixDepthNormalDownSample");
        public static readonly RenderTargetIdentifier buffID = new(buffName);
        public override void Execute()
        {
            TriggerEvent(PixRenderEventName.BeforeAll);

            base.Execute();
            renderer.cmb.GetTemporaryRT(buffName, renderer.size.x, renderer.size.y, 32, FilterMode.Point, RenderTextureFormat.ARGB32, renderer.colorSpace);
            renderer.cmb.SetRenderTarget(buffID);
            renderer.cmb.ClearRenderTarget(true, true, Color.clear);
            RendererList list = GetRendererList(depthNormalTag, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);

            if (list.isValid)
                renderer.cmb.DrawRendererList(list);

            PixInstance.DrawPass(renderer, 3, renderer.frustum);

            renderer.cmb.GetTemporaryRT(downSample, renderer.size.z, renderer.size.w, 0, FilterMode.Point, RenderTextureFormat.ARGB32, renderer.colorSpace);
            renderer.cmb.SetRenderTarget(downSample);
            renderer.cmb.SetGlobalTexture(_MainTex, buffName);
            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, blitMat, 0, 1);
            renderer.cmb.SetGlobalTexture(downSample, downSample);
            renderer.cmb.SetGlobalTexture(buffName, buffName);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
    }
}