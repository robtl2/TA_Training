using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace PixRenderPipline
{
    public class PostProcessPass : PixPassBase
    {
        Material postMaterial;

        Material materialTAA;

        static int _Bloom_Threshold = Shader.PropertyToID("_Bloom_Threshold");
        static int _BloomTex = Shader.PropertyToID("_BloomTex");
        static int _BloomTexVertical = Shader.PropertyToID("_BloomTexVertical");
        static int _BloomTexHorizontal = Shader.PropertyToID("_BloomTexHorizontal");
        // static int _BloomTexDown = Shader.PropertyToID("_BloomTexDown");
        static int _BloomRadius = Shader.PropertyToID("_BloomRadius");
        static int _BloomIntensity = Shader.PropertyToID("_BloomIntensity");

        static int _HistroyWeight = Shader.PropertyToID("_HistroyWeight");
        static int _SharpenProps = Shader.PropertyToID("_SharpenProps");
        static int _Exposure = Shader.PropertyToID("_Exposure");
        static int _Vagnet = Shader.PropertyToID("_Vagnet");

        public static readonly int ColorBuff_Front = Shader.PropertyToID("_PixColorTex_Front");

        public PostProcessPass(PixRenderer renderer) : base("PixPostProcessPass", renderer)
        {
            postMaterial = new Material(Shader.Find("Hidden/Pix/Post"));
            materialTAA = new Material(Shader.Find("Hidden/Pix/TAA"));
        }

        RenderTextureDescriptor getTexDes(int2 size)
        { 
            RenderTextureDescriptor rtDescriptor = new RenderTextureDescriptor(size.x, size.y, RenderTextureFormat.ARGB32,0,4);
            rtDescriptor.sRGB = false;
            rtDescriptor.mipCount = 8;
            rtDescriptor.useMipMap = true;
            rtDescriptor.autoGenerateMips = false;
            return rtDescriptor;
        }

        public override void Execute()
        {
            base.Execute();

            var setting = PixRenderSetting.instance;
            PixDeferredRenderer dr = renderer as PixDeferredRenderer;

            if (setting.EnableBloom)
            {
                int2 size = renderer.size / 2;

                renderer.cmb.SetGlobalTexture(_BloomTex, TransparentPass.ColorBuff);
                renderer.cmb.SetGlobalFloat(_Bloom_Threshold, setting.Bloom_Threshold * 0.5f);
                renderer.cmb.SetGlobalFloat(_BloomRadius, setting.Bloom_Radius);
                renderer.cmb.SetGlobalFloat(_BloomIntensity, setting.Bloom_Intensity);

                // 先做竖着方向的模糊
                GetTemporaryColorRT(_BloomTexVertical, size.x, size.y, FilterMode.Trilinear);
                renderer.cmb.SetRenderTarget(_BloomTexVertical);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, blitMat, 0, 2);

                // 再做横着方向的模糊
                var rtDescriptor = getTexDes(size);
                renderer.cmb.GetTemporaryRT(_BloomTexHorizontal, rtDescriptor, FilterMode.Trilinear);
                renderer.cmb.SetRenderTarget(_BloomTexHorizontal);
                renderer.cmb.SetGlobalTexture(_BloomTex, _BloomTexVertical);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, blitMat, 0, 3);

            }

            renderer.cmb.SetRenderTarget(DeferredPass.ColorBuff);
            renderer.cmb.SetGlobalTexture(TransparentPass.ColorBuff, TransparentPass.ColorBuff);
            renderer.cmb.SetGlobalTexture(DeferredPass.DepthDownSample, DeferredPass.DepthDownSample);
            if (setting.EnableBloom)
            {
                renderer.cmb.GenerateMips(_BloomTexHorizontal);
                renderer.cmb.SetGlobalTexture(_BloomTex, _BloomTexHorizontal);
                postMaterial.EnableKeyword("PP_BLOOM");
            }
            else
            {
                postMaterial.DisableKeyword("PP_BLOOM");
            }

            Vector4 sharpenProps = new();
            sharpenProps.x = setting.SharpenStrength;
            sharpenProps.y = setting.SharpenMid;
            postMaterial.SetVector(_SharpenProps, sharpenProps);

            postMaterial.SetFloat(_Exposure, setting.Exposure);
            postMaterial.SetFloat(_Vagnet, setting.Vagnet);

            TriggerEvent(PixRenderEventName.BeforePostProcess);

            if (setting.SharpenStrength > 0.1 && setting.enable_TAA)
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

            if (setting.EnableBloom)
            {
                renderer.cmb.ReleaseTemporaryRT(_BloomTexVertical);
                renderer.cmb.ReleaseTemporaryRT(_BloomTexHorizontal);
            }

            TriggerEvent(PixRenderEventName.AfterPostProcess);

            if (setting.enable_TAA)
            {
                materialTAA.SetTexture(ColorBuff_Front, dr.frontRT[renderer.camera]);
                materialTAA.SetFloat(_HistroyWeight, setting.TAA_histroy);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, materialTAA, 0, 0);
                renderer.cmb.SetRenderTarget(dr.frontRT[renderer.camera]);
                renderer.cmb.SetGlobalTexture(_MainTex, DeferredPass.ColorBuff);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, blitMat, 0, 1);

                renderer.cmb.ReleaseTemporaryRT(GBufferPass.GbufferID_3);
            }

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();


        }
    }
}