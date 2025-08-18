using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace PixRenderPipline
{
    public class DownSamplingPass : PixPassBase
    {
        public DownSamplingPass(PixRenderer renderer) : base("PixDownSamplingPass", renderer)
        { 
            material = new Material(Shader.Find("Hidden/Pix/DownSampling"));
        }
        public static readonly int rtID = Shader.PropertyToID("_PixDownSampling");
        public Material material;
        public override void Execute()
        {
            base.Execute();
            if(!material)material = new Material(Shader.Find("Hidden/Pix/DownSampling"));

            // 现在这个下采样Pass暂时只画SSAO和VolumeSunLight，所以只需要用到两个Channal,以后还有别的记得上这里来改格式
            renderer.cmb.GetTemporaryRT(rtID, renderer.size.z, renderer.size.w, 0, FilterMode.Bilinear, RenderTextureFormat.RG16, RenderTextureReadWrite.Linear);
            renderer.cmb.SetRenderTarget(rtID);

            renderer.cmb.DrawMesh(FullScreenQuad, Matrix4x4.identity, material, 0, 0);

            renderer.cmb.SetGlobalTexture(rtID, rtID);

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }
    }
}