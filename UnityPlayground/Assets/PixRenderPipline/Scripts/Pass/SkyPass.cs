using UnityEngine;

public class SkyPass : PixPassBase
{
    public SkyPass(PixRenderer renderer) : base("PixSkyPass", renderer) { }

    public override void Execute()
    {
        base.Execute();
        var asset = renderer.asset;
        asset.skyMaterial.SetFloat("_RotateSky", asset.rotateSky);
        asset.skyMaterial.SetFloat("_SkyIntensity", asset.skyIntensity);
        asset.skyMaterial.SetFloat("_SkyFovScale", asset.skyFovScale);

        renderer.cmb.SetRenderTarget(DeferredPass.ColorBuff, EarlyZPass.depthID);
        renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, asset.skyMaterial, 0, 0);
        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();
    }
}