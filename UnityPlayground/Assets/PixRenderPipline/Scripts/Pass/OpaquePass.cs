using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace PixRenderPipline
{
    public class OpaquePass : PixPassBase
    {
        public OpaquePass(PixRenderer renderer) : base("PixOpaquePass", renderer) { }
        static ShaderTagId shaderTag = new("PixForward");

        // 这Forward也不个干净的Forward，其实也是MRT
        // 在开了TAA时，还是会多写一个motionVector的Target
        RenderTargetIdentifier[] colorBuffers = new RenderTargetIdentifier[] {
            new(OpaqueRT),
            new(MotionVectorRT),
        };

        RenderTargetIdentifier colorBuffer = new(OpaqueRT);
            
        public override void Execute()
        {
            base.Execute();

            var setting = PixRenderSetting.instance;

            renderer.cmb.GetTemporaryRT(OpaqueRT, renderer.size.x, renderer.size.y, 0, FilterMode.Point, RenderTextureFormat.RGB111110Float, renderer.colorSpace);

            if (setting.enable_TAA)
            {
                renderer.cmb.GetTemporaryRT(MotionVectorRT, renderer.size.x, renderer.size.y, 0, FilterMode.Point, RenderTextureFormat.RG32, renderer.colorSpace);
                renderer.cmb.SetRenderTarget(colorBuffers, EarlyZPass.buffID);
            }
            else
            { 
                renderer.cmb.SetRenderTarget(colorBuffer, EarlyZPass.buffID);
            }

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