using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PixRenderPipline
{

    [ExecuteInEditMode]
    public class PixLight : MonoBehaviour
    {
        #region static 
        public const int MAX_LIGHT_COUNT = 64;

        public static PixLight mainLight;
        public static List<PixLight> lights = new();

        static int lightCount = 0;
        static Vector4[] shadowmapSizePropList = new Vector4[MAX_LIGHT_COUNT];
        static Vector4[] positionPropList = new Vector4[MAX_LIGHT_COUNT];
        static Vector4[] dirctionPropList = new Vector4[MAX_LIGHT_COUNT];
        static Vector4[] colorPropList = new Vector4[MAX_LIGHT_COUNT];
        static Vector4[] contactShadowPropList = new Vector4[MAX_LIGHT_COUNT];
        static Vector4[] shadowMapPropList = new Vector4[MAX_LIGHT_COUNT];
        static Matrix4x4[] shadowMapMatrixVP = new Matrix4x4[MAX_LIGHT_COUNT];
        static Matrix4x4[] effectAreaProps = new Matrix4x4[MAX_LIGHT_COUNT];
        static readonly int _PixLightCount = Shader.PropertyToID("_PixLightCount");
        static readonly int _PixLightsShadowMapSize = Shader.PropertyToID("_PixLightsShadowMapSize");
        static readonly int _PixLightsPosition = Shader.PropertyToID("_PixLightsPosition");
        static readonly int _PixLightsDirection = Shader.PropertyToID("_PixLightsDirection");
        static readonly int _PixLightsColor = Shader.PropertyToID("_PixLightsColor");
        static readonly int _PixLightsContactShadow = Shader.PropertyToID("_PixLightsContactShadow");
        static readonly int _PixLightsShadowMap = Shader.PropertyToID("_PixLightsShadowMap");
        static readonly int _PixLights_VP = Shader.PropertyToID("_PixLights_VP");
        static readonly int _PixLightsEffectArea = Shader.PropertyToID("_PixLightsEffectArea");

        public enum LightType
        {
            Directional,
            Spot,
            Point
        }

        public enum DirectionalShadowMapArea
        {
            Camera,
            Box_Area,
            Camera_I_BoxArea,//TODO
        }

        public enum SampleQuality
        {
            Low,
            Medium,
            High,
        }
        #endregion

        #region properties 
        [Header("Main")]
        public LightType lightType = LightType.Directional;
        public int priority;

        public Color color = Color.white;
        public float intensity = 10;
        public float spotAngle;
        public float lightRange;

        [Header("ShadingFilter")]
        public bool enableDiffuse = true;
        public bool enableSpecular = true;

        // 灯光可以自由设置f0的强度，做逆光灯时很方便
        [Range(0, 2)]
        public float f0 = 1.0f;

        [Range(1,5)]
        public float f90 = 1.0f;

        public BoxCollider effectArea;
        public float areaFadeRange = 0.0f;

        [Header("ShadowMap")]
        public bool enableShadowMap = false;
        public int shadowMapSize = 512;
        int getShadowMapSize
        {
            get
            {
                if (shadowMapSize < 64) return 64;
                return shadowMapSize;
            }
        }
        public bool shadowMapJitter = false;

        [Range(0.00001f, 0.03f)]
        public float shadowMapBias = 0.0001f;
        public DirectionalShadowMapArea shadowMapArea = DirectionalShadowMapArea.Box_Area;
        public BoxCollider boxArea;

        [Header("Contact Shadow")]
        public bool enableContactShadow = false;
        public bool contactShadowJitter = false;
        [Range(0,3)]
        public float contactShadowJitterRadius = 0.5f;

        [Range(0f, 1f)]
        public float contactRayLength = 0.25f;

        [Range(1, 16)]
        public int contactSampleCount = 1;

        [Range(0f, 0.0001f)]
        public float contactBias = 0.000005f;

        [Header("VisbilityShadow")]
        public bool enableVisbilityShadow = false;

        [Range(0.001f,1.0f)]
        public float visibilityShadowSoftness = 0.2f;

        [Header("Volume Light")]
        public bool volumeLight = false;

        #endregion


        [HideInInspector]
        [SerializeField]
        public Texture2D bakedShadowMap;

        Matrix4x4 matrixVP;
        Matrix4x4 matrixVP_gpu;
        Plane[] frustum;

        public int index { get; private set; }
        public int shadowMapIndex { get; private set; }
        float halfAngle;
        bool passAdded = false;

        void OnEnable()
        {
            if (mainLight == null)
                mainLight = this;

            lights.Add(this);

            if (isActiveAndEnabled && !passAdded && enableShadowMap && shadowMapBias > 0)
            {
                PixRenderEvent.AddEvent(PixRenderEventName.BeforeAll, ShadowMapPass);
                passAdded = true;
            }
        }

        void OnDisable()
        {
            lights.Remove(this);

            // Debug.Log(lights.Count);

            if (this == mainLight)
            {
                mainLight = null;

                if (lights.Count > 0)
                    mainLight = lights[0];
                else
                    UpLoadParameters();
            }

            if (passAdded)
            {
                PixRenderEvent.RemoveEvent(PixRenderEventName.BeforeAll, ShadowMapPass);
                passAdded = false;
            }
        }

        void OnValidate()
        {
            if (isActiveAndEnabled && !passAdded && enableShadowMap && shadowMapBias > 0 && lightType != LightType.Point)
            {
                PixRenderEvent.AddEvent(PixRenderEventName.BeforeAll, ShadowMapPass);
                passAdded = true;
            }
            else if (!isActiveAndEnabled || !enableShadowMap || shadowMapBias == 0 || lightType == LightType.Point)
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

        static readonly int _LightIndex = Shader.PropertyToID("_LightIndex");

        void ShadowMapPass(PixRenderer renderer)
        {
            if (shadowMapIndex < 0) return;
            // 最多只画4个shadowMap, 画多一次肉痛一次
            if (shadowMapIndex > 3) return;

            PixRenderEvent.TriggerEvent(PixRenderEventName.BeforeShadowMap, renderer);

            PixRenderEvent.AddEvent(PixRenderEventName.BeforeDeferred, SetGlobalShadowMap);
            PixRenderEvent.AddEvent(PixRenderEventName.AfterTransparent, CleanUp);

            renderer.cmb.name = shadowMapPassName;
            int shadowMapName = PixShadowMapIDs[shadowMapIndex];

            renderer.cmb.GetTemporaryRT(shadowMapName, getShadowMapSize, getShadowMapSize, 32, FilterMode.Point, RenderTextureFormat.Depth);
            renderer.cmb.SetRenderTarget(shadowMapName);
            renderer.cmb.ClearRenderTarget(true, true, Color.clear);
            renderer.cmb.SetGlobalInt(_LightIndex, index);

            if (FrustumCulling(renderer, out CullingResults cullingResults))
            {
                RendererList rendererList = GetRenderList(renderer, cullingResults);

                if (rendererList.isValid)
                    renderer.cmb.DrawRendererList(rendererList);
            }

            if (PixGPUSkin.gpuSkins.Count > 0)
                foreach (var gpuskin in PixGPUSkin.gpuSkins) gpuskin.ExecuteGPUskinPass(renderer);

            PixInstance.DrawPass(renderer, 4, frustum);

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
            RendererListDesc rendererListDesc = new(PixPassBase.shadowCasterTag, cullingResults, renderer.camera)
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
            frustum = GeometryUtility.CalculateFrustumPlanes(matrixVP);
        }

        void UpLoadParameters()
        { 
           lightCount = Mathf.Min(lights.Count, MAX_LIGHT_COUNT);

           lights.Sort((l1, l2) => l1.priority.CompareTo(l2.priority));

            int index = 0;
            for (int i = 0; i < lightCount; i++)
            {
                if (i >= MAX_LIGHT_COUNT) break;

                var light = lights[i];
                float bias = light.shadowMapBias;
                if (!light.enableShadowMap || light.lightType==LightType.Point) bias = 0;

                bool requestShadowMap = bias > 0;

                if (requestShadowMap)
                {
                    light.shadowMapIndex = index;
                    index++;
                }
                else
                    light.shadowMapIndex = -1;


                light.RefreshProperty(i, bias);
            }

            Shader.SetGlobalInt(_PixLightCount, lightCount);
            Shader.SetGlobalVectorArray(_PixLightsShadowMapSize, shadowmapSizePropList);
            Shader.SetGlobalVectorArray(_PixLightsPosition, positionPropList);
            Shader.SetGlobalVectorArray(_PixLightsDirection, dirctionPropList);
            Shader.SetGlobalVectorArray(_PixLightsColor, colorPropList);
            Shader.SetGlobalVectorArray(_PixLightsContactShadow, contactShadowPropList);
            Shader.SetGlobalVectorArray(_PixLightsShadowMap, shadowMapPropList);
            Shader.SetGlobalMatrixArray(_PixLights_VP, shadowMapMatrixVP);
            Shader.SetGlobalMatrixArray(_PixLightsEffectArea, effectAreaProps); 
        }

        void LateUpdate()
        {
            // mainLight负责提交数据给GPU
            if (mainLight != this) return;

            UpLoadParameters();
        }

        // 将自己的参数填充到全屏数组中
        bool RefreshProperty(int i, float bias)
        {
            index = i;

            float visibilityShadow = visibilityShadowSoftness;
            if (!enableVisbilityShadow) visibilityShadow = 0;
            shadowmapSizePropList[i] = new Vector4(getShadowMapSize, 1.0f / getShadowMapSize, visibilityShadow, f0);

            Vector3 pos = transform.position;
            Vector4 posProp = new Vector4(pos.x, pos.y, pos.z, (int)lightType);
            positionPropList[i] = posProp;

            Vector3 dir = transform.forward;
            Vector4 dirProp = new Vector4(dir.x, dir.y, dir.z, shadowMapIndex);
            dirctionPropList[i] = dirProp;
            Color _color = color * Mathf.Abs(intensity);
            colorPropList[i] = new Vector4(_color.r, _color.g, _color.b, intensity);

            Vector4 contactShadowParam = Vector4.zero;
            float contactShadow = enableContactShadow ? contactRayLength: 0;
            // contactShadow /= contactSampleCount;
            contactShadowParam.x = contactShadow;
            contactShadowParam.y = contactSampleCount;
            contactShadowParam.z = contactBias;
            contactShadowParam.w = contactShadowJitter ? contactShadowJitterRadius : 0;
            contactShadowPropList[i] = contactShadowParam;

            Vector4 shadowMapParam = Vector4.zero;
            shadowMapParam.x = bias;

            halfAngle = spotAngle * 0.5f * Mathf.Deg2Rad;
            shadowMapParam.y = Mathf.Cos(halfAngle);

            int shadingFilter = 0;
            if (enableDiffuse && !enableSpecular) shadingFilter = 1;
            if (!enableDiffuse && enableSpecular) shadingFilter = 2;
            if (enableDiffuse && enableSpecular) shadingFilter = 3;

            shadowMapParam.z = shadingFilter;
            shadowMapParam.w = shadowMapJitter ? 1 : 0;
            shadowMapPropList[i] = shadowMapParam;

            shadowMapMatrixVP[i] = matrixVP_gpu;

            Matrix4x4 effectAreaProp = Matrix4x4.zero;
            if (effectArea != null)
            {
                effectAreaProp = effectArea.transform.worldToLocalMatrix;
                Vector4 axis_x = effectAreaProp.GetRow(0) / (effectArea.size.x * 0.5f);
                Vector4 axis_y = effectAreaProp.GetRow(1) / (effectArea.size.y * 0.5f);
                Vector4 axis_z = effectAreaProp.GetRow(2) / (effectArea.size.z * 0.5f);
                effectAreaProp.SetRow(0, axis_x);
                effectAreaProp.SetRow(1, axis_y);
                effectAreaProp.SetRow(2, axis_z);

                Vector3 center = effectArea.center;
                Vector4 localPos = effectAreaProp.GetColumn(3);
                localPos.x -= center.x;
                localPos.y -= center.y;
                localPos.z -= center.z;
                effectAreaProp.SetColumn(3, localPos);

                effectAreaProp.SetRow(3, new Vector4(areaFadeRange, 0.0f, 0.0f, 1.0f)); // 设置fade范围
            }
            effectAreaProp.m31 = lightRange;
            effectAreaProps[i] = effectAreaProp;

            return bias > 0;
        }

        void UpdateBoxArea()
        {
            if (shadowMapArea != DirectionalShadowMapArea.Box_Area) return;
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
            GetBoxColliderCorners(boxArea);
            
            // 将角点转换到世界坐标系
            for (int i = 0; i < corners.Length; i++)
                corners[i] = boxArea.transform.TransformPoint(corners[i]);

            if (lightType == LightType.Directional)
                CalculateDirectionalLightVPMatrix(corners);
            else if (lightType == LightType.Spot)
                CalculateSpotLightVPMatrix();
        }

        static Vector3[] corners = new Vector3[8];
        void GetBoxColliderCorners(BoxCollider boxCollider)
        {
            Vector3 center = boxCollider.center;
            Vector3 size = boxCollider.size;
            Vector3 extents = size * 0.5f;

            corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
            corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
            corners[2] = center + new Vector3(-extents.x, extents.y, -extents.z);
            corners[3] = center + new Vector3(extents.x, extents.y, -extents.z);
            corners[4] = center + new Vector3(-extents.x, -extents.y, extents.z);
            corners[5] = center + new Vector3(extents.x, -extents.y, extents.z);
            corners[6] = center + new Vector3(-extents.x, extents.y, extents.z);
            corners[7] = center + new Vector3(extents.x, extents.y, extents.z);
        }

        // 计算视图矩阵
        Matrix4x4 CalculateVMatrix()
        {
            Matrix4x4 translation = Matrix4x4.Translate(-transform.position);
            var iRotation = Quaternion.Inverse(transform.rotation);
            Matrix4x4 rotation = Matrix4x4.Rotate(iRotation);
            Matrix4x4 viewMatrix = rotation * translation;
            return viewMatrix;
        }

        void CalculateDirectionalLightVPMatrix(Vector3[] corners)
        {
            Matrix4x4 viewMatrix = CalculateVMatrix();

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
        void CalculateSpotLightVPMatrix()
        {
            Matrix4x4 viewMatrix = CalculateVMatrix();

            // 创建透视投影矩阵
            Matrix4x4 projectionMatrix = Matrix4x4.Perspective(spotAngle, 1, 0.1f, lightRange);
            
            matrixVP = projectionMatrix * viewMatrix; 
            projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);
            matrixVP_gpu = projectionMatrix * viewMatrix; 
        }
#endregion

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Handles.color = Gizmos.color;

            switch (lightType)
            {
                case LightType.Directional:
                    DrawDirectionalGizmos();
                    break;
                case LightType.Point:
                    DrawPointGizmos();
                    break;
                case LightType.Spot:
                    DrawSpotGizmos();
                    break;
            }
        }

        void DrawDirectionalGizmos()
        { 
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawLine(new Vector3(0.1f, 0, 0f), new Vector3(0.1f, 0, -0.1f));
            Gizmos.DrawLine(new Vector3(-0.1f, 0, 0f), new Vector3(-0.1f, 0, -0.1f));
            Gizmos.DrawLine(new Vector3(0, 0, -0.1f), new Vector3(0, 0, -0.2f));
            Gizmos.DrawLine(new Vector3(0, 0.1f, 0f), new Vector3(0, 0.1f, -0.1f));
            Gizmos.DrawLine(new Vector3(0, -0.1f, 0f), new Vector3(0, -0.1f, -0.1f));
            
            Handles.DrawWireDisc(transform.position, -transform.forward, 0.1f);
        }

        void DrawPointGizmos()
        {
            var sceneCam = SceneView.lastActiveSceneView?.camera;
            if (sceneCam == null)
                return;

            Vector3 normal = (sceneCam.transform.position - transform.position).normalized;
            Handles.DrawWireDisc(transform.position, normal, 0.1f);
            Handles.DrawWireDisc(transform.position, normal, lightRange);

            Vector3 right = sceneCam.transform.right;
            Vector3 up = sceneCam.transform.up;
            Vector3 upRight = (right + up).normalized;
            Vector3 downRight = (right - up).normalized;
            Gizmos.DrawLine(transform.position + right * 0.11f, transform.position + right * 0.15f);
            Gizmos.DrawLine(transform.position - right * 0.11f, transform.position - right * 0.15f);
            Gizmos.DrawLine(transform.position + up * 0.11f, transform.position + up * 0.15f);
            Gizmos.DrawLine(transform.position - up * 0.11f, transform.position - up * 0.15f);
            Gizmos.DrawLine(transform.position + upRight * 0.11f, transform.position + upRight * 0.15f);
            Gizmos.DrawLine(transform.position - upRight * 0.11f, transform.position - upRight * 0.15f);
            Gizmos.DrawLine(transform.position + downRight * 0.11f, transform.position + downRight * 0.15f);
            Gizmos.DrawLine(transform.position - downRight * 0.11f, transform.position - downRight * 0.15f);
        }

        void DrawSpotGizmos()
        {
            Vector3 forward = -transform.forward;
            Vector3 up = transform.up;
            Vector3 right = transform.right;
            float radius = Mathf.Tan(halfAngle) * lightRange;
            Vector3 end = transform.position + forward * lightRange;
            Handles.DrawWireDisc(end, forward, radius);
            Handles.DrawLine(transform.position, end + up * radius);
            Handles.DrawLine(transform.position, end - up * radius);
            Handles.DrawLine(transform.position, end + right * radius);
            Handles.DrawLine(transform.position, end - right * radius);
        }

        // TODO: fullfil
        public void BakeShadowMap()
        {

        }
#endif

    }
}