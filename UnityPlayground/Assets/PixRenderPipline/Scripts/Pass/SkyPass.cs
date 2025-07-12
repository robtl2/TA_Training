using UnityEngine;

public class SkyPass : PixPassBase
{
    public SkyPass(PixRenderer renderer) : base("PixSkyPass", renderer) { }

    public override void Execute()
    {
        base.Execute();

        var material = PixSky.instance.material;
        renderer.cmb.SetRenderTarget(DeferredPass.ColorBuff, EarlyZPass.depthID);
        renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, material, 0, 0);
        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();
    }
}