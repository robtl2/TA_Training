using UnityEngine.Rendering;

public class DecalPass : PixPassBase
{
    public DecalPass(PixRenderer renderer) : base("DecalPass", renderer) { }

    readonly ShaderTagId[] tagIDs = new ShaderTagId[] {
        new("PixDecal_Stencil_Front"),
        new("PixDecal_Stencil_Back"),
        new("PixDecal_Main"),
    };

    public override void Execute()
    {
        base.Execute();

        renderer.cmb.SetRenderTarget(DeferredPass.ColorBuff, EarlyZPass.depthID);

        foreach (var tagID in tagIDs)
        {
            RendererList list = GetRendererList(tagID, SortingCriteria.CommonTransparent, RenderQueueRange.transparent);
            if (list.isValid)
                renderer.cmb.DrawRendererList(list);
        }

        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();
    }
}