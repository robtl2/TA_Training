#ifndef SSAO_INCLUDED
#define SSAO_INCLUDED

#include "sampler.hlsl"
#include "light.hlsl"

#define SSAO_SAMPLE_BIAS 0.00001

//x:intensity y:radius z:stepCount w:jitter
half4 _SSAO_Props;
int2 _SSAO_Clip;
half4 _SSAO_Props_2nd;

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
        half ao = ContactShadow(gbufferData, gbufferData.bentNormal, radius, stepCount, jitter, SSAO_SAMPLE_BIAS, false);
    #else
        half3x3 tbn = half3x3(gbufferData.tangentWS, gbufferData.bitangentWS, gbufferData.normalWS);

        half ao = 0.0h;
        for(int i = 0; i < SSAO_SAMPLER_COUNT; i++){
            half3 direction = dirSamplers[i];
            direction = mul(direction, tbn);
            ao += ContactShadow(gbufferData, direction, radius, stepCount, jitter, SSAO_SAMPLE_BIAS, _SSAO_Clip.x>0);
        }
        ao /= SSAO_SAMPLER_COUNT; 
    #endif

    if(intensity<0.99)
        ao = lerp(1.0h, ao, intensity);

    if(_SSAO_Props_2nd.x > 0.01)
    {
        half intensity_2nd = _SSAO_Props_2nd.x;
        half radius_2nd = _SSAO_Props_2nd.y;
        int stepCount_2nd = (int)_SSAO_Props_2nd.z;
        half jitter_2nd = _SSAO_Props_2nd.w;

        half ao_2nd = ContactShadow(gbufferData, gbufferData.bentNormal, radius_2nd, stepCount_2nd, jitter_2nd, SSAO_SAMPLE_BIAS, _SSAO_Clip.y>0);
        if(intensity_2nd<1)
            ao_2nd = lerp(1.0h, ao_2nd, intensity_2nd);
        
        ao *= ao_2nd;
    }

    return ao;
}

#endif