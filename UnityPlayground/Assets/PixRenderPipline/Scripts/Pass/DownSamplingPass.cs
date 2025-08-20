using UnityEngine;

namespace PixRenderPipline
{
    /// <summary>
    /// 体积光，SSAO, Shadow，这些东西都放在这个降采样的Pass提前画好
    /// 质量肯定会有点损失，但谁让我这么抠呢
    /// </summary>
    public class DownSamplingPass : PixPassBase
    {
        public DownSamplingPass(PixRenderer renderer) : base("PixDownSamplingPass", renderer)
        {
            material = new Material(Shader.Find("Hidden/Pix/DownSampling"));
        }
        public static readonly int rtID = Shader.PropertyToID("_PixDownSampling");
        static int _BlurDownsample = Shader.PropertyToID("_BlurDownsample");
        static int blurRT = Shader.PropertyToID("_PixDownSamplingBlur");
        public Material material;
        public override void Execute()
        {
            base.Execute();
            if (!material) material = new Material(Shader.Find("Hidden/Pix/DownSampling"));

            var setting = PixRenderSetting.instance;

            renderer.cmb.GetTemporaryRT(rtID, renderer.size.z, renderer.size.w, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            renderer.cmb.SetRenderTarget(rtID);

            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, material, 0, 0);

            if (setting.blurDownsample > 0)
            {
                renderer.cmb.SetGlobalFloat(_BlurDownsample, setting.blurDownsample);

                renderer.cmb.GetTemporaryRT(blurRT, renderer.size.z, renderer.size.w, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                renderer.cmb.SetGlobalTexture(rtID, rtID);
                renderer.cmb.SetRenderTarget(blurRT);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, material, 0, 1);

                renderer.cmb.SetRenderTarget(rtID);
                renderer.cmb.SetGlobalTexture(blurRT, blurRT);
                renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, material, 0, 2);

                renderer.cmb.ReleaseTemporaryRT(blurRT);
            }

            renderer.cmb.SetGlobalTexture(rtID, rtID);
            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
    }
}