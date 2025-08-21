using UnityEngine.Rendering;

/// <summary>
/// 这里的Forward管线是在EarlyZ阶段把Normal也画了，画Normal时会有点OverDraw, 
/// ForwardEarlyZ阶段因为要进象素，所以这个阶段会比Deferred的EarlyZ贵一点
/// 不过后面流程中的带宽就血赚了
/// 反正就是试试嘛
/// </summary>
namespace PixRenderPipline
{
    public class PixForwardRenderer : PixRenderer
    {
        public ForwardEarlyZPass forwardEarlyZPass { get; private set; }
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
            forwardEarlyZPass = new(this);
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
            
            ExecutePass(forwardEarlyZPass);
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