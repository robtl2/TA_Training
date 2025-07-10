using UnityEngine;

public class PostProcessPass : PixPassBase
{
    public PostProcessPass(PixRenderer renderer) : base("PixPostProcessPass", renderer) { }

    Material postMaterial;

    public override void Execute()
    {
        if (postMaterial == null)
            postMaterial = new Material(Shader.Find("Pix/Post"));

        cmb.SetRenderTarget(DeferredPass.ColorBuff);
        cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
        cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);
        cmb.SetGlobalTexture(TransparentPass.ColorBuff, TransparentPass.ColorBuff);
        cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, postMaterial, 0, 0);
        cmb.ReleaseTemporaryRT(TransparentPass.ColorBuff);

        renderer.context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }
}