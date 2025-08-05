#ifndef RANDOM_INCLUDED
#define RANDOM_INCLUDED

#include "mathacc.hlsl"

half2       _TAA_Jitter;

// [-1 ~ 1]
// Gemini帮忙写的，性价比超棒
half2 hash22(half2 p)
{
    p += _TAA_Jitter;
    p = frac(p * float2(53.543123f, 62.743513f));
    p += dot(p, p.yx + 19.1919f);
    return frac(float2(p.x * p.y, p.x + p.y) * 95.4321f)*2.0 - 1.0;
}

half hash21(half2 p)
{
    // p += _TAA_Jitter;
    p = frac(p * half2(53.543h, 62.743h));
    p += dot(p, p.yx + 19.191h);
    return frac((p.x * p.y + p.x + p.y) * 95.432h) * 2.0h - 1.0h;
}

half3 hash23(half2 p)
{
    p += _TAA_Jitter.xy;

    p = frac(p * float2(53.543123f, 62.743513f));
    p += dot(p, p.yx + 19.1919f);

    float3 result = frac(float3(p.x * p.y, p.x + p.y, p.x * p.y + p.x) * 95.4321f);
    
    return result * 2.0 - 1.0;
}

half dether(half2 uv, half alpha){
    half r = hash21(uv);
    r = r*0.5+0.5;
    return r < alpha;
}

half3 dether(half2 uv, half3 alpha){
    half3 r = hash23(uv);
    r = r*0.5 + 0.5;
    return r < alpha;
}


#endif