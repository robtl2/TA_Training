using UnityEngine;
using UnityEngine.Rendering;

namespace PixRenderPipline
{
    public class PostProcessPass : PixPassBase
    {
        Material postMaterial;

        Material materialTAA;
        static int _HistroyWeight = Shader.PropertyToID("_HistroyWeight");
        static int _SharpenAmount = Shader.PropertyToID("_SharpenAmount");

        public static readonly int ColorBuff_Front = Shader.PropertyToID("_PixColorTex_Front");

        public PostProcessPass(PixRenderer renderer) : base("PixPostProcessPass", renderer)
        {
            postMaterial = new Material(Shader.Find("Hidden/Pix/Post"));
            materialTAA = new Material(Shader.Find("Hidden/Pix/TAA"));
        }

        public override void Execute()
        {
            base.Execute();

            var setting = PixRenderSetting.instance;

            renderer.cmb.SetRenderTarget(DeferredPass.ColorBuff);
            renderer.cmb.SetGlobalTexture(TransparentPass.ColorBuff, TransparentPass.ColorBuff);
            renderer.cmb.SetGlobalTexture(DeferredPass.DepthDownSample, DeferredPass.DepthDownSample);

            postMaterial.SetFloat(_SharpenAmount, setting.SharpenStrength);

            TriggerEvent(PixRenderEventName.BeforePostProcess);

            if (setting.Enable_Sharpen)
                postMaterial.EnableKeyword("PP_SHARPEN");
            else
                postMaterial.DisableKeyword("PP_SHARPEN");

            if (setting.Enable_Tonemapping)
                postMaterial.EnableKeyword("PP_TONEMAPPING");
            else
                postMaterial.DisableKeyword("PP_TONEMAPPING");

            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, postMaterial, 0, 0);

            renderer.cmb.ReleaseTemporaryRT(TransparentPass.ColorBuff);
            renderer.cmb.ReleaseTemporaryRT(DeferredPass.DepthDownSample);

            TriggerEvent(PixRenderEventName.AfterPostProcess);

            if (setting.enable_TAA)
            {
                PixDeferredRenderer dr = renderer as PixDeferredRenderer;

                materialTAA.SetTexture(ColorBuff_Front, dr.frontRT[renderer.camera]);
                materialTAA.SetFloat(_HistroyWeight, setting.TAA_histroy);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, materialTAA, 0, 0);
                renderer.cmb.SetRenderTarget(dr.frontRT[renderer.camera], EarlyZPass.depthID);
                renderer.cmb.SetGlobalTexture(DeferredPass.ColorBuff, DeferredPass.ColorBuff);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, blitMat, 0, 1);

                renderer.cmb.ReleaseTemporaryRT(GBufferPass.GbufferID_3);
            }

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();

            
        }
    }
}