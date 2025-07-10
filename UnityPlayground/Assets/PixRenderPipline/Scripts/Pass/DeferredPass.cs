using UnityEngine;
using UnityEngine.Rendering;

public class DeferredPass : PixPassBase
{
    public DeferredPass(PixRenderer renderer) : base("PixDeferredPass", renderer) { }

    public static readonly int ColorBuff = Shader.PropertyToID("_PixOpaqueTex");

    public Material material;

    public override void Execute()
    {
        GetTemporaryColorRT(ColorBuff);
        cmb.SetRenderTarget(ColorBuff);

        cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
        cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);
        cmb.SetGlobalTexture(EarlyZPass.nameID, EarlyZPass.depthID, RenderTextureSubElement.Depth);
        cmb.SetGlobalTexture(TiledPass.tileID, TiledPass.tileID);

        if (material == null)
            material = new Material(Shader.Find("Pix/Deferred"));

        cmb.DrawMesh(TiledFullScreenQuad, Matrix4x4.identity, material, 0, 0);

        cmb.ReleaseTemporaryRT(TiledPass.tileID);

        renderer.context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }
    
}