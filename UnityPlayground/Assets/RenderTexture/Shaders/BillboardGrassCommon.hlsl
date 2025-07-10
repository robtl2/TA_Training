#ifndef BILLBOARD_GRASS_COMMON_INCLUDED
#define BILLBOARD_GRASS_COMMON_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct Attributes
{
    float4 positionOS   : POSITION;
    float2 uv          : TEXCOORD0;
    float3 normalOS    : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct Varyings
{
    float2 uv          : TEXCOORD0;
    float4 positionCS  : SV_POSITION;
    float2 interaction : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_WindNoise);
SAMPLER(sampler_WindNoise);
TEXTURE2D(_GrassCurrRT);
SAMPLER(sampler_GrassCurrRT);

float4 _GrassCurrAABB;

float4 _MainTex_ST;
float4 _WindNoise_ST;
float _Thickness;
float _Length;
float _TipScale;
float4 _WindDirection;

float4 _Debug;

float2 hash2_2(float2 val)
{
    float2 h = frac(sin(float2(dot(val, float2(127.1,311.7)), dot(val, float2(269.5,183.3))))*43758.5453);

    return h*2-1;
}

Varyings vert (Attributes v)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float3 pivot = TransformObjectToWorld(v.positionOS.xyz);

    float2 offset = floor(pivot.xz*_Debug.x);
    offset = hash2_2(offset)*_Debug.y;

    pivot.xz += offset;

    float3 nor = TransformObjectToWorldNormal(v.normalOS);

    float2 interactionUV = pivot.xz - _GrassCurrAABB.xy;
    float2 size = _GrassCurrAABB.zw - _GrassCurrAABB.xy;
    interactionUV /= size;
    
    float2 interaction = SAMPLE_TEXTURE2D_LOD(_GrassCurrRT, sampler_GrassCurrRT, interactionUV, 0).rg;
    interaction = interaction*2-1;

    float2 windDir = normalize(_WindDirection.xy);
    float windStrength = _WindDirection.z;
    float windSpeed = _WindDirection.w;
    float2 windOffset = frac(-windDir * windSpeed * _Time.y);
    float windNoise = SAMPLE_TEXTURE2D_LOD(_WindNoise, sampler_WindNoise, pivot.xz*_WindNoise_ST.xy + _WindNoise_ST.zw +windOffset,2).r;
    float3 wind = float3(windDir.x,0,windDir.y) * windNoise * windStrength;
    nor += wind;
    nor.xz += interaction;
    nor = normalize(nor);


    float2 uv = v.uv;
    float3 viewDir = normalize(GetCameraPositionWS() - pivot);
    float3 side = normalize(cross(viewDir, nor));
    side *= lerp(1, _TipScale, uv.y);
    
    float3 pos = pivot + nor * _Length * uv.y;
    pos += side * _Thickness * (uv.x-0.5);
    
    o.positionCS = TransformWorldToHClip(pos);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.interaction = interaction;
    return o;
}
#endif 