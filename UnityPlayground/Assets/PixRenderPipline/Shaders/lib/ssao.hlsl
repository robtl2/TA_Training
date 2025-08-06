#ifndef SSAO_INCLUDED
#define SSAO_INCLUDED

#include "sampler.hlsl"
#include "light.hlsl"

#define SSAO_SAMPLE_BIAS 0.00001

//x:intensity y:radius z:stepCount w:jitter
half4 _SSAO_Props;

half calculateSSAO(half2 uv, GBufferData gbufferData)
{
#if defined SSAO_QUALITY_OFF
    return 1.0h;
#endif

    half intensity = _SSAO_Props.x;
    half radius = _SSAO_Props.y;
    int stepCount = (int)_SSAO_Props.z;
    half jitter = _SSAO_Props.w;

    #ifdef SSAO_QUALITY_POOR
        // TAA太畜牲了
        half ao = ContactShadow(gbufferData, gbufferData.bentNormal, radius, stepCount, jitter, SSAO_SAMPLE_BIAS);
    #else
        half3x3 tbn = half3x3(gbufferData.tangentWS, gbufferData.bitangentWS, gbufferData.normalWS);

        half ao = 0.0h;
        for(int i = 0; i < SSAO_SAMPLER_COUNT; i++){
            half3 direction = dirSamplers[i];
            direction = mul(direction, tbn);
            ao += ContactShadow(gbufferData, direction, radius, stepCount, jitter, SSAO_SAMPLE_BIAS);
        }
        ao /= SSAO_SAMPLER_COUNT;
    #endif

    if(intensity<0.99)
        ao = lerp(1.0h, ao, intensity);

    return ao;
}

#endif