using UnityEngine;
using UnityEngine.Rendering;

public class DeferredPass : PixPassBase
{
    public static readonly int ColorBuff = Shader.PropertyToID("_PixOpaqueTex");
    public Material material;
    public DeferredPass(PixRenderer renderer) : base("PixDeferredPass", renderer)
    { 
        material = new Material(Shader.Find("Hidden/Pix/Deferred"));
    }

    public override void Execute()
    {
        TriggerEvent(PixRenderEventName.BeforeDeferred);
        base.Execute();

        GetTemporaryColorRT(ColorBuff);
        // TiledPass搞好后用Tile来剔除多余的栅格化
        // EarlyZpass画的深度下面要拿来用，所以不管是深度还是Stencil都不能在这个Pass拿来测试象素
        renderer.cmb.SetRenderTarget(ColorBuff);
        renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
        renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);
        renderer.cmb.SetGlobalTexture(EarlyZPass.nameID, EarlyZPass.depthID, RenderTextureSubElement.Depth);
        renderer.cmb.SetGlobalTexture(TiledPass.tileID, TiledPass.tileID);
        renderer.cmb.DrawMesh(TiledFullScreenQuad, Matrix4x4.identity, material, 0, 0);
        renderer.cmb.ReleaseTemporaryRT(TiledPass.tileID);
        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();

        TriggerEvent(PixRenderEventName.AfterDeferred);
    }
    
}