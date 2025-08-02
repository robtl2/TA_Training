using UnityEngine.Rendering;

namespace PixRenderPipline
{
    public class OcclusionCullingPass : PixPassBase
    {
        public OcclusionCullingPass(PixRenderer renderer) : base("PixOcclusionCullingPass", renderer)
        {

        }

        // TODO: fullfil
        public override void Execute()
        {
            if (!renderer.asset.GPU_OCC_Culling) return;
            base.Execute();


        }
    }
}