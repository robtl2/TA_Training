using UnityEngine;
using UnityEngine.Rendering;

public class FinalPass : PixPassBase
{
    public FinalPass(PixRenderer renderer) : base("PixFinalPass", renderer) { }

    Material filterMaterial;

    Material debugMaterial;

    static readonly int CHANNEL_ID = Shader.PropertyToID("_Channel");
    static readonly int SIZE_ID = Shader.PropertyToID("_Size");

    public override void Execute()
    {
        base.Execute();
        if (filterMaterial == null)
            filterMaterial = new Material(Shader.Find("Pix/Filter"));

        filterMaterial.SetFloat("_IsSceneView", renderer.isSceneView ? 1 : 0);

        renderer.cmb.SetGlobalTexture(TransparentPass.ColorBuff, DeferredPass.ColorBuff);
        renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
        renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);


#if UNITY_EDITOR
        // 如果是Editor中的场景视图，不能使用Blit来画，反正我画不出来
        if (renderer.isSceneView)
        {
            renderer.cmb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, filterMaterial);

            int debugMode = (int)renderer.asset.debugMode;
            if (debugMode > 0)
            {
                if (debugMaterial == null)
                    debugMaterial = new Material(Shader.Find("Pix/Debugger"));

                debugMaterial.SetInt(CHANNEL_ID, debugMode - 1);
                debugMaterial.SetFloat(SIZE_ID, renderer.asset.debugSize);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, debugMaterial, 0, 0);
            }
        }
        else
#endif
            renderer.cmb.Blit(DeferredPass.ColorBuff, BuiltinRenderTextureType.CameraTarget, filterMaterial);

        renderer.cmb.ReleaseTemporaryRT(DeferredPass.ColorBuff);
        renderer.cmb.ReleaseTemporaryRT(GBufferPass.GbufferID_0);
        renderer.cmb.ReleaseTemporaryRT(GBufferPass.GbufferID_1);

        renderer.context.ExecuteCommandBuffer(renderer.cmb);
        renderer.cmb.Clear();
    }
}