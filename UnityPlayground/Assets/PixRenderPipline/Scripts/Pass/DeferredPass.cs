using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixRenderPipline
{
    public class DeferredPass : PixPassBase
    {
        public Material material;
        
        public DeferredPass(PixRenderer renderer) : base("PixDeferredPass", renderer)
        {
            material = new Material(Shader.Find("Hidden/Pix/Deferred"));
        }

        public override void Execute()
        {
            base.Execute();

            if (!material) material = new Material(Shader.Find("Hidden/Pix/Deferred"));
            int2 size = renderer.size.xy;
            renderer.cmb.GetTemporaryRT(OpaqueRT, size.x, size.y, 0, FilterMode.Point, RenderTextureFormat.RGB111110Float, renderer.colorSpace);
            // GetTemporaryColorRT(ColorBuff);

            // TODO: TiledPass搞好后用Tile来剔除多余的栅格化
            renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
            renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);
            renderer.cmb.SetGlobalTexture(TiledPass.tileID, TiledPass.tileID);
            renderer.cmb.SetRenderTarget(OpaqueRT, EarlyZPass.buffID);

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