#ifndef LIGHT_INCLUDED
#define LIGHT_INCLUDED


#include "common.hlsl"
#include "gbuffer.hlsl"
#include "random.hlsl"


#define MAX_PIX_LIGHT_COUNT 64
#define PIX_LIGHT_COUNT     _PixLightCount

int         _PixLightCount;
half4       _PixLightsShadowMapSize[MAX_PIX_LIGHT_COUNT];
half4       _PixLightsPosition[MAX_PIX_LIGHT_COUNT];
half4       _PixLightsDirection[MAX_PIX_LIGHT_COUNT];
half3       _PixLightsColor[MAX_PIX_LIGHT_COUNT];
half4       _PixLightsContactShadow[MAX_PIX_LIGHT_COUNT];
half4       _PixLightsShadowMap[MAX_PIX_LIGHT_COUNT];
float4x4    _PixLights_VP[MAX_PIX_LIGHT_COUNT];

// 最多4盏灯可以用shadowMap
TEXTURE2D(_PixShadowMap_0);SAMPLER(sampler_PixShadowMap_0);
TEXTURE2D(_PixShadowMap_1);SAMPLER(sampler_PixShadowMap_1);
TEXTURE2D(_PixShadowMap_2);SAMPLER(sampler_PixShadowMap_2);
TEXTURE2D(_PixShadowMap_3);SAMPLER(sampler_PixShadowMap_3);

half SampleShadowMap(int index, float2 uv)
{
    switch(index)
    {
        case 0:
            return SAMPLE_TEXTURE2D(_PixShadowMap_0,sampler_PixShadowMap_0, uv).r;
        case 1:
            return SAMPLE_TEXTURE2D(_PixShadowMap_1,sampler_PixShadowMap_1, uv).r;
        case 2:
            return SAMPLE_TEXTURE2D(_PixShadowMap_2,sampler_PixShadowMap_2, uv).r;
        case 3:
            return SAMPLE_TEXTURE2D(_PixShadowMap_3,sampler_PixShadowMap_3, uv).r;
        default:
            return 1.0;
    }
}

PixLight GetPixLight(int index)
{
    index = min(index, PIX_LIGHT_COUNT-1);

    PixLight light;
    light.lightType = (int)_PixLightsPosition[index].w;
    light.shadowMapIndex = (int)_PixLightsDirection[index].w;
    light.shadowMapSize = _PixLightsShadowMapSize[index].xy;

    light.VP = _PixLights_VP[index];
    light.position = _PixLightsPosition[index].xyz;
    light.direction = _PixLightsDirection[index].xyz;
    light.color = _PixLightsColor[index];

    light.contactShadow = _PixLightsContactShadow[index].x;
    light.contactSampleCount = (int)_PixLightsContactShadow[index].y;
    light.contactBias = _PixLightsContactShadow[index].z;

    light.shadowMapBias = _PixLightsShadowMap[index].x;
    light.shadowMapType = (int)_PixLightsShadowMap[index].y;
    light.shadowMapQuality = (int)_PixLightsShadowMap[index].z;
    light.shadowMapJitter = _PixLightsShadowMap[index].w > 0;
    
    return light;
}

half ShadowMap(PixLight light, half2 screenUV, GBufferData gbufferData){
    if(light.shadowMapBias==0.0)return 1.0h;

    float4 clipPos = mul(light.VP, float4(gbufferData.positionWS,1));
    float3 ndcPos = clipPos.xyz / clipPos.w;
    float2 uv = ndcPos.xy * 0.5 + 0.5;
    uv.y = 1-uv.y;

    if(any(uv<0 || uv>1))return 1.0h;

    if(light.shadowMapJitter)
        uv += hash22(screenUV).xy*light.shadowMapSize.y;

    float depthSrc = saturate(ndcPos.z + light.shadowMapBias);

    int index = light.shadowMapIndex;
    half depthDest = SampleShadowMap(index, uv);

    return depthSrc>depthDest;
}

// TODO: 改成在灯光的local空间进行对比
half ContactShadow(PixLight light, GBufferData gbufferData){
    if(light.contactShadow == 0) return 1.0h;

    int sampleCount = light.contactSampleCount + 1; 
    half step = light.contactShadow; //采样步长
    half3 pos = gbufferData.positionWS; //ray的起点

    //遍历次数不定加[loop]，避免编译器unroll优化时报错
    [loop]
    for(int i = 1; i < sampleCount; i++){
        pos += light.direction * step; //ray的步进

        half2 uv = PosWorldToScreenUV(pos);
        half depth = sampleDepth(uv);
        half4 ndcPos = TransformWorldToHClip(pos);
        half rayDepth = ndcPos.z/ndcPos.w;
        rayDepth += light.contactBias;
        
        if(depth > rayDepth){
            return 0.0h;
        }
    }
    return 1.0h;
}

#endif