using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

/// <summary>
/// Renderer负责按设计方案依次调度各个Pass
/// 把渲染管线想象成做菜的话，那这里主要的功能就是写菜单
/// 菜单写好后最后提交给厨房就好了 
/// </summary>
namespace PixRenderPipline
{
    public class PixRenderer
    {
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
        public int2 size { get; private set; }

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

        public PixRenderer()
        {
            cmb = new();
            cmb.name = "PixRenderer";
        }

        public void Setup(ScriptableRenderContext context, Camera camera, PixRenderPiplineAsset asset)
        {
            this.context = context;
            this.camera = camera;
            this.asset = asset;

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
            tiledSize = size / 8;

            colorSpace = RenderTextureReadWrite.Linear;
            if (asset.colorSpace == PixRenderPiplineAsset.ColorSpace.Gamma)
                colorSpace = RenderTextureReadWrite.sRGB;

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

            context.SetupCameraProperties(camera);
            SetupGlobalUniform();
        }
        
        #region GlobalUniforms
        /// <summary>
        /// VP矩阵的逆矩阵
        /// unity URP中的内置命名也是这个
        /// </summary>
        readonly int MATRIX_I_VP = Shader.PropertyToID("unity_MatrixInvVP");

        protected virtual void SetupGlobalUniform()
        {
            // 把VP的逆矩阵传给Shader
            // 不要以为URP有就代表SRP有，这里得自己传
            Matrix4x4 V = camera.worldToCameraMatrix;
            Matrix4x4 P = camera.projectionMatrix;
            P = GL.GetGPUProjectionMatrix(P, true);
            Matrix4x4 VP = P * V;
            Matrix4x4 iVP = VP.inverse;
            Shader.SetGlobalMatrix(MATRIX_I_VP, iVP);

            // 以后缺什么补什么
        }
        #endregion
    }
}
