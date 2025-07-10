using UnityEngine;
using Unity.Mathematics;

public class TiledPass : PixPassBase
{
    public TiledPass(PixRenderer renderer) : base("PixTiledPass", renderer) { }

    public static readonly int tileID = Shader.PropertyToID("_PixTiledID");

    Material material;

    public override void Execute()
    {
        if (material == null)
            material = new Material(Shader.Find("Pix/Tiled"));

        int2 size = renderer.size;
        size /= 8;

        GetTemporaryColorRT(tileID, size.x, size.y);
        cmb.SetRenderTarget(tileID);
        cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
        // TODO: 计算PerTiled Light
        cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, material, 0, 0);

        renderer.context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }
}