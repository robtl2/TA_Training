#ifndef LIGHT_INCLUDED
#define LIGHT_INCLUDED


#include "common.hlsl"
#include "gbuffer.hlsl"
#include "random.hlsl"
#include "bentnormal.hlsl"


#define MAX_PIX_LIGHT_COUNT 64

#define PIX_LIGHT_COUNT _PixLightCount

int         _PixLightCount;
half4       _PixLightsShadowMapSize[MAX_PIX_LIGHT_COUNT];
half4       _PixLightsPosition[MAX_PIX_LIGHT_COUNT];
half4       _PixLightsDirection[MAX_PIX_LIGHT_COUNT];
half4       _PixLightsColor[MAX_PIX_LIGHT_COUNT];
half4       _PixLightsContactShadow[MAX_PIX_LIGHT_COUNT];
half4       _PixLightsShadowMap[MAX_PIX_LIGHT_COUNT];
float4x4    _PixLights_VP[MAX_PIX_LIGHT_COUNT];
float4x4    _PixLightsEffectArea[MAX_PIX_LIGHT_COUNT];

half4       _ScreenTexelSize;

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
    index = min(index, _PixLightCount-1);
    
    PixLight light;
    light.lightType = (int)_PixLightsPosition[index].w;
    light.shadowMapIndex = (int)_PixLightsDirection[index].w;

    light.VP = _PixLights_VP[index];
    light.position = _PixLightsPosition[index].xyz;
    light.direction = _PixLightsDirection[index].xyz;
    light.color = _PixLightsColor[index].rgb;

    light.halfAngle = _PixLightsShadowMap[index].y;

    light.enabled = _PixLightsColor[index].a != 0;
    light.isPositive = _PixLightsColor[index].a > 0;

    light.contactShadow = _PixLightsContactShadow[index].x;
    light.contactSampleCount = (int)_PixLightsContactShadow[index].y;
    light.contactBias = _PixLightsContactShadow[index].z;
    light.contactShadowJitter = _PixLightsContactShadow[index].w;

    light.shadowMapBias = _PixLightsShadowMap[index].x;
    light.shadowMapJitter = _PixLightsShadowMap[index].w > 0;
    light.shadowMapSize = _PixLightsShadowMapSize[index].xy;
    light.visibilityShadow = _PixLightsShadowMapSize[index].z;
    light.f0 = _PixLightsShadowMapSize[index].w;

    int shadingFilter = _PixLightsShadowMap[index].z;
    bool enableDiffuse = true;
    bool enableSpecular = true;

    if(shadingFilter==0)
        light.enabled = false;
    else if(shadingFilter==1)
        enableSpecular = false;
    else if(shadingFilter==2)
        enableDiffuse = false;

    light.enableDiffuse = enableDiffuse;
    light.enableSpecular = enableSpecular;

    float4x4 effectArea = _PixLightsEffectArea[index];
    light.enableAreaEffect = any(effectArea != 0);
    light.areaFadeRange = effectArea[3].x;

    light.range = effectArea[3].y;

    if(light.enableAreaEffect)
        effectArea[3] = float4(0,0,0,1); 

    light.effectArea = effectArea;

    return light;
}

half VisibilityShadow(PixLight light, MaterialData matData){
    if(light.visibilityShadow == 0)return 1.0h;

    half3 L = light.direction;
    half shadow = selfOcclusion(matData, L, light.visibilityShadow);

    return shadow;
}

half ShadowMap(PixLight light, half3 positionWS, half2 screenUV){
    if(light.shadowMapBias==0.0)return 1.0h;

    float4 clipPos = mul(light.VP, float4(positionWS,1));
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

half ContactShadow(half3 positionWS, half3 direction, half rayLength, int stepCount, half jitterRadius, half bias, half clip, half outOfUV = 1.0){
    int sampleCount = stepCount + 1; 
    half step = rayLength/stepCount; //采样步长
    half3 pos_ori = positionWS; //ray的起点
    half3 pos_src = pos_ori;
    
    //遍历次数不定加[loop]，避免编译器unroll优化时报错
    [loop]
    for(int i = 1; i < sampleCount; i++){
        pos_src += direction * step; //ray的步进

        half2 uv = PosWorldToScreenUV(pos_src);

        if(jitterRadius>0)
            uv += hash22(uv).xy*jitterRadius;
        
        if(any(uv<0 || uv>1))return outOfUV;

        half depth_dest = sampleDepthDownSample(uv);
        half3 pos_dest = ReconstructWorldPos(uv, depth_dest);
        
        half dist = length(pos_ori - pos_dest);
        
        if(clip>0 && dist>clip)
            return 1.0h;

        half4 ndcPos = TransformWorldToHClip(pos_src);
        half depth_src = ndcPos.z/ndcPos.w;
        depth_src += bias;
        
        if(depth_dest > depth_src)
            return 0.0h;
    }
    return 1.0h;
}

half ContactShadow(PixLight light, half3 positionWS){
    if(light.contactShadow == 0) return 1.0h;

    half3 direction = light.direction;
    if(light.lightType>0)
        direction = normalize(light.position - positionWS);

    half rayLength = light.contactShadow;
    int stepCount = light.contactSampleCount;
    half jitterRadius = light.contactShadowJitter*light.shadowMapSize.y;
    half bias = light.contactBias;

    return ContactShadow(positionWS, direction, rayLength, stepCount, jitterRadius, bias, rayLength*0.5);
}

#endif