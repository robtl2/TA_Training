#ifndef RANDOM_INCLUDED
#define RANDOM_INCLUDED

#include "mathacc.hlsl"

// [-1 ~ 1]
// Gemini帮忙写的，性价比超棒
half2 hash22(half2 p)
{
    p = frac(p * float2(53.543123f, 62.743513f));
    p += dot(p, p.yx + 19.1919f);
    return frac(float2(p.x * p.y, p.x + p.y) * 95.4321f)*2.0 - 1.0;
}




#endif