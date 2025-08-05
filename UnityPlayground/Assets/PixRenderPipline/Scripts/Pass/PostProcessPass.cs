using UnityEngine;
using UnityEngine.Rendering;

namespace PixRenderPipline
{
    public class PostProcessPass : PixPassBase
    {
        Material postMaterial;

        public PostProcessPass(PixRenderer renderer) : base("PixPostProcessPass", renderer)
        {
            postMaterial = new Material(Shader.Find("Hidden/Pix/Post"));
        }

        public override void Execute()
        {
            base.Execute();

            renderer.cmb.SetRenderTarget(DeferredPass.ColorBuff);
            renderer.cmb.SetGlobalTexture(TransparentPass.ColorBuff, TransparentPass.ColorBuff);
            renderer.cmb.SetGlobalTexture(DeferredPass.DepthDownSample, DeferredPass.DepthDownSample);

            TriggerEvent(PixRenderEventName.BeforePostProcess);

            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, postMaterial, 0, 0);
            renderer.cmb.ReleaseTemporaryRT(TransparentPass.ColorBuff);
            
            renderer.cmb.ReleaseTemporaryRT(DeferredPass.DepthDownSample);
            renderer.cmb.ReleaseTemporaryRT(EarlyZPass.nameID);

            TriggerEvent(PixRenderEventName.AfterPostProcess);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();

            
        }
    }
}