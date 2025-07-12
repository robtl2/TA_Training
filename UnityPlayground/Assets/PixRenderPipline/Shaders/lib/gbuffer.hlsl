#ifndef GBUFFER_INCLUDED
#define GBUFFER_INCLUDED

#include "comman.hlsl"

struct GBufferData
{
    half3 albedo;
    half alpha;

    float3 positionWS;
    half3 normalWS;
    // half3 trueNormal;
    half3 normalVS;
    half3 viewDir;

    half NoV;
    half ndcDepth;
    half depth;
};

struct GBuffer
{
    half4 gbuffer_0;
    half4 gbuffer_1;
};


TEXTURE2D(_PixGBuffer_0);SAMPLER(sampler_PixGBuffer_0);
TEXTURE2D(_PixGBuffer_1);TEXTURE2D(_PixEarlyZDepth);
TEXTURE2D(_PixTiledID);SAMPLER(sampler_PixTiledID);

float2 _PixGBuffer_0_TexelSize;


half3 UnpackNormal(half2 nor)
{
    half2 xy = nor*2-1;
    half z = sqrt(1.0 - dot(xy, xy));
    z = max(z, 0.01);
    return half3(xy, z);
}

float3 ReconstructWorldPos(float2 uv, float ndcDepth) 
{
    float3 ndc = float3(uv*2.0 - 1.0, ndcDepth); 

    #if UNITY_UV_STARTS_AT_TOP
    ndc.y = -ndc.y;
    #endif

    float4 worldPosH = mul(UNITY_MATRIX_I_VP, float4(ndc, 1.0));
    return worldPosH.xyz / worldPosH.w;
}

half sampleDepth(float2 uv){
    return SAMPLE_TEXTURE2D(_PixEarlyZDepth, sampler_PixGBuffer_0, uv).r;
}

half3 samplePositionWS(float2 uv){
    half depth = sampleDepth(uv);
    return ReconstructWorldPos(uv, depth);
}

// ------------------------------------------------------------
// Reconstruct True Normal
// 通过采样紧邻象素的深度构建出世界坐标来计算几何体的真实法线
// 虽然这些象素肯定都在L1缓存里，但多次重构世界坐标还是有点肉痛
// ------------------------------------------------------------
half3 ReconstructTrueNormal_Tap4(half3 posWorld, half2 uv){
    half3 posWorld_l = samplePositionWS(uv - half2(_PixGBuffer_0_TexelSize.x, 0));
    half3 posWorld_r = samplePositionWS(uv + half2(_PixGBuffer_0_TexelSize.x, 0));
    half3 posWorld_d = samplePositionWS(uv - half2(0, _PixGBuffer_0_TexelSize.y));
    half3 posWorld_u = samplePositionWS(uv + half2(0, _PixGBuffer_0_TexelSize.y));

    half3 l = posWorld - posWorld_l;
    half3 r = posWorld_r - posWorld;
    half3 d = posWorld - posWorld_d;
    half3 u = posWorld_u - posWorld;

    half3 dx = abs(l.z) < abs(r.z) ? l : r;
    half3 dy = abs(d.z) < abs(u.z) ? d : u;

    half3 normal = normalize(cross(dy, dx));

    return normal;
}
// ------------------------------------------------------------

GBuffer PackGBuffer(half4 color, int shadingModel, half2 normalVS){
    half2 rgb = PackToR5G6B5(color.rgb);

    half4 _color = half4(rgb, 0, 1);

    GBuffer gbuffer;
    gbuffer.gbuffer_0 = _color;
    gbuffer.gbuffer_1 = half4(normalVS, 0, 0);
    return gbuffer;
}

GBufferData UnpackGBuffer(float2 uv)
{
    half4 gbuffer_0 = SAMPLE_TEXTURE2D(_PixGBuffer_0, sampler_PixGBuffer_0, uv);
    half4 gbuffer_1 = SAMPLE_TEXTURE2D(_PixGBuffer_1, sampler_PixGBuffer_0, uv);
    float ndcDepth = SAMPLE_TEXTURE2D(_PixEarlyZDepth, sampler_PixGBuffer_0, uv).r;

    half3 _color = UnpackFromR5G6B5(gbuffer_0.rg);
    half3 normalVS = UnpackNormal(gbuffer_1.xy);
    float3 worldPos = ReconstructWorldPos(uv, ndcDepth);

    half3 cameraPos = _WorldSpaceCameraPos;
    half3 viewDir = cameraPos - worldPos;
    half depth = length(viewDir);
    viewDir /= depth;

    half3 viewUp = half3(0.0, 1.0, 0.0);
    half3 up = mul((half3x3)UNITY_MATRIX_I_V, viewUp);
    half3 right = normalize(cross(viewDir, up));
    up = cross(right, viewDir);
    half3x3 viewToWorld = half3x3(right, up, viewDir);
    half3 normalWS = mul(normalVS, viewToWorld);

    // half3 trueNormal = ReconstructTrueNormal_Tap4(worldPos, uv);


    GBufferData gbufferData;
    gbufferData.albedo = _color;
    gbufferData.alpha = gbuffer_0.a;

    gbufferData.positionWS = worldPos;
    gbufferData.normalWS = normalWS;
    gbufferData.normalVS = normalVS;
    gbufferData.viewDir = viewDir;
    gbufferData.NoV = normalVS.z;
    gbufferData.ndcDepth = ndcDepth;
    gbufferData.depth = depth;
    // gbufferData.trueNormal = trueNormal;
    return gbufferData;
}



#endif