using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PixRenderPipline
{
    [ExecuteInEditMode]
    public class PixRenderSetting : MonoBehaviour
    {
        public static PixRenderSetting instance;


       
        public enum Style
        {
            PBR,
            NPR,
        }

        public enum DownSampleSize
        {
            Div2,
            Div4,
            Div8,
        }

        #region main
        public Style style;

        public bool GPU_OCC_Culling = true;

        public FilterMode defaultFilterMode = FilterMode.Bilinear;
        public DownSampleSize downSampleSize = DownSampleSize.Div2;
        [Range(0, 3)]
        public float blurDownsample = 1.0f;
        #endregion

        #region Fog
        [Header("Fog")]
        public bool Enable_Fog = true;
        public Vector4 FogParams = new Vector4(0.1f, 0.1f, 200f, 10f); // Density, Start, End, Height
        public Color FogColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        #endregion

        #region AO
        [Header("AO")]
        [Range(0, 1)]
        public float ao_factor = 1;

        [Space(10)]
        public bool Enable_SSAO = true;
        public PixRenderer.SamplerQuality ssao_quality = PixRenderer.SamplerQuality.Low;
        [Range(0, 1)]
        public float ssao_factor = 1.0f;
        public float ssao_radius = 8f;
        public float ssao_clipByDistance = 0;

        [Space(10)]

        public bool Enable_SSAO_2nd = false;
        [Range(0, 1)]
        public float ssao_factor_2nd = 1.0f;
        public float ssao_radius_2nd = 4f;
        public float ssao_clipByDistance_2nd = 0;
       
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
        [Range(0, 1)]
        public float Vagnet = 1.0f;

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

        [Range(0, 1)]
        public float depthScale = 1.0f;

        [Space(10)]
        public bool useDebugMaterial = false;
        [Range(0,1)]
        public float materialBrightness = 0.5f;

        [Space(10)]
        public bool showInstancingBounds = true;
        
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
