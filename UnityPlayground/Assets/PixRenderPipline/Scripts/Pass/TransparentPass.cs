using UnityEngine.Rendering;
using UnityEngine;

public class TransparentPass : PixPassBase
{
    public TransparentPass(PixRenderer renderer) : base("PixTransparentPass", renderer) { }

    static readonly ShaderTagId tagID = new("PixTransparent");
    
    public static readonly int ColorBuff = Shader.PropertyToID("_PixColorTex");
    
    public override void Execute()
    {
        GetTemporaryColorRT(ColorBuff);

        //先把之前Deferred渲染的结果拿来
        cmb.Blit(DeferredPass.ColorBuff, ColorBuff);

        cmb.SetRenderTarget(ColorBuff, EarlyZPass.depthID);
        cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
        cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);
        cmb.SetGlobalTexture(DeferredPass.ColorBuff, DeferredPass.ColorBuff);

        RendererList list = GetRendererList(tagID, SortingCriteria.CommonTransparent, RenderQueueRange.transparent);

        if (list.isValid)
            cmb.DrawRendererList(list);

        cmb.ReleaseTemporaryRT(EarlyZPass.nameID);

        renderer.context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }
}
