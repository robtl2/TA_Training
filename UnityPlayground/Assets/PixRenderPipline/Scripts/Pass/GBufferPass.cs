using UnityEngine.Rendering;
using UnityEngine;

public class GBufferPass : PixPassBase
{
    static readonly ShaderTagId gBufferTag = new("PixGBuffer");
    public static readonly int GbufferID_0 = Shader.PropertyToID("_PixGBuffer0");
    public static readonly int GbufferID_1 = Shader.PropertyToID("_PixGBuffer1");

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
        GetTemporaryColorRT(GbufferID_0);
        GetTemporaryColorRT(GbufferID_1);

        cmb.SetRenderTarget(gbuffers, EarlyZPass.depthID);
        cmb.ClearRenderTarget(false, true, black);

        RendererList list = GetRendererList(gBufferTag, SortingCriteria.CommonOpaque, RenderQueueRange.opaque);

        if (list.isValid)
            cmb.DrawRendererList(list);

        renderer.context.ExecuteCommandBuffer(cmb);
        cmb.Clear();
    }
}
