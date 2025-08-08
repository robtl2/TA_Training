using UnityEngine;

namespace PixRenderPipline
{
    [ExecuteInEditMode]
    public class PixRenderSetting : MonoBehaviour
    {
        public static PixRenderSetting instance;


        #region main
        public enum Style
        {
            PBR,
            NPR,
        }

        public Style style;

        public enum ColorSpace
        {
            Gamma,
            Linear,
        }
        public ColorSpace colorSpace = ColorSpace.Linear;

        public bool GPU_OCC_Culling = true;

        [Header("AO")]
        [Range(0, 1)]
        public float ao_factor = 1;
        public bool Enable_SSAO = true;

        [Range(0, 1)]
        public float ssao_factor = 1.0f;
        public float ssao_radius = 0.01f;
        public int ssao_stepCount = 4;
        public float ssao_jitterRadius = 1.0f;

        public PixRenderer.SamplerQuality ssao_quality = PixRenderer.SamplerQuality.Low;

        #endregion

        #region PostProcessing
        [Header("PostProcessing")]
        [Range(0, 3)]
        public float SharpenStrength = 2.0f;
        [Range(0f, 1.0f)]
        public float SharpenMid = 0.2f; 

        [Space(10)]

        [Range(0, 3)]
        public float Exposure = 1.0f;

        [Space(10)]
        public bool EnableBloom = true;
        public float Bloom_Threshold = 0.5f;
        public float Bloom_Radius = 1.0f;
        public float Bloom_Intensity = 1.0f;

        [Space(10)]
        public bool Enable_Tonemapping = true;
        #endregion

        #region TAA
        [Header("TAA")]
        public bool enable_TAA = false;

        [Range(0, 1)]
        public float TAA_jitter = 1.0f;

        [Range(0, 0.995f)]
        public float TAA_histroy = 0.8f;

        #endregion


        #region GBuffer Debug
        public enum DebugMode
        {
            None,
            Albedo,
            Diffuse,
            F0,
            AO,
            BentNormal,
            PositionWS,
            NormalWS,
            // TrueNormal,
            NormalVS,
            TangentWS,
            ViewDir,
            NdotV,
            Depth,
            MotionVector,
        }

        [Header("GBuffer Debug")]
        public DebugMode debugMode = DebugMode.None;

        [Range(0, 1)]
        public float debugSize = 1;

        #endregion

        void OnEnable()
        {
            instance = this;
        }

        void OnDisable()
        {
            instance = null;
        }

    }
}
