#ifndef COMMAN_INCLUDED
#define COMMAN_INCLUDED

half2 posWorldToScreenUV(float3 posWorld){
    float4 posNDC = mul(UNITY_MATRIX_VP, float4(posWorld, 1.0h));
    half2 uv =  posNDC.xy / posNDC.w;
    uv = uv * 0.5h + 0.5h;
    #if UNITY_UV_STARTS_AT_TOP
    uv.y = 1.0h - uv.y;
    #endif
    return uv;
}



#endif
