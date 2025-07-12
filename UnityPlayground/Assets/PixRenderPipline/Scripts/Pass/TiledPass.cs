using UnityEngine;
using Unity.Mathematics;

public class TiledPass : PixPassBase
{
    public TiledPass(PixRenderer renderer) : base("PixTiledPass", renderer) { }

    public static readonly int tileID = Shader.PropertyToID("_PixTiledID");

    Material material;

    public override void Execute()
    {
        base.Execute();

        PixRenderEvent.TriggerEvent(PixRenderEventName.BeforeTiled, renderer);

        if (material == null)
            material = new Material(Shader.Find("Hidden/Pix/Tiled"));

        int2 size = renderer.tiledSize;
        GetTemporaryColorRT(tileID, size.x, size.y);
        renderer.cmb.SetRenderTarget(tileID);
        renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
        // TODO: 计算PerTiled Lights和ShadingModel的掩码
        renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, material, 0, 0);

        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();

        PixRenderEvent.TriggerEvent(PixRenderEventName.AfterTiled, renderer);
    }
}