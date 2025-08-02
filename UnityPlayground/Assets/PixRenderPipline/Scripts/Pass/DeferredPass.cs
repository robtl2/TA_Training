using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixRenderPipline
{
    public class DeferredPass : PixPassBase
    {
        public static readonly int ColorBuff = Shader.PropertyToID("_PixOpaqueTex");
        public static readonly int DepthDownSample = Shader.PropertyToID("_PixDepthDownSample");
        static RenderTargetIdentifier depthDownsampleID = new(DepthDownSample);


        static int _AO_Factor = Shader.PropertyToID("_AO_Factor");
        static int _SSAO_radius = Shader.PropertyToID("_SSAO_radius");
        static int _SSAO_intensity = Shader.PropertyToID("_SSAO_intensity");
        static int _SSAO_sampleCount = Shader.PropertyToID("_SSAO_sampleCount");



        public Material material;
        public DeferredPass(PixRenderer renderer) : base("PixDeferredPass", renderer)
        { 
            material = new Material(Shader.Find("Hidden/Pix/Deferred"));
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

            if (renderer.asset.Enable_SSAO)
            {
                int2 size = renderer.size / 2;
                GetTemporaryColorRT(DepthDownSample, size.x, size.y, FilterMode.Bilinear);
                renderer.cmb.Blit(EarlyZPass.nameID, DepthDownSample);
                renderer.cmb.SetGlobalTexture(DepthDownSample, DepthDownSample);

                material.EnableKeyword("ENABLE_SSAO");

                if (renderer.camera.orthographic)
                    material.EnableKeyword("_ORTHOGRAPHIC");
                else
                    material.DisableKeyword("_ORTHOGRAPHIC");

                material.SetFloat(_AO_Factor, renderer.asset.ao_factor);
                material.SetFloat(_SSAO_radius, renderer.asset.ssao_radius);
                material.SetFloat(_SSAO_intensity, renderer.asset.ssao_intensity);
                material.SetInt(_SSAO_sampleCount, renderer.asset.ssao_sampleCount);
            }
            else
            {
                material.DisableKeyword("ENABLE_SSAO");
                material.DisableKeyword("_ORTHOGRAPHIC");
                // renderer.cmb.SetGlobalTexture(DepthDownSample, EarlyZPass.depthID, RenderTextureSubElement.Depth);
            }

            renderer.cmb.SetRenderTarget(ColorBuff);

            TriggerEvent(PixRenderEventName.BeforeDeferred);

            renderer.cmb.DrawMesh(TiledFullScreenQuad, Matrix4x4.identity, material, 0, 0);
            renderer.cmb.ReleaseTemporaryRT(TiledPass.tileID);
            
            if (renderer.asset.Enable_SSAO)
                renderer.cmb.ReleaseTemporaryRT(DepthDownSample);

            TriggerEvent(PixRenderEventName.AfterDeferred);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
            
        }
        
    }
}