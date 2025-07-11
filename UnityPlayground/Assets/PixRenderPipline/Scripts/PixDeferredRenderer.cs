using UnityEngine.Rendering;

/// <summary>
/// 呐，几乎干的所有事情就是写下来我给的菜品方案而已
/// 然后每样菜写之前和写之后还顺便告诉下别人，这时别人还能再加点料到菜单上
/// </summary>
public class PixDeferredRenderer : PixRenderer
{
    public EarlyZPass earlyZPass { get; private set; }
    public GBufferPass gBufferPass { get; private set; }
    public TiledPass tiledPass { get; private set; }
    public DeferredPass deferredPass { get; private set; }
    public SkyPass skyPass { get; private set; }
    public TransparentPass transparentPass { get; private set; }
    public PostProcessPass postProcessPass { get; private set; }
    public FinalPass finalPass { get; private set; }

    public PixDeferredRenderer() { }

    public override void Render()
    {
        base.Render();

        earlyZPass ??= new EarlyZPass(this);
        PixRenderEvent.TriggerEvent(PixRenderEventName.BeforeEarlyZ, this);
        earlyZPass.Execute();
        PixRenderEvent.TriggerEvent(PixRenderEventName.AfterEarlyZ, this);

        gBufferPass ??= new GBufferPass(this);
        PixRenderEvent.TriggerEvent(PixRenderEventName.BeforeGBuffer, this);
        gBufferPass.Execute();
        PixRenderEvent.TriggerEvent(PixRenderEventName.AfterGBuffer, this);

        tiledPass ??= new TiledPass(this);
        PixRenderEvent.TriggerEvent(PixRenderEventName.BeforeTiled, this);
        tiledPass.Execute();
        PixRenderEvent.TriggerEvent(PixRenderEventName.AfterTiled, this);

        deferredPass ??= new DeferredPass(this);
        PixRenderEvent.TriggerEvent(PixRenderEventName.BeforeDeferred, this);
        deferredPass.Execute();
        PixRenderEvent.TriggerEvent(PixRenderEventName.AfterDeferred, this);

        if (asset.skyMaterial != null)
        {
            skyPass ??= new SkyPass(this);
            skyPass.Execute();
        }

        transparentPass ??= new TransparentPass(this);
        PixRenderEvent.TriggerEvent(PixRenderEventName.BeforeTransparent, this);
        transparentPass.Execute();
        PixRenderEvent.TriggerEvent(PixRenderEventName.AfterTransparent, this);

        postProcessPass ??= new PostProcessPass(this);
        PixRenderEvent.TriggerEvent(PixRenderEventName.BeforePostProcess, this);
        postProcessPass.Execute();
        PixRenderEvent.TriggerEvent(PixRenderEventName.AfterPostProcess, this);

        finalPass ??= new FinalPass(this);
        PixRenderEvent.TriggerEvent(PixRenderEventName.BeforeFinal, this);
        finalPass.Execute();

#if UNITY_EDITOR
        // 绘制编辑器视图中的Gizmos
        if (isSceneView)
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
#endif

        //菜单以及工艺都写完了，交还给厨房管事儿的
        context.Submit();
    }


}