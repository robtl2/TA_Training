using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PixLight : MonoBehaviour
{
    public static PixLight MainDirectionalLight;
    public static List<PixLight> lights = new();

    public enum LightType
    {
        MainDirectional,
        Ambient,
        Directional,
        Point,
        Spot
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
        AABB_Box,
    }

    public LightType lightType = LightType.MainDirectional;

    public Color color = Color.white;
    public float intensity = 10;

    [Header("ShadowMap")]
    public ShadowMapType shadowMapType = ShadowMapType.None;
    public float shadowMapSize = 512;
    public ShadowMapArea shadowMapArea = ShadowMapArea.Camera;
    public Transform AABB_LocaltionTarget;
    public Vector3 AABB_Size;
    
    [Header("Contact Shadow")]
    public bool enableContactShadow = false;

    [Range(0f, 0.25f)]
    public float contactRayLength = 0.25f;

    [Range(1, 16)]
    public int contactSampleCount = 1;

    [Range(0f, 0.00002f)]
    public float contactBias = 0.000005f;

    [Header("Volume Light")]
    public bool volumeLight = false;

    [HideInInspector]
    [SerializeField]
    public Texture2D bakedShadowMap;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    void Start()
    {   
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshFilter.hideFlags = HideFlags.HideInInspector;
        meshRenderer.hideFlags = HideFlags.HideInInspector;
    }

    void OnEnable()
    {
        if (lightType == LightType.MainDirectional)
        {
            MainDirectionalLight = this;
        }
        lights.Add(this);
    }

    void OnDisable()
    {
        if (lightType == LightType.MainDirectional)
        {
            MainDirectionalLight = null;
        }
        lights.Remove(this);
    }

    readonly int _PixMainLightPosition = Shader.PropertyToID("_PixMainLightPosition");
    readonly int _PixMainLightDirection = Shader.PropertyToID("_PixMainLightDirection");
    readonly int _PixMainLightColor = Shader.PropertyToID("_PixMainLightColor");
    readonly int _PixMainLightContactShadow = Shader.PropertyToID("_PixMainLightContactShadow");
    readonly int _PixMainLightContactSampleCount = Shader.PropertyToID("_PixMainLightContactSampleCount");
    readonly int _PixMainLightContactBias = Shader.PropertyToID("_PixMainLightContactBias");
    readonly int _PixAmbientLightColor = Shader.PropertyToID("_PixAmbientLightColor");

    void Update()
    {
        if (lightType != LightType.MainDirectional) return;

        Shader.SetGlobalVector(_PixMainLightPosition, transform.position);
        Shader.SetGlobalVector(_PixMainLightDirection, -transform.forward);
        Shader.SetGlobalColor(_PixMainLightColor, color * intensity);
        float contactShadow = enableContactShadow ? contactRayLength : 0;
        contactShadow /= contactSampleCount;
        Shader.SetGlobalFloat(_PixMainLightContactShadow, contactShadow);
        Shader.SetGlobalInt(_PixMainLightContactSampleCount, contactSampleCount);
        Shader.SetGlobalFloat(_PixMainLightContactBias, contactBias);

        foreach (var light in lights)
        {
            if (light.lightType == LightType.Ambient)
            {
                Shader.SetGlobalColor(_PixAmbientLightColor, light.color * light.intensity);
            }
        }
    }

    void OnDestroy()
    {
        DestroyImmediate(bakedShadowMap);
        DestroyImmediate(meshFilter);
        DestroyImmediate(meshRenderer);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }

    public void BakeShadowMap()
    {

    }
#endif

}