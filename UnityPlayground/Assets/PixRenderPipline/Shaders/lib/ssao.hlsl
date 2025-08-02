#ifndef SSAO_INCLUDED
#define SSAO_INCLUDED

#include "gbuffer.hlsl"
#include "mathacc.hlsl"

half _SSAO_radius;
int _SSAO_sampleCount;
half _SSAO_intensity;


// // 计算SSAO
half calculateSSAO(half2 uv, GBufferData gbufferData)
{
    return 1.0;
}

#endif