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
        public static readonly int GbufferID_3 = Shader.PropertyToID("_PixGBuffer_3");

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
                new(GbufferID_3),
            };
        }

        static Color gray = new Color(0.5f, 0.5f, 0, 0);

        public override void Execute()
        {
            base.Execute();

            GetTemporaryColorRT(GbufferID_0);
            GetTemporaryColorRT(GbufferID_1);
            GetTemporaryColorRT(GbufferID_2);

            if (renderer.asset.enable_TAA)
            {
                renderer.cmb.GetTemporaryRT(GbufferID_3, renderer.size.x, renderer.size.y, 0, FilterMode.Point, RenderTextureFormat.Default, renderer.colorSpace);
                renderer.cmb.SetRenderTarget(gbuffersWithMotionVec, EarlyZPass.depthID);
                renderer.cmb.ClearRenderTarget(false, true, gray);
            }
            else
            {
                renderer.cmb.SetRenderTarget(gbuffers, EarlyZPass.depthID);
                renderer.cmb.ClearRenderTarget(false, true, black);
            }

            TriggerEvent(PixRenderEventName.BeforeGBuffer);

            RendererList list = GetRendererList(gBufferTags, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);

            if (list.isValid)
                renderer.cmb.DrawRendererList(list);

            TriggerEvent(PixRenderEventName.AfterGBuffer);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();


        }
    }
        
}