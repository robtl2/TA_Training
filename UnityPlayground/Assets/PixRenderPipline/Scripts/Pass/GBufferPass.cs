using UnityEngine.Rendering;
using UnityEngine;

namespace PixRenderPipline
{
    public class GBufferPass : PixPassBase
    {
        readonly ShaderTagId[] gBufferTags = new ShaderTagId[] {
            new("PixGBuffer"),
        };

        public static readonly int GbufferID_0 = Shader.PropertyToID("_PixGBuffer_0");
        public static readonly int GbufferID_1 = Shader.PropertyToID("_PixGBuffer_1");
        public static readonly int GbufferID_2 = Shader.PropertyToID("_PixGBuffer_2");
        // public static readonly int GbufferID_3 = Shader.PropertyToID("_PixGBuffer_3");

        readonly RenderTargetIdentifier[] gbuffers;
        readonly RenderTargetIdentifier[] gbuffersWithMotionVec;

        public GBufferPass(PixRenderer renderer) : base("PixGBufferPass", renderer)
        {
            gbuffers = new RenderTargetIdentifier[] {
                new(GbufferID_0),
                new(GbufferID_1),
                new(GbufferID_2),
            };

            gbuffersWithMotionVec = new RenderTargetIdentifier[] {
                new(GbufferID_0),
                new(GbufferID_1),
                new(GbufferID_2),
                new(MotionVectorRT),
            };
        }

        public override void Execute()
        {
            base.Execute();

            var setting = PixRenderSetting.instance;

            GetTemporaryColorRT(GbufferID_0);
            GetTemporaryColorRT(GbufferID_1);
            GetTemporaryColorRT(GbufferID_2);

            if (setting.enable_TAA)
            {
                renderer.cmb.GetTemporaryRT(MotionVectorRT, renderer.size.x, renderer.size.y, 0, FilterMode.Point, RenderTextureFormat.RG32, renderer.colorSpace);
                renderer.cmb.SetRenderTarget(gbuffersWithMotionVec, ForwardEarlyZPass.buffID);
                renderer.cmb.ClearRenderTarget(false, true, Color.clear);
            }
            else
            {
                renderer.cmb.SetRenderTarget(gbuffers, ForwardEarlyZPass.buffID);
                renderer.cmb.ClearRenderTarget(false, true, Color.clear);
            }

            TriggerEvent(PixRenderEventName.BeforeGBuffer);

            RendererList list = GetRendererList(gBufferTags, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);

            if (list.isValid)
                renderer.cmb.DrawRendererList(list);

            PixInstance.DrawPass(renderer, 1, renderer.frustum);

            TriggerEvent(PixRenderEventName.AfterGBuffer);

            renderer.cmb.SetGlobalTexture(MotionVectorRT, MotionVectorRT);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();


        }
    }
        
}