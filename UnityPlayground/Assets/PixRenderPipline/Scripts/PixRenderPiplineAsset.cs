using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

/// <summary>
/// 渲染管线的序列化资源
/// 管线中用到的各种参数
/// </summary>
namespace PixRenderPipline
{
    [CreateAssetMenu(menuName = "Rendering/PixRenderPipline")]
    public class PixRenderPiplineAsset : RenderPipelineAsset<PixRenderPipline>
    {
        #region main
        public enum RenderSize
        {
            _240p,
            _320p,
            _480p,
            _640p,
            _720p,
        }
        public RenderSize renderSize;

        public enum ColorSpace
        {
            Gamma,
            Linear,
        }
        public ColorSpace colorSpace = ColorSpace.Linear;
    #endregion

    #region GBuffer Debug
        public enum DebugMode
        {
            None,
            Albedo,
            PositionWS,
            NormalWS,
            // TrueNormal,
            NormalVS,
            ViewDir,
            NdotV,
            Depth,
        }

        [Header("GBuffer Debug")]
        public DebugMode debugMode = DebugMode.None;

        [Range(0, 1)]
        public float debugSize = 1;

    #endregion

        protected override RenderPipeline CreatePipeline()
        {
            return new PixRenderPipline(this);
        }

        public int2 GetRenderSize(float aspect)
        {
            int h = 0;
            switch (renderSize)
            {
                case RenderSize._240p:
                    h = 240;
                    break;
                case RenderSize._320p:
                    h = 320;
                    break;
                case RenderSize._480p:
                    h = 480;
                    break;
                case RenderSize._640p:
                    h = 640;
                    break;
                case RenderSize._720p:
                    h = 720;
                    break;
            }
            int2 size = new((int)(h * aspect), h);
            // 让size.x是8的倍数
            size.x = (size.x + 7) / 8 * 8;
            
            // 确保尺寸不为零
            if (size.x < 8) size.x = 8;
            if (size.y < 8) size.y = 8;
            
            return size;
        }
    }
}
