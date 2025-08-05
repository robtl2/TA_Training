using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixRenderPipline
{
    public class DeferredPass : PixPassBase
    {
        public static readonly int ColorBuff = Shader.PropertyToID("_PixOpaqueTex");
        public static readonly int DepthDownSample = Shader.PropertyToID("_PixDepthDownSample");
        static int _AO_Factor = Shader.PropertyToID("_AO_Factor");
        static int _SSAO_Props = Shader.PropertyToID("_SSAO_Props");
        public Material material;
        Material blitMaterial;

        public DeferredPass(PixRenderer renderer) : base("PixDeferredPass", renderer)
        {
            material = new Material(Shader.Find("Hidden/Pix/Deferred"));
            blitMaterial = new Material(Shader.Find("Hidden/Pix/Blit"));
        }

        public override void Execute()
        {
            base.Execute();

            GetTemporaryColorRT(ColorBuff);

            // TODO: TiledPass搞好后用Tile来剔除多余的栅格化
            // EarlyZpass画的深度下面要拿来用，所以不管是深度还是Stencil都不能在这个Pass拿来测试象素
            renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
            renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);
            renderer.cmb.SetGlobalTexture(EarlyZPass.nameID, EarlyZPass.depthID, RenderTextureSubElement.Depth);
            renderer.cmb.SetGlobalTexture(TiledPass.tileID, TiledPass.tileID);
            
            material.SetFloat(_AO_Factor, renderer.asset.ao_factor);

            // Depth经常拿来被采样，这里blit出来一个下采样的DepthTexture
            int2 size = renderer.size / 2;
            renderer.cmb.GetTemporaryRT(DepthDownSample, size.x, size.y, 0, FilterMode.Point, RenderTextureFormat.R16);
            renderer.cmb.SetRenderTarget(DepthDownSample);
            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, blitMaterial, 0, 0);

            if (renderer.camera.orthographic)
                material.EnableKeyword("ORTHOGRAPHIC");
            else
                material.DisableKeyword("ORTHOGRAPHIC");

            if (renderer.asset.Enable_SSAO)
            {
                Vector4 ssao_props = new();
                ssao_props.x = renderer.asset.ssao_factor;
                ssao_props.y = renderer.asset.ssao_radius * 0.001f / renderer.asset.ssao_stepCount;
                ssao_props.z = renderer.asset.ssao_stepCount;
                ssao_props.w = renderer.asset.ssao_jitterRadius;
                material.SetVector(_SSAO_Props, ssao_props);

                material.DisableKeyword("SSAO_QUALITY_OFF");
                material.DisableKeyword("SSAO_QUALITY_LOW");
                material.DisableKeyword("SSAO_QUALITY_MEDIUM");
                material.DisableKeyword("SSAO_QUALITY_HIGH");

                switch (renderer.asset.ssao_quality)
                {
                    case PixRenderer.SamplerQuality.Low:
                        material.EnableKeyword("SSAO_QUALITY_LOW");
                        break;
                    case PixRenderer.SamplerQuality.Medium:
                        material.EnableKeyword("SSAO_QUALITY_MEDIUM");
                        break;
                    case PixRenderer.SamplerQuality.High:
                        material.EnableKeyword("SSAO_QUALITY_HIGH");
                        break;
                }
            }
            else
            {
                material.EnableKeyword("SSAO_QUALITY_OFF");
                material.DisableKeyword("SSAO_QUALITY_LOW");
                material.DisableKeyword("SSAO_QUALITY_MEDIUM");
                material.DisableKeyword("SSAO_QUALITY_HIGH");
            }

            renderer.cmb.SetRenderTarget(ColorBuff);
            renderer.cmb.SetGlobalTexture(DepthDownSample, DepthDownSample);

            TriggerEvent(PixRenderEventName.BeforeDeferred);

            renderer.cmb.DrawMesh(TiledFullScreenQuad, Matrix4x4.identity, material, 0, 0);
            renderer.cmb.ReleaseTemporaryRT(TiledPass.tileID);
            
            TriggerEvent(PixRenderEventName.AfterDeferred);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
        
    }
}