using UnityEngine.Rendering;
using UnityEngine;

public class GBufferPass : PixPassBase
{
    static readonly ShaderTagId gBufferTag = new("PixGBuffer");
    public static readonly int GbufferID_0 = Shader.PropertyToID("_PixGBuffer_0");
    public static readonly int GbufferID_1 = Shader.PropertyToID("_PixGBuffer_1");

    readonly RenderTargetIdentifier[] gbuffers;

    public GBufferPass(PixRenderer renderer) : base("PixGBufferPass", renderer)
    {
        gbuffers = new RenderTargetIdentifier[] {
            new(GbufferID_0),
            new(GbufferID_1),
        };
    }

    public override void Execute()
    {
        base.Execute();
        GetTemporaryColorRT(GbufferID_0);
        GetTemporaryColorRT(GbufferID_1);

        renderer.cmb.SetRenderTarget(gbuffers, EarlyZPass.depthID);
        renderer.cmb.ClearRenderTarget(false, true, black);

        RendererList list = GetRendererList(gBufferTag, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);

        if (list.isValid)
            renderer.cmb.DrawRendererList(list);

        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();
    }
}
