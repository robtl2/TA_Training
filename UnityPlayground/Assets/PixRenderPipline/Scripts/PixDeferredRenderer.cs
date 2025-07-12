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
    public DecalPass decalPass { get; private set; }
    public TransparentPass transparentPass { get; private set; }
    public PostProcessPass postProcessPass { get; private set; }
    public FinalPass finalPass { get; private set; }

    public PixDeferredRenderer()
    { 
        earlyZPass = new(this);
        gBufferPass = new(this);
        tiledPass = new(this);
        deferredPass = new(this);
        skyPass = new(this);
        decalPass = new(this);
        transparentPass = new(this);
        postProcessPass = new(this);
        finalPass = new(this);
    }

    public override void Render()
    {
        base.Render();

        earlyZPass.Execute();
        gBufferPass.Execute();
        tiledPass.Execute();
        deferredPass.Execute();

        if (PixSky.instance != null && PixSky.instance.skyType != PixSky.SkyType.None)
            skyPass.Execute();

        if (PixDecal.decals.Count > 0)
            decalPass.Execute();

        transparentPass.Execute();
        postProcessPass.Execute();
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