using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace PixRenderPipline
{

    [ExecuteInEditMode]
    public class PixLight : MonoBehaviour
    {
        public const int MAX_LIGHT_COUNT = 64;

        public static PixLight mainLight;
        public static List<PixLight> lights = new();
        static bool lightListIsDirty = true;

        static int lightCount = 0;
        static Vector4[] shadowmapSizePropList = new Vector4[MAX_LIGHT_COUNT];
        static Vector4[] positionPropList = new Vector4[MAX_LIGHT_COUNT];
        static Vector4[] dirctionPropList = new Vector4[MAX_LIGHT_COUNT];
        static Vector4[] colorPropList = new Vector4[MAX_LIGHT_COUNT];
        static Vector4[] contactShadowPropList = new Vector4[MAX_LIGHT_COUNT];
        static Vector4[] shadowMapPropList = new Vector4[MAX_LIGHT_COUNT];
        static Matrix4x4[] shadowMapMatrixVP = new Matrix4x4[MAX_LIGHT_COUNT];
        static readonly int _PixLightCount = Shader.PropertyToID("_PixLightCount");

        static readonly int _PixLightsShadowMapSize = Shader.PropertyToID("_PixLightsShadowMapSize");
        static readonly int _PixLightsPosition = Shader.PropertyToID("_PixLightsPosition");
        static readonly int _PixLightsDirection = Shader.PropertyToID("_PixLightsDirection");
        static readonly int _PixLightsColor = Shader.PropertyToID("_PixLightsColor");
        static readonly int _PixLightsContactShadow = Shader.PropertyToID("_PixLightsContactShadow");
        static readonly int _PixLightsShadowMap = Shader.PropertyToID("_PixLightsShadowMap");
        static readonly int _PixLights_VP = Shader.PropertyToID("_PixLights_VP");

        public enum LightType
        {
            Directional,
            Spot,
            Point
        }

        /// <summary>
        /// 主光源才有实时ShadowMap
        /// </summary>
        public enum ShadowMapType
        {
            None,
            Hard,
            PCF,
            PCSS,
        }

        public enum ShadowMapArea
        {
            Camera,
            Box_Area,
        }

        public enum SampleQuality
        {
            Low,
            Medium,
            High,
        }

        public LightType lightType = LightType.Directional;

        public SampleQuality shadowMapQuality = SampleQuality.Medium;

        public Color color = Color.white;
        public float intensity = 10;
        public float spotAngle;

        [Header("ShadowMap")]
        public ShadowMapType shadowMapType = ShadowMapType.None;
        public int shadowMapSize = 512;

        [Range(0.00001f, 0.03f)]
        public float shadowMapBias = 0.0001f;
        public ShadowMapArea shadowMapArea = ShadowMapArea.Box_Area;
        public BoxCollider boxArea;

        [Header("Contact Shadow")]
        public bool enableContactShadow = false;

        [Range(0f, 0.01f)]
        public float contactRayLength = 0.25f;

        [Range(1, 16)]
        public int contactSampleCount = 1;

        [Range(0f, 0.00003f)]
        public float contactBias = 0.000005f;

        [Header("Volume Light")]
        public bool volumeLight = false;

        [HideInInspector]
        [SerializeField]
        public Texture2D bakedShadowMap;

        Matrix4x4 matrixVP;
        Matrix4x4 matrixVP_gpu;

        int index;
        int shadowMapIndex = 0;

        void OnEnable()
        {
            if (mainLight == null)
                mainLight = this;

            lights.Add(this);
            lightListIsDirty = true;

            if (enabled && !passAdded && (int)shadowMapType > 0 && shadowMapBias > 0)
            {
                PixRenderEvent.AddEvent(PixRenderEventName.BeforeAll, ShadowMapPass);
                passAdded = true;
            }
        }

        void OnDisable()
        {
            lights.Remove(this);
            lightListIsDirty = true;

            if (passAdded)
            {
                PixRenderEvent.RemoveEvent(PixRenderEventName.BeforeAll, ShadowMapPass);
                passAdded = false;
            }
        }

        [SerializeField]
        bool passAdded = false;
        void OnValidate()
        {
            // 当满足这三个条件时才开启ShadowMapPass
            if (enabled && !passAdded && shadowMapType != ShadowMapType.None && shadowMapBias > 0)
            {
                PixRenderEvent.AddEvent(PixRenderEventName.BeforeAll, ShadowMapPass);
                passAdded = true;
            }
            else if (!enabled || shadowMapType == ShadowMapType.None || shadowMapBias == 0)
            {
                PixRenderEvent.RemoveEvent(PixRenderEventName.BeforeAll, ShadowMapPass);
                passAdded = false;
            }
        }

    #region shadowmap pass
        static string shadowMapPassName = "PixShadowMap";
        static readonly int[] PixShadowMapIDs = {
            Shader.PropertyToID("_PixShadowMap_0"),
            Shader.PropertyToID("_PixShadowMap_1"),
            Shader.PropertyToID("_PixShadowMap_2"),
            Shader.PropertyToID("_PixShadowMap_3")
        };

        static Color black = new Color(0, 0, 0, 0);
        static readonly int _LightIndex = Shader.PropertyToID("_LightIndex");

        void ShadowMapPass(PixRenderer renderer)
        {
            if (shadowMapIndex < 0) return;
            // 最多只画4个shadowMap, 画多一次肉痛一次
            if (shadowMapIndex > 3) return;

            PixRenderEvent.AddEvent(PixRenderEventName.BeforeDeferred, SetGlobalShadowMap);
            PixRenderEvent.AddEvent(PixRenderEventName.AfterTransparent, CleanUp);

            renderer.cmb.name = shadowMapPassName;
            int shadowMapName = PixShadowMapIDs[shadowMapIndex];

            renderer.cmb.GetTemporaryRT(shadowMapName, shadowMapSize, shadowMapSize, 32, FilterMode.Point, RenderTextureFormat.Depth);
            renderer.cmb.SetRenderTarget(shadowMapName);
            renderer.cmb.ClearRenderTarget(true, true, black);
            renderer.cmb.SetGlobalInt(_LightIndex, index);

            if (FrustumCulling(renderer, out CullingResults cullingResults))
            {
                RendererList rendererList = GetRenderList(renderer, cullingResults);

                if (rendererList.isValid)
                    renderer.cmb.DrawRendererList(rendererList);
            }

            renderer.context.ExecuteCommandBuffer(renderer.cmb);
            renderer.cmb.Clear();
        }

        void SetGlobalShadowMap(PixRenderer renderer)
        {
            PixRenderEvent.RemoveEvent(PixRenderEventName.BeforeDeferred, SetGlobalShadowMap);

            int nameID = PixShadowMapIDs[shadowMapIndex];
            var rtID = new RenderTargetIdentifier(nameID);
            renderer.cmb.SetGlobalTexture(nameID, rtID, RenderTextureSubElement.Depth);
        }

        void CleanUp(PixRenderer renderer)
        {
            PixRenderEvent.RemoveEvent(PixRenderEventName.AfterTransparent, CleanUp);

            renderer.cmb.ReleaseTemporaryRT(PixShadowMapIDs[shadowMapIndex]);
        }

        // TODO: 这cullingResult没把CastShadows为off的renderer剔除掉，要不还是自己写，不用这撒币cullingResult算了
        // TODO: 考虑要不要再和camera的frustum交集一下
        bool FrustumCulling(PixRenderer renderer, out CullingResults cullingResult)
        {
            if (renderer.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParams))
            {
                bool isOrthographic = lightType == LightType.Directional;
                cullingParams.isOrthographic = isOrthographic;
                cullingParams.cullingMatrix = matrixVP;
                cullingParams.cullingOptions = CullingOptions.ShadowCasters;

                var frustum = GeometryUtility.CalculateFrustumPlanes(matrixVP);
                cullingParams.cullingPlaneCount = frustum.Length;
                for (int i = 0; i < frustum.Length; i++)
                {
                    cullingParams.SetCullingPlane(i, frustum[i]);
                }

                cullingResult = renderer.context.Cull(ref cullingParams);
                return true;
            }

            cullingResult = new CullingResults();
            return false;
        }

        RendererList GetRenderList(PixRenderer renderer, CullingResults cullingResults)
        { 
            ShaderTagId tag = new ShaderTagId("PixShadowCaster");
            RendererListDesc rendererListDesc = new(tag, cullingResults, renderer.camera)
            {
                renderQueueRange = RenderQueueRange.opaque,
                sortingCriteria = SortingCriteria.CommonOpaque
            };

            return renderer.context.CreateRendererList(rendererListDesc);
        }
    #endregion
    
        void Update()
        {
            UpdateBoxArea();
        }

        void LateUpdate()
        {
            // mainLight负责提交数据给GPU
            if (mainLight != this) return;

            // 如果lightList有变动则刷新
            if (lightListIsDirty) RefreshMainLight();

            lightCount = Mathf.Min(lights.Count, MAX_LIGHT_COUNT);

            int index = 0;
            for (int i = 0; i < lightCount; i++)
            {
                var light = lights[i];
                float bias = light.shadowMapBias;
                if (light.shadowMapType == ShadowMapType.None) bias = 0;

                bool requestShadowMap = bias > 0;

                if (requestShadowMap)
                {
                    lights[i].shadowMapIndex = index;
                    index++;
                }
                else
                    lights[i].shadowMapIndex = -1;

                lights[i].RefreshProperty(i, bias);
            }

            Shader.SetGlobalInt(_PixLightCount, lightCount);
            Shader.SetGlobalVectorArray(_PixLightsShadowMapSize, shadowmapSizePropList);
            Shader.SetGlobalVectorArray(_PixLightsPosition, positionPropList);
            Shader.SetGlobalVectorArray(_PixLightsDirection, dirctionPropList);
            Shader.SetGlobalVectorArray(_PixLightsColor, colorPropList);
            Shader.SetGlobalVectorArray(_PixLightsContactShadow, contactShadowPropList);
            Shader.SetGlobalVectorArray(_PixLightsShadowMap, shadowMapPropList);
            Shader.SetGlobalMatrixArray(_PixLights_VP, shadowMapMatrixVP);
        }

        // 将自己的参数填充到全屏数组中
        bool RefreshProperty(int i, float bias)
        {
            index = i;

            shadowmapSizePropList[i] = new Vector4(shadowMapSize, 1.0f / shadowMapSize, 0, 0);

            Vector3 pos = transform.position;
            Vector4 posProp = new Vector4(pos.x, pos.y, pos.z, (int)lightType);
            positionPropList[i] = posProp;

            Vector3 dir = -transform.forward;
            Vector4 dirProp = new Vector4(dir.x, dir.y, dir.z, shadowMapIndex);
            dirctionPropList[i] = dirProp;

            Color col = color * intensity;
            colorPropList[i] = new Vector3(col.r, col.g, col.b);

            Vector4 contactShadowParam = Vector4.zero;
            float contactShadow = enableContactShadow ? contactRayLength : 0;
            contactShadow /= contactSampleCount;
            contactShadowParam.x = contactShadow;
            contactShadowParam.y = contactSampleCount;
            contactShadowParam.z = contactBias;
            contactShadowPropList[i] = contactShadowParam;

            Vector4 shadowMapParam = Vector4.zero;
            shadowMapParam.x = bias;
            shadowMapParam.y = (int)shadowMapType;
            shadowMapParam.z = (int)shadowMapQuality;
            shadowMapPropList[i] = shadowMapParam;

            shadowMapMatrixVP[i] = matrixVP_gpu;

            return bias > 0;
        }

        void RefreshMainLight()
        {
            if (lights.Count > 0)
                mainLight = lights[0];
            else
                mainLight = null;

            lightListIsDirty = false;
        }

        void UpdateBoxArea()
        {
            if (shadowMapArea != ShadowMapArea.Box_Area) return;
            if (boxArea == null) return;

            // TODO: 只在发生改变时更新
            CalculateLightVPMatrix();
        }

        void OnDestroy()
        {
            if(bakedShadowMap != null)
                DestroyImmediate(bakedShadowMap);
        }

    #region VPMatrix
        // TODO: 用collider和camera.frustum的交集来做边界盒
        void CalculateLightVPMatrix()
        {
            // 获取BoxCollider的8个角点
            Vector3[] corners = GetBoxColliderCorners(boxArea);
            
            // 将角点转换到世界坐标系
            for (int i = 0; i < corners.Length; i++)
                corners[i] = boxArea.transform.TransformPoint(corners[i]);

            if (lightType == LightType.Directional)
                CalculateDirectionalLightVPMatrix(corners);
            else if (lightType == LightType.Spot)
                CalculateSpotLightVPMatrix(corners);
        }

        Vector3[] GetBoxColliderCorners(BoxCollider boxCollider)
        {
            Vector3 center = boxCollider.center;
            Vector3 size = boxCollider.size;
            Vector3 extents = size * 0.5f;

            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
            corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
            corners[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
            corners[3] = center + new Vector3(extents.x, extents.y, -extents.z);
            corners[4] = center + new Vector3(-extents.x, -extents.y, extents.z);
            corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
            corners[6] = center + new Vector3(-extents.x, extents.y, extents.z);
            corners[7] = center + new Vector3(extents.x, extents.y, extents.z);

            return corners;
        }

        void CalculateDirectionalLightVPMatrix(Vector3[] corners)
        {
            // 找到包围盒的中心点和范围
            Bounds bounds = new(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(corners[i]);

            // 计算视图矩阵
            Vector3 lightPosition = bounds.center;

            Vector3 up = transform.up;
            Vector3 forward = transform.forward;

            Matrix4x4 viewMatrix = Matrix4x4.LookAt(lightPosition, lightPosition - forward, -up);

            // 将角点转换到光源空间
            Vector3[] lightSpaceCorners = new Vector3[corners.Length];
            for (int i = 0; i < corners.Length; i++)
                lightSpaceCorners[i] = viewMatrix.MultiplyPoint3x4(corners[i]);

            // 计算投影矩阵的边界
            float minX = Mathf.Infinity, minY = Mathf.Infinity, minZ = Mathf.Infinity;
            float maxX = Mathf.NegativeInfinity, maxY = Mathf.NegativeInfinity, maxZ = Mathf.NegativeInfinity;

            foreach (Vector3 corner in lightSpaceCorners)
            {
                minX = Mathf.Min(minX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                minZ = Mathf.Min(minZ, corner.z);
                maxX = Mathf.Max(maxX, corner.x);
                maxY = Mathf.Max(maxY, corner.y);
                maxZ = Mathf.Max(maxZ, corner.z);
            }

            // 创建正交投影矩阵
            Matrix4x4 projectionMatrix = Matrix4x4.Ortho(minX, maxX, minY, maxY, -maxZ, -minZ);

            matrixVP = projectionMatrix * viewMatrix;
            projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);
            matrixVP_gpu = projectionMatrix * viewMatrix; 
        }

        // TODO: SpotLight Shadow
        void CalculateSpotLightVPMatrix(Vector3[] corners)
        {
            // 计算视图矩阵（从光源位置看向包围盒中心）
            Bounds bounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(corners[i]);

            Vector3 lightPosition = transform.position;
            Vector3 lookTarget = bounds.center;
            Matrix4x4 viewMatrix = Matrix4x4.LookAt(lightPosition, lookTarget, Vector3.up);

            // 将角点转换到光源空间
            Vector3[] lightSpaceCorners = new Vector3[corners.Length];
            for (int i = 0; i < corners.Length; i++)
                lightSpaceCorners[i] = viewMatrix.MultiplyPoint3x4(corners[i]);

            // 计算近平面和远平面距离
            float minZ = Mathf.Infinity;
            float maxZ = Mathf.NegativeInfinity;
            
            foreach (Vector3 corner in lightSpaceCorners)
            {
                minZ = Mathf.Min(minZ, corner.z);
                maxZ = Mathf.Max(maxZ, corner.z);
            }

            // 确保近平面为正值且不超过远平面
            minZ = Mathf.Max(0.1f, minZ);
            maxZ = Mathf.Max(minZ + 0.1f, maxZ);

            // 根据聚光灯角度和距离计算投影范围
            float fov = spotAngle;
            float aspect = 1.0f; // 通常使用1:1的宽高比
            
            // 创建透视投影矩阵
            Matrix4x4 projectionMatrix = Matrix4x4.Perspective(fov, aspect, minZ, maxZ);
            
            matrixVP = projectionMatrix * viewMatrix; 
            // projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);
            // matrixVP_gpu = projectionMatrix * viewMatrix; 
        }
        #endregion

#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawLine(new Vector3(0.05f, 0, -0.1f), new Vector3(0.05f, 0, 0.08f));
            Gizmos.DrawLine(new Vector3(-0.05f, 0, -0.1f), new Vector3(-0.05f, 0, 0.08f));
            Gizmos.DrawLine(new Vector3(0, 0, -0.1f), new Vector3(0, 0, 0.15f));
            Gizmos.DrawLine(new Vector3(0, 0.05f, -0.1f), new Vector3(0, 0.05f, 0.08f));
            Gizmos.DrawLine(new Vector3(0, -0.05f, -0.1f), new Vector3(0, -0.05f, 0.08f));
        }

        // TODO: fullfil
        public void BakeShadowMap()
        {

        }
    #endif

    }
}
