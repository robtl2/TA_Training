using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System.Collections.Generic;

/// <summary>
/// Renderer负责按设计方案依次调度各个Pass
/// 把渲染管线想象成做菜的话，那这里主要的功能就是写菜单
/// 菜单写好后最后提交给厨房就好了 
/// </summary>
namespace PixRenderPipline
{
    public class PixRenderer
    {
        public enum SamplerQuality
        {
            Poor = 0,
            Low = 1,
            Medium = 2,
            High = 3,
        }

        /// <summary>
        /// 这就是那个菜单
        /// </summary>
        public ScriptableRenderContext context { get; private set; }

        /// <summary>
        /// commandBuffer就像是写制作流程的笔
        /// pass通过这个commandBuffer把流程写到上面的菜单里 
        /// </summary>
        public CommandBuffer cmb { get; private set; }

        /// <summary>
        /// 当前客户
        /// </summary>
        public Camera camera { get; private set; }

        /// <summary>
        /// 相机的Frustum Planes
        /// </summary>
        public Plane[] frustum { get; private set; }

        /// <summary>
        /// 把asset拿手上才知道参数
        /// </summary>
        public PixRenderPiplineAsset asset { get; private set; }

        /// <summary>
        /// 是否是unityEditor中的Scene相机
        /// </summary>
        public bool isSceneView { get; private set; }

        /// <summary>
        /// 绘制非UI元素时RenderTexture的色彩空间
        /// 这里可以切换纯为了讲课 
        /// </summary>
        public RenderTextureReadWrite colorSpace { get; private set; }

        /// <summary>
        /// 绘制尺寸
        /// </summary>
        public int4 size { get; private set; }

        /// <summary>
        /// TiledPass绘制尺寸
        /// </summary>
        public int2 tiledSize { get; private set; }

        /// <summary>
        /// 相机剔除结果
        /// </summary>
        public CullingResults cullingResults { get; private set; }

        /// <summary>
        /// 相机剔除是否成功
        /// </summary>
        public bool cullingSuccess { get; private set; }

        public int frameIndex{get; private set; }

        /// <summary>
        /// 上一帧后处理之前的渲染结果
        /// </summary>
        public Dictionary<Camera, RenderTexture> frontRT = new();

        public PixRenderer()
        {
            cmb = new();
            cmb.name = "PixRenderer";
        }

        public void Setup(ScriptableRenderContext context, Camera camera, PixRenderPiplineAsset asset, int frameIndex)
        {
            this.context = context;
            this.camera = camera;
            this.asset = asset;
            this.frameIndex = frameIndex;

            frustum = GeometryUtility.CalculateFrustumPlanes(camera);
        }
        
        /// <summary>
        /// 相机剔除
        /// </summary>
        public bool FrustumCull(Bounds bounds)
        {
            return GeometryUtility.TestPlanesAABB(frustum, bounds);
        }

        /// <summary>
        /// 基类的Render方法就是把写菜单前的手续先办好
        /// </summary>
        public virtual void Render()
        {
            size = asset.GetRenderSize(camera.aspect);
            tiledSize = size.xy / 8;

            colorSpace = RenderTextureReadWrite.Linear;

#if UNITY_EDITOR
            isSceneView = camera.cameraType == CameraType.SceneView;
#endif

            // 计算相机剔除结果
            cullingSuccess = false;
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParams))
            {
                cullingResults = context.Cull(ref cullingParams);
                cullingSuccess = true;
            }

            var setting = PixRenderSetting.instance;
            if (setting.enable_TAA)
                PrepairTAA();

