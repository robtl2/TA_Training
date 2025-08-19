#ifndef SSAO_INCLUDED
#define SSAO_INCLUDED

#include "sampler.hlsl"
#include "light.hlsl"

//x:intensity y:radius z:stepCount w:jitter
half4 _SSAO_Props;
half3 _SSAO_Clip;
half4 _SSAO_Props_2nd;

half calculateSSAO(half2 uv, half3 positionWS)
{
#if defined SSAO_QUALITY_OFF
    return 1.0h;
#endif

    half intensity = _SSAO_Props.x;
    int stepCount = (int)_SSAO_Props.z;
    half radius = _SSAO_Props.y;
    half jitter = _SSAO_Props.w;
    half bias = _SSAO_Clip.z;

    float3 cameraPos = _WorldSpaceCameraPos;
    half3 V = positionWS - cameraPos;
    half len = length(V);
    // half factor = 1/len;
    // bias *= factor;
    // radius *= factor;

    V = normalize(V); 

    half3 N = V;

    half3 viewUp = half3(0.0, 1.0, 0.0);
    half3 B = mul((half3x3)UNITY_MATRIX_I_V, viewUp);
    half3 T = normalize(cross(V, B));
    B = cross(T, V);

    #ifdef SSAO_QUALITY_POOR
        // TAA太畜牲了
        half ao = ContactShadow(positionWS, N, radius, stepCount, jitter, bias, false);
    #else
        half3x3 tbn = half3x3(T, B, N);

        half ao = 0.0h;
        for(int i = 0; i < SSAO_SAMPLER_COUNT; i++){
            half3 direction = dirSamplers[i];
            direction = mul(direction, tbn);
            ao += ContactShadow(positionWS, direction, radius, stepCount, jitter, bias, _SSAO_Clip.x);
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

        half ao_2nd = ContactShadow(positionWS, N, radius_2nd, stepCount_2nd, jitter_2nd, bias, _SSAO_Clip.y);
        if(intensity_2nd<1)
            ao_2nd = lerp(1.0h, ao_2nd, intensity_2nd);
        
        ao *= ao_2nd;
    }

    return ao;
}

#endif