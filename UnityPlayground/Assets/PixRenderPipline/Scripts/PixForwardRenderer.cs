using UnityEngine.Rendering;

/// <summary>
/// 试试Forward管线，
/// 帧率还掉了好几帧，就离谱
/// </summary>
namespace PixRenderPipline
{
    public class PixForwardRenderer : PixRenderer
    {
        public EarlyZPass earlyZPass { get; private set; }
        public OcclusionCullingPass occlusionCullingPass { get; private set; }
        public DownSamplingPass downSamplingPass { get; private set; }
        public OpaquePass opaquePass { get; private set; }
        public SkyPass skyPass { get; private set; }
        public DecalPass decalPass { get; private set; }
        public TransparentPass transparentPass { get; private set; }
        public PostProcessPass postProcessPass { get; private set; }
        public FinalPass finalPass { get; private set; }

        public PixForwardRenderer()
        {
            earlyZPass = new(this);
            occlusionCullingPass = new(this);
            downSamplingPass = new(this);
            opaquePass = new(this);
            skyPass = new(this);
            decalPass = new(this);
            transparentPass = new(this);
            postProcessPass = new(this);
            finalPass = new(this);
        }

        public override void Render()
        {
            base.Render();
            
            ExecutePass(earlyZPass);
            ExecutePass(occlusionCullingPass);
            ExecutePass(downSamplingPass);
            ExecutePass(opaquePass);
            ExecutePass(skyPass);
            ExecutePass(decalPass);
            ExecutePass(transparentPass);
            ExecutePass(postProcessPass);
            ExecutePass(finalPass);

            #if UNITY_EDITOR
            // 绘制编辑器视图中的Gizmos
            if (isSceneView)
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            #endif

            //菜单以及工艺都写完了，交还给厨房管事儿的
            context.Submit();
        }

        void ExecutePass(PixPassBase pass)
        {
            pass.Execute();
        }
       
    }
}