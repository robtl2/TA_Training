using UnityEngine.Rendering;
using UnityEngine;
using UnityEditor.VersionControl;

namespace PixRenderPipline
{
    public class TransparentPass : PixPassBase
    {
        public TransparentPass(PixRenderer renderer) : base("PixTransparentPass", renderer)
        {
            materialTAA = new Material(Shader.Find("Hidden/Pix/TAA"));
        }

        readonly ShaderTagId[] tagID = new ShaderTagId[] {
            new("PixTransparent"),
        };

        Material materialTAA;
        
        public static readonly int ColorBuff = Shader.PropertyToID("_PixColorTex");
        public static readonly int ColorBuff_Front = Shader.PropertyToID("_PixColorTex_Front");
        static int _HistroyWeight = Shader.PropertyToID("_HistroyWeight");
        
        public override void Execute()
        {
            base.Execute();

            GetTemporaryColorRT(ColorBuff);
            //先把之前Deferred渲染的结果复制过来
            renderer.cmb.Blit(DeferredPass.ColorBuff, ColorBuff);
            renderer.cmb.SetRenderTarget(ColorBuff, EarlyZPass.depthID);

            TriggerEvent(PixRenderEventName.BeforeTransparent);

            RendererList list = GetRendererList(tagID, SortingCriteria.CommonTransparent, RenderQueueRange.transparent);
            if (list.isValid)
                renderer.cmb.DrawRendererList(list);

            TriggerEvent(PixRenderEventName.AfterTransparent);



            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
    }
}
