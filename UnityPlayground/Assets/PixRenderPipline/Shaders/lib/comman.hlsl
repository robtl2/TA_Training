#ifndef COMMAN_INCLUDED
#define COMMAN_INCLUDED

#include "lib/mathacc.hlsl"

#define MATH_ACC 1

half2 PosWorldToScreenUV(float3 posWorld){
    float4 posNDC = mul(UNITY_MATRIX_VP, float4(posWorld, 1.0h));
    half2 uv =  posNDC.xy / posNDC.w;
    uv = uv * 0.5h + 0.5h;
    #if UNITY_UV_STARTS_AT_TOP
    uv.y = 1.0h - uv.y;
    #endif
    return uv;
}

half2 DirToThetaPhi(float3 dir)
{
    dir = normalize(dir);

#if MATH_ACC   
    half theta = fast_acos(dir.y);
    half phi = fast_atan2(dir.x, dir.z);
#else
    half theta = acos(dir.y);
    half phi = atan2(dir.x, dir.z);
#endif
    
    half2 uv;
    uv.y = theta / PI;
    uv.x = (phi + PI) / (2.0 * PI);

    uv = 1-uv;
    
    return uv;
}

half3 rotate_y(half3 v, half angle)
{
    half sin_angle;
    half cos_angle;

#if MATH_ACC
    fast_sincos(angle, sin_angle, cos_angle);
#else
    sincos(angle, sin_angle, cos_angle);
#endif

    half3x3 rotationMatrix = half3x3(
        half3(cos_angle, 0, -sin_angle),
        half3(0, 1, 0),
        half3(sin_angle, 0, cos_angle)    
    );

    return mul(rotationMatrix, v);
}



#endif
