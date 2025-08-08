#ifndef FULLSCREEN_INCLUDED
#define FULLSCREEN_INCLUDED

TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);

struct AttrFullScreenQuad
{
    float2 uv : TEXCOORD0;
};

struct VarFullScreenQuad
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

VarFullScreenQuad vertFullScreen(AttrFullScreenQuad input)
{
    VarFullScreenQuad output;
    float2 uv = input.uv;
    float2 pos = uv*2.0-1.0;

    // #if UNITY_UV_STARTS_AT_TOP
    uv.y = 1.0 - uv.y;
    // #endif
    
    output.positionCS = float4(pos, 0.0, 1.0);
    output.uv = uv;
    return output;
}

half4 fragFullScreen(VarFullScreenQuad input) : SV_Target
{
    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    return color;
}

#endif 