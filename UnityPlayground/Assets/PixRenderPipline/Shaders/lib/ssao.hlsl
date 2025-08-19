#ifndef SSAO_INCLUDED
#define SSAO_INCLUDED

#include "sampler.hlsl"
#include "light.hlsl"

half4 _SSAO_Props;
half2 _SSAO_Clip;

half calculateSSAO(half2 uv, half depth_src, half3 pos_src)
{
#if defined SSAO_QUALITY_OFF
    return 1.0h;
#endif

    half intensity = _SSAO_Props.x;
    half radius = _SSAO_Props.y;
    half maxDistance = _SSAO_Clip.x;
    half rMaxDistance = rcp(maxDistance);


    float3 cameraPos = _WorldSpaceCameraPos;
    half len = length(cameraPos - pos_src);

    half ao = 0;
    for(int i=0; i<SSAO_SAMPLER_COUNT; i++)
    {
        half2 offset = hash22(uv+i)*_ScreenTexelSize.zw*radius;

        half2 uv_dest = uv+offset;
        half depth_dest = sampleDepthDownSample(uv_dest);

        if(depth_dest < depth_src){
            ao += 1.0;
        }
        else if(maxDistance>0){
            half3 pos_dest = ReconstructWorldPos(uv_dest, depth_dest);
            half dist = length(pos_dest - pos_src);
            dist = min(dist, maxDistance);
            ao += dist*rMaxDistance;
        }
    }

    ao /= SSAO_SAMPLER_COUNT;

    if(intensity<0.99)
        ao = lerp(1.0h, ao, intensity);

    
    if(_SSAO_Props.z > 0.01)
    {
        half intensity_2nd = _SSAO_Props.z;
        half radius_2nd = _SSAO_Props.w;
        half maxDistance_2nd = _SSAO_Clip.y;
        half rMaxDistance_2nd = rcp(maxDistance_2nd);

        half ao_2nd = 0;
        for(int i=0; i<SSAO_SAMPLER_COUNT_2; i++)
        {
            half2 offset = hash22(uv+i*2)*_ScreenTexelSize.zw*radius_2nd;

            half2 uv_dest = uv+offset;
            half depth_dest = sampleDepthDownSample(uv_dest);

            if(depth_dest < depth_src){
                ao_2nd += 1.0;
            }
            else if(maxDistance_2nd>0){
                half3 pos_dest = ReconstructWorldPos(uv_dest, depth_dest);
                half dist = length(pos_dest - pos_src);
                dist = min(dist, maxDistance_2nd);
                ao_2nd += dist*rMaxDistance_2nd;
            }
        }

        ao_2nd /= SSAO_SAMPLER_COUNT_2;
        if(intensity_2nd<0.99)
            ao_2nd = lerp(1.0h, ao_2nd, intensity_2nd);

        ao *= ao_2nd;
    }

    return ao;
}

/*
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
    half3 V = cameraPos - positionWS;
    half len = length(V);
    V = normalize(V); 

    half3 N = V;

    half3 viewUp = half3(0.0, 1.0, 0.0);
    half3 B = mul((half3x3)UNITY_MATRIX_I_V, viewUp);
    half3 T = normalize(cross(V, B));
    B = cross(T, V);

    #ifdef SSAO_QUALITY_POOR
        // TAA太畜牲了
        half ao = ContactShadow(positionWS, N, radius, stepCount, jitter, bias, _SSAO_Clip.x, 0);
    #else
        half3x3 tbn = half3x3(T, B, N);

        half ao = 0.0h;
        for(int i = 0; i < SSAO_SAMPLER_COUNT; i++){
            half3 direction = dirSamplers[i];
            direction = mul(direction, tbn);
            ao += ContactShadow(positionWS, direction, radius, stepCount, jitter, bias, _SSAO_Clip.x, 0);
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

        half ao_2nd = ContactShadow(positionWS, N, radius_2nd, stepCount_2nd, jitter_2nd, 0.00005, _SSAO_Clip.y, 0);
        if(intensity_2nd<1)
            ao_2nd = lerp(1.0h, ao_2nd, intensity_2nd);

        ao *= ao_2nd;
    }

    return ao;
}
*/
#endif