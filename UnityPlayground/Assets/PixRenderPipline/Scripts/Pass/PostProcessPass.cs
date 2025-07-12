using UnityEngine;

public class PostProcessPass : PixPassBase
{
    public PostProcessPass(PixRenderer renderer) : base("PixPostProcessPass", renderer) { }

    Material postMaterial;

    public override void Execute()
    {
        base.Execute();
        if (postMaterial == null)
            postMaterial = new Material(Shader.Find("Hidden/Pix/Post"));

        renderer.cmb.SetRenderTarget(DeferredPass.ColorBuff);
        renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
        renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);
        renderer.cmb.SetGlobalTexture(TransparentPass.ColorBuff, TransparentPass.ColorBuff);
        renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, postMaterial, 0, 0);
        renderer.cmb.ReleaseTemporaryRT(TransparentPass.ColorBuff);

        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();
    }
}