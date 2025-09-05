#ifndef SSAO_INCLUDED
#define SSAO_INCLUDED

#include "sampler.hlsl"
#include "light.hlsl"

#define SSAO_BIASE 0.00007

half4 _SSAO_Props;
half2 _SSAO_Clip;

half calculateSSAO(half2 uv, half depth_src, half3 pos_src, half3 normalWS)
{
#if defined SSAO_QUALITY_OFF
    return 1.0h;
#endif

    half intensity = _SSAO_Props.x;
    half radius = _SSAO_Props.y;
    half maxDistance = _SSAO_Clip.x;
    half rMaxDistance = rcp(maxDistance);

    half clip = 0.3h;

    depth_src += SSAO_BIASE;

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
            half3 dir = pos_dest - pos_src;
            half dist = length(pos_dest - pos_src);
            dir /= dist;

            half NoD = dot(normalWS, dir);
            dist = NoD>clip?dist:maxDistance;

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
                half3 dir = pos_dest - pos_src;
                half dist = length(dir);
                dir/=dist;

                half NoD = dot(normalWS, dir);
                dist = NoD>clip?dist:maxDistance_2nd;

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

#endif