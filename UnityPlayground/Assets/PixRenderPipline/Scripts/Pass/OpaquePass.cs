using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace PixRenderPipline
{
    public class OpaquePass : PixPassBase
    {
        public OpaquePass(PixRenderer renderer) : base("PixOpaquePass", renderer) { }
        static ShaderTagId shaderTag = new("PixForward");

        RenderTargetIdentifier[] colorBuffers = new RenderTargetIdentifier[] {
            new(OpaqueRT),
            new(MotionVectorRT),
        };
            
        public override void Execute()
        {
            base.Execute();
            renderer.cmb.GetTemporaryRT(OpaqueRT, renderer.size.x, renderer.size.y, 0, FilterMode.Point, RenderTextureFormat.RGB111110Float, renderer.colorSpace);
            renderer.cmb.GetTemporaryRT(MotionVectorRT, renderer.size.x, renderer.size.y, 0, FilterMode.Point, RenderTextureFormat.RG32, renderer.colorSpace);
            renderer.cmb.SetRenderTarget(colorBuffers, ForwardEarlyZPass.buffID);
            renderer.cmb.ClearRenderTarget(false, true, Color.clear);
            RendererList list = GetRendererList(shaderTag, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);

            if (list.isValid)
                renderer.cmb.DrawRendererList(list);

            PixInstance.DrawPass(renderer, 0, renderer.frustum);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
    }
}