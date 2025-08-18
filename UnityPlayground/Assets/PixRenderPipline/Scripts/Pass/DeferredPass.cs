using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixRenderPipline
{
    public class DeferredPass : PixPassBase
    {
        public static readonly int ColorBuff = Shader.PropertyToID("_PixOpaqueTex");
        public static readonly RenderTargetIdentifier ColorBuffID = new(ColorBuff);
        
        static int _AO_Factor = Shader.PropertyToID("_AO_Factor");
        static int _SSAO_Props = Shader.PropertyToID("_SSAO_Props");
        static int _SSAO_Clip = Shader.PropertyToID("_SSAO_Clip");
        static int _SSAO_Props_2nd = Shader.PropertyToID("_SSAO_Props_2nd");
        public Material material;
        

        public DeferredPass(PixRenderer renderer) : base("PixDeferredPass", renderer)
        {
            material = new Material(Shader.Find("Hidden/Pix/Deferred"));
        }

        public override void Execute()
        {
            base.Execute();

            if(!material)material = new Material(Shader.Find("Hidden/Pix/Deferred"));

            var setting = PixRenderSetting.instance;

            GetTemporaryColorRT(ColorBuff);

            // TODO: TiledPass搞好后用Tile来剔除多余的栅格化
            // EarlyZpass画的深度下面要拿来用，所以不管是深度还是Stencil都不能在这个Pass拿来测试象素
            renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
            renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);
            renderer.cmb.SetGlobalTexture(EarlyZPass.nameID, EarlyZPass.depthID, RenderTextureSubElement.Depth);
            renderer.cmb.SetGlobalTexture(TiledPass.tileID, TiledPass.tileID);
            
            material.SetFloat(_AO_Factor, setting.ao_factor);

            

            if (renderer.camera.orthographic)
                material.EnableKeyword("ORTHOGRAPHIC");
            else
                material.DisableKeyword("ORTHOGRAPHIC");

            if (setting.Enable_SSAO)
            {
                Vector4 ssao_props = new();
                ssao_props.x = setting.ssao_factor;
                ssao_props.y = setting.ssao_radius / setting.ssao_stepCount;
                ssao_props.z = setting.ssao_stepCount;
                ssao_props.w = setting.ssao_jitterRadius;
                material.SetVector(_SSAO_Props, ssao_props);
                material.SetInteger(_SSAO_Clip, setting.ssao_clipByDistance ? 1 : 0);

                Vector4 ssao_props_2sec = new();
                ssao_props_2sec.x = setting.ssao_factor_2nd;
                if (!setting.Enable_SSAO_2nd)
                    ssao_props_2sec.x = 0;
                ssao_props_2sec.y = setting.ssao_radius_2nd / setting.ssao_stepCount_2nd;
                ssao_props_2sec.z = setting.ssao_stepCount_2nd;
                ssao_props_2sec.w = setting.ssao_jitterRadius_2nd;
                material.SetVector(_SSAO_Props_2nd, ssao_props_2sec);

                material.DisableKeyword("SSAO_QUALITY_OFF");
                material.DisableKeyword("SSAO_QUALITY_POOR");
                material.DisableKeyword("SSAO_QUALITY_LOW");
                material.DisableKeyword("SSAO_QUALITY_MEDIUM");
                material.DisableKeyword("SSAO_QUALITY_HIGH");

                switch (setting.ssao_quality)
                {
                    case PixRenderer.SamplerQuality.Poor:
                        material.EnableKeyword("SSAO_QUALITY_POOR");
                        break;
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
                material.DisableKeyword("SSAO_QUALITY_POOR");
                material.DisableKeyword("SSAO_QUALITY_LOW");
                material.DisableKeyword("SSAO_QUALITY_MEDIUM");
                material.DisableKeyword("SSAO_QUALITY_HIGH");
            }

            renderer.cmb.SetRenderTarget(ColorBuff);

            TriggerEvent(PixRenderEventName.BeforeDeferred);

            // renderer.cmb.DrawMesh(TiledFullScreenQuad, Matrix4x4.identity, material, 0, 0);
            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, material, 0, 0);
            renderer.cmb.ReleaseTemporaryRT(TiledPass.tileID);
            
            TriggerEvent(PixRenderEventName.AfterDeferred);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
        
    }
}