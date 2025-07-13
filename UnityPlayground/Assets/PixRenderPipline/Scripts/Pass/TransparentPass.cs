using UnityEngine.Rendering;
using UnityEngine;

public class TransparentPass : PixPassBase
{
    public TransparentPass(PixRenderer renderer) : base("PixTransparentPass", renderer) { }

    readonly ShaderTagId tagID = new("PixTransparent");
    
    public static readonly int ColorBuff = Shader.PropertyToID("_PixColorTex");
    
    public override void Execute()
    {
        TriggerEvent(PixRenderEventName.BeforeTransparent);
        base.Execute();

        GetTemporaryColorRT(ColorBuff);

        //先把之前Deferred渲染的结果复制过来
        renderer.cmb.Blit(DeferredPass.ColorBuff, ColorBuff);

        renderer.cmb.SetRenderTarget(ColorBuff, EarlyZPass.depthID);
        renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
        renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);
        renderer.cmb.SetGlobalTexture(DeferredPass.ColorBuff, DeferredPass.ColorBuff);

        RendererList list = GetRendererList(tagID, SortingCriteria.CommonTransparent, RenderQueueRange.transparent);

        if (list.isValid)
            renderer.cmb.DrawRendererList(list);

        renderer.cmb.ReleaseTemporaryRT(EarlyZPass.nameID);

        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();

        TriggerEvent(PixRenderEventName.AfterTransparent);
    }
}