            context.SetupCameraProperties(camera);
            SetupGlobalUniform();
        }

        public virtual void CleanUp()
        { 
            foreach (var rt in frontRT)
            {
                if (rt.Value != null && rt.Value.IsCreated())
                    rt.Value.Release();
            }

            frontRT.Clear();
        }

        void PrepairTAA()
        { 
            // TAA用到的这张历史记录的颜色贴图之所以不用临时RT，是因为它会在下一帧时还得用到
            if (!frontRT.ContainsKey(camera) || !frontRT[camera] || frontRT[camera].width != size.x || frontRT[camera].height != size.y)
            {
                if (frontRT.ContainsKey(camera) && frontRT[camera])
                    frontRT[camera].Release();

                frontRT[camera] = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                {
                    name = "_ColorTex_Front"
                };
                frontRT[camera].Create();
            }
        }
        
        #region GlobalUniforms
        /// <summary>
        /// VP矩阵的逆矩阵
        /// unity URP中的内置命名也是这个
        /// </summary>
        readonly int MATRIX_I_VP = Shader.PropertyToID("unity_MatrixInvVP");

        /// <summary>
        /// 因为是自己做TAA，所以VP得自己来抖
        /// 这个名字当然是有讲究的，就是unity内置VP矩阵的名字
        /// </summary>
        readonly int MATRIX_VP = Shader.PropertyToID("_MatrixVP");
        
        readonly int _MatrixVP_Prev = Shader.PropertyToID("_MatrixVP_Prev");
        readonly int _AO_Factor = Shader.PropertyToID("_AO_Factor");
        readonly int _SSAO_Props = Shader.PropertyToID("_SSAO_Props");
        readonly int _SSAO_Clip = Shader.PropertyToID("_SSAO_Clip");
        readonly int _SSAO_Props_2nd = Shader.PropertyToID("_SSAO_Props_2nd");
        readonly int _TAA_Jitter = Shader.PropertyToID("_TAA_Jitter");
        readonly int _ScreenTexelSize = Shader.PropertyToID("_ScreenTexelSize");
        readonly int _DebugBrightness = Shader.PropertyToID("_DebugBrightness");
        
        // 相机上一帧的VP
        static Dictionary<Camera, Matrix4x4> VP_pre_map = new();
        static Matrix4x4 VP = Matrix4x4.identity;
        
        static float2[] haltonSamples = new float2[]
        {
            new (0.0f, 0.0f),
            new (-0.5f, 0.5f),
            new (0.5f, -0.5f),
            new (-0.75f, 0.25f),
            new (0.25f, -0.75f),
            new (-0.25f, 0.75f),
            new (0.75f, -0.25f),
            new (-0.875f, 0.125f)
        };

        public bool jitterUploaded = false;
        float2 taaJitter = float2.zero;

        protected virtual void SetupGlobalUniform()
        {
            var setting = PixRenderSetting.instance;

            if (setting.pipeline == PixRenderSetting.Pipeline.Forward)
            {
                Shader.EnableKeyword("FORWARD_PIPELINE");
                Shader.DisableKeyword("DEFERRED_PIPELINE");
            }
            else
            {
                Shader.EnableKeyword("DEFERRED_PIPELINE");
                Shader.DisableKeyword("FORWARD_PIPELINE");
            }

            // if (setting.style == PixRenderSetting.Style.PBR)
            // {
            //     Shader.EnableKeyword("PIX_STYLE_PBR");
            //     Shader.DisableKeyword("PIX_STYLE_NPR");
            // }
            // else
            // {
            //     Shader.EnableKeyword("PIX_STYLE_NPR");
            //     Shader.DisableKeyword("PIX_STYLE_PBR");
            // }

            if(setting.Enable_Fog)
            {
                Shader.EnableKeyword("FOG");
                Shader.SetGlobalVector("_FogParams", setting.FogParams);
                Shader.SetGlobalColor("_FogColor", setting.FogColor);
            }
            else
            {
                Shader.DisableKeyword("FOG");
            }

            if (camera.orthographic)
                Shader.EnableKeyword("ORTHOGRAPHIC");
            else
                Shader.DisableKeyword("ORTHOGRAPHIC");

             Shader.SetGlobalFloat(_AO_Factor, setting.ao_factor);

            if (setting.Enable_SSAO)
            {
                Vector4 ssao_props = new();
                ssao_props.x = setting.ssao_factor;
                ssao_props.y = setting.ssao_radius;
                ssao_props.z = setting.ssao_factor_2nd;
                if (!setting.Enable_SSAO_2nd)
                    ssao_props.z = 0;
                ssao_props.w = setting.ssao_radius_2nd;
                Shader.SetGlobalVector(_SSAO_Props, ssao_props);

                Vector3 ssao_clip = new();
                ssao_clip.x = setting.ssao_clipByDistance;
                ssao_clip.y = setting.ssao_clipByDistance_2nd;
                Shader.SetGlobalVector(_SSAO_Clip, ssao_clip);

                Shader.DisableKeyword("SSAO_QUALITY_OFF");
                Shader.DisableKeyword("SSAO_QUALITY_POOR");
                Shader.DisableKeyword("SSAO_QUALITY_LOW");
                Shader.DisableKeyword("SSAO_QUALITY_MEDIUM");
                Shader.DisableKeyword("SSAO_QUALITY_HIGH");

                switch (setting.ssao_quality)
                {
                    case SamplerQuality.Poor:
                        Shader.EnableKeyword("SSAO_QUALITY_POOR");
                        break;
                    case SamplerQuality.Low:
                        Shader.EnableKeyword("SSAO_QUALITY_LOW");
                        break;
                    case SamplerQuality.Medium:
                        Shader.EnableKeyword("SSAO_QUALITY_MEDIUM");
                        break;
                    case SamplerQuality.High:
                        Shader.EnableKeyword("SSAO_QUALITY_HIGH");
                        break;
                }
            }
            else
            {
                Shader.EnableKeyword("SSAO_QUALITY_OFF");
                Shader.DisableKeyword("SSAO_QUALITY_POOR");
                Shader.DisableKeyword("SSAO_QUALITY_LOW");
                Shader.DisableKeyword("SSAO_QUALITY_MEDIUM");
                Shader.DisableKeyword("SSAO_QUALITY_HIGH");
            }

            if (setting.enable_TAA)
                Shader.EnableKeyword("TAA");
            else
                Shader.DisableKeyword("TAA");

            if(setting.useDebugMaterial)
                Shader.EnableKeyword("DEBUG_LIGHT");
            else
                Shader.DisableKeyword("DEBUG_LIGHT");


            Shader.SetGlobalFloat(_DebugBrightness, setting.materialBrightness);
            Shader.SetGlobalVector(_ScreenTexelSize, new Vector4(1.0f / size.x, 1.0f / size.y, 1.0f/size.z, 1.0f/size.w));

            if (!jitterUploaded)
            {
                jitterUploaded = true;

                int jitterIndex = (frameIndex + 1) % haltonSamples.Length;
                taaJitter = haltonSamples[jitterIndex];

                taaJitter *= setting.TAA_jitter;

                if (setting.enable_TAA)
                    Shader.SetGlobalVector(_TAA_Jitter, new Vector2(taaJitter.x, taaJitter.y));
            }

            // 把VP的逆矩阵传给Shader
            // 不要以为URP有就代表SRP有，这里得自己传
            Matrix4x4 V = camera.worldToCameraMatrix;
            Matrix4x4 P = camera.projectionMatrix;

            // 只需要抖P矩阵
            if (setting.enable_TAA)
            {
                float jitterX = taaJitter.x / size.x;
                float jitterY = taaJitter.y / size.y;
                P.m02 += jitterX;
                P.m12 += jitterY;

                Shader.EnableKeyword("TAA");
            }
            else
            {
                Shader.DisableKeyword("TAA");
            }

            P = GL.GetGPUProjectionMatrix(P, true);
            VP = P * V;

            Shader.SetGlobalMatrix(MATRIX_VP, VP);

            Matrix4x4 iVP = VP.inverse;
            Shader.SetGlobalMatrix(MATRIX_I_VP, iVP);

            if (!VP_pre_map.ContainsKey(camera))
                VP_pre_map[camera] = VP;

            Shader.SetGlobalMatrix(_MatrixVP_Prev, VP_pre_map[camera]);
            VP_pre_map[camera] = VP;

            // 以后缺什么补什么
        }
        #endregion
    }
}
