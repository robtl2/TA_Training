using UnityEngine.Rendering;
using UnityEngine;
using UnityEditor.VersionControl;

namespace PixRenderPipline
{
    public class TransparentPass : PixPassBase
    {
        public TransparentPass(PixRenderer renderer) : base("PixTransparentPass", renderer)
        {
            materialTAA = new Material(Shader.Find("Hidden/Pix/TAA"));
        }

        readonly ShaderTagId[] tagID = new ShaderTagId[] {
            new("PixTransparent"),
        };

        Material materialTAA;
        
        public static readonly int ColorBuff = Shader.PropertyToID("_PixColorTex");
        public static readonly int ColorBuff_Front = Shader.PropertyToID("_PixColorTex_Front");
        static int _HistroyWeight = Shader.PropertyToID("_HistroyWeight");
        
        public override void Execute()
        {
            base.Execute();

            GetTemporaryColorRT(ColorBuff);
            //先把之前Deferred渲染的结果复制过来
            renderer.cmb.Blit(DeferredPass.ColorBuff, ColorBuff);
            renderer.cmb.SetRenderTarget(ColorBuff, EarlyZPass.depthID);

            TriggerEvent(PixRenderEventName.BeforeTransparent);

            RendererList list = GetRendererList(tagID, SortingCriteria.CommonTransparent, RenderQueueRange.transparent);
            if (list.isValid)
                renderer.cmb.DrawRendererList(list);

            TriggerEvent(PixRenderEventName.AfterTransparent);

            if (renderer.asset.enable_TAA)
            {
                PixDeferredRenderer dr = renderer as PixDeferredRenderer;

                materialTAA.SetTexture(ColorBuff_Front, dr.frontRT[renderer.camera]);
                materialTAA.SetFloat("_HistroyWeight", renderer.asset.TAA_histroy);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, materialTAA, 0, 0);
                renderer.cmb.SetRenderTarget(dr.frontRT[renderer.camera], EarlyZPass.depthID);
                renderer.cmb.SetGlobalTexture(ColorBuff, ColorBuff);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, blitMat, 0, 1);

                renderer.cmb.ReleaseTemporaryRT(GBufferPass.GbufferID_3);
            }

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
    }
}
