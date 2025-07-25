#ifndef LIGHT_INCLUDED
#define LIGHT_INCLUDED

#include "gbuffer.hlsl"

half3 _PixAmbientLightColor;

half3 _PixMainLightPosition;
half3 _PixMainLightDirection;
half3 _PixMainLightColor;
half _PixMainLightContactShadow;
int _PixMainLightContactSampleCount;
half _PixMainLightContactBias;

struct Light{
    half3 position;
    half3 direction;
    half3 color;
    half contactShadow;
    int contactSampleCount;
    half contactBias;

    half NoL;
    half shadow;
    half3 lit;
};

Light GetMainLight(){
    Light light;
    light.position = _PixMainLightPosition;
    light.direction = _PixMainLightDirection;
    light.color = _PixMainLightColor;
    light.contactShadow = _PixMainLightContactShadow;
    light.contactSampleCount = _PixMainLightContactSampleCount;
    light.contactBias = _PixMainLightContactBias;
    return light;
}

half ContactShadow(Light light, GBufferData gbufferData){
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

// 后面ShadingModel计算的入口
void CauclateLight(inout Light light, GBufferData gbufferData) 
{
    half3 N = gbufferData.normalWS;
    half3 L = light.direction;

    half3 NoL = saturate(dot(N, L));
    light.NoL = NoL;

    NoL = smoothstep(0.15, 0.16, NoL);
    
    half contactShadow = ContactShadow(light, gbufferData);

    light.shadow = contactShadow;
    light.lit = light.color * NoL * contactShadow;
}

#endif