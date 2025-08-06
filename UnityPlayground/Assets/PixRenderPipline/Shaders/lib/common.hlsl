#ifndef COMMON_INCLUDED
#define COMMON_INCLUDED

#include "struct.hlsl"
#include "mathacc.hlsl"
#include "random.hlsl"

#define MATH_ACC 1

void TestAlpha(half alpha, half cutOff, half2 screenUV){
    #ifdef TAA
        clip(detherAlpha(alpha, cutOff, screenUV));
    #else
        clip(alpha - cutOff);
    #endif
}

void TestAlpha(half alpha, half cuttOff, half4 clipPos){
    half2 screenUV = clipPos.xy/clipPos.w;
    screenUV = screenUV*0.5+0.5;

    TestAlpha(alpha, cuttOff, screenUV);
}

half remap01(half from, half to, half x)
{
    return saturate((x - from) / (to - from));
}

// 将两个int打包成一个8bit的float
// value1: 5bit (0-31) 用来装metallic
// value2: 3bit (0-7) 用来装shadingModel
float PackTwoIntToFloat(int value1, int value2)
{
    // 确保值在有效范围内
    value1 = clamp(value1, 0, 31);
    value2 = clamp(value2, 0, 7);
    
    // 将value1放在高5位，value2放在低3位
    int packedInt = (value1 << 3) | value2;
    
    // 转换为0-255范围的值，然后归一化到0-1范围
    return (float)packedInt / 255.0;
}
 
// 从float中解包出两个int
void UnpackFloatToTwoInt(float packedValue, out int value1, out int value2)
{
    // 从0-1范围恢复到0-255范围
    int packedInt = (int)(packedValue * 255.0 + 0.5); // +0.5用于四舍五入
    
    // 提取高5位作为value1
    value1 = (packedInt >> 3) & 31;
    
    // 提取低3位作为value2
    value2 = packedInt & 7;
}

half3 UnpackNormal(half2 nor)
{
    half2 xy = nor*2-1;
    half z = sqrt(1.0 - dot(xy, xy));
    z = max(z, 0.004);
    return half3(xy, z);
}

half3 HDR2LDR(half3 rgb, half maxVal = 2){
    return saturate(sqrt(rgb)/maxVal);
}

half3 LDR2HDR(half3 ldr, half maxVal = 2){
    half3 rgb = ldr*maxVal;
    rgb = rgb*rgb;
    return rgb;
}

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
