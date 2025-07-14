using UnityEngine;
using UnityEngine.Rendering;

namespace PixRenderPipline
{
    public class PostProcessPass : PixPassBase
    {
        Material postMaterial;

        int _OutLineDepthNormalThreshold = Shader.PropertyToID("_OutLineDepthNormalThreshold");
        public PostProcessPass(PixRenderer renderer) : base("PixPostProcessPass", renderer)
        {
            postMaterial = new Material(Shader.Find("Hidden/Pix/Post"));
        }

        public override void Execute()
        {
            TriggerEvent(PixRenderEventName.BeforePostProcess);
            base.Execute();

            renderer.cmb.SetRenderTarget(DeferredPass.ColorBuff);
            renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
            renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);
            renderer.cmb.SetGlobalTexture(EarlyZPass.nameID, EarlyZPass.depthID, RenderTextureSubElement.Depth);
            renderer.cmb.SetGlobalTexture(TransparentPass.ColorBuff, TransparentPass.ColorBuff);
            renderer.cmb.SetGlobalVector(_OutLineDepthNormalThreshold, new Vector2(renderer.asset.outLineDepthThreshold, renderer.asset.outLineNormalThreshold));

            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, postMaterial, 0, 0);
            renderer.cmb.ReleaseTemporaryRT(TransparentPass.ColorBuff);
            renderer.cmb.ReleaseTemporaryRT(EarlyZPass.nameID);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();

            TriggerEvent(PixRenderEventName.AfterPostProcess);
        }
    }
}