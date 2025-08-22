using UnityEngine;
using UnityEngine.Rendering;

namespace PixRenderPipline
{
    public class FinalPass : PixPassBase
    {
        readonly int _Channel = Shader.PropertyToID("_Channel");
        readonly int _Size = Shader.PropertyToID("_Size");
        readonly int _DepthScale = Shader.PropertyToID("_DepthScale");
        readonly int _IsSceneView = Shader.PropertyToID("_IsSceneView");
        Material filterMaterial;
        Material debugMaterial;
        
        public FinalPass(PixRenderer renderer) : base("PixFinalPass", renderer)
        {
            filterMaterial = new Material(Shader.Find("Hidden/Pix/Filter"));
            debugMaterial = new Material(Shader.Find("Hidden/Pix/Debugger"));
        }

        public override void Execute()
        {
            TriggerEvent(PixRenderEventName.BeforeFinal);

            base.Execute();

            if(!filterMaterial)filterMaterial = new Material(Shader.Find("Hidden/Pix/Filter"));
            if(!debugMaterial)debugMaterial = new Material(Shader.Find("Hidden/Pix/Debugger"));


            var setting = PixRenderSetting.instance;

            filterMaterial.SetFloat(_IsSceneView, renderer.isSceneView ? 1 : 0);

            renderer.cmb.SetGlobalTexture(TransparentPass.ColorBuff, OpaqueRT);
            renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_0, GBufferPass.GbufferID_0);
            renderer.cmb.SetGlobalTexture(GBufferPass.GbufferID_1, GBufferPass.GbufferID_1);

    #if UNITY_EDITOR
            // 如果是Editor中的场景视图，不能使用Blit来画，反正我画不出来
            if (renderer.isSceneView)
            {
                renderer.cmb.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, filterMaterial);

                int debugMode = (int)setting.debugMode;
                if (debugMode > 0)
                {
                    debugMaterial.SetInt(_Channel, debugMode - 1);
                    debugMaterial.SetFloat(_Size, setting.debugSize);
                    debugMaterial.SetFloat(_DepthScale, setting.depthScale);

                    if (setting.enable_TAA)
                        debugMaterial.EnableKeyword("MOTION_VECTOR_ON");
                    else
                        debugMaterial.DisableKeyword("MOTION_VECTOR_ON");

                    renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, debugMaterial, 0, 0);
                }
            }
            else
    #endif
                renderer.cmb.Blit(OpaqueRT, BuiltinRenderTextureType.CameraTarget, filterMaterial);

            renderer.cmb.ReleaseTemporaryRT(OpaqueRT);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
    }
}