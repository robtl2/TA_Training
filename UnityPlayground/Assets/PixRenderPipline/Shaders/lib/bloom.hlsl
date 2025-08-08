#ifndef BLOOM_INCLUDED
#define BLOOM_INCLUDED

#include "fullscreen.hlsl"
#include "common.hlsl"


struct VarBloom
{
    float2 uv[5] : TEXCOORD0;
    float4 positionCS : SV_POSITION;
};

TEXTURE2D(_BloomTex);SAMPLER(sampler_BloomTex);float2 _BloomTex_TexelSize;

half _BloomRadius;
half _Bloom_Threshold;

VarBloom vertBloomVertical(AttrFullScreenQuad input)
{
    VarBloom output;
    float2 uv = input.uv;
    float2 pos = uv*2.0-1.0;

    #if UNITY_UV_STARTS_AT_TOP
    uv.y = 1.0 - uv.y;
    #endif
    
    output.positionCS = float4(pos, 0.0, 1.0);

    half offset1 = _BloomTex_TexelSize.y * _BloomRadius;
    half offset2 = offset1*2.0;
    output.uv[0] = uv;
    output.uv[1] = uv + float2(0, offset1);
    output.uv[2] = uv - float2(0, offset1);
    output.uv[3] = uv + float2(0, offset2);
    output.uv[4] = uv - float2(0, offset2);

    return output;
}

VarBloom vertBloomHorizontal(AttrFullScreenQuad input)
{
    VarBloom output;
    float2 uv = input.uv;
    float2 pos = uv*2.0-1.0;

    #if UNITY_UV_STARTS_AT_TOP
    uv.y = 1.0 - uv.y;
    #endif
    
    output.positionCS = float4(pos, 0.0, 1.0);

    half offset1 = _BloomTex_TexelSize.x * _BloomRadius;
    half offset2 = offset1*2.0;
    output.uv[0] = uv;
    output.uv[1] = uv + float2(offset1, 0);
    output.uv[2] = uv - float2(offset1, 0);
    output.uv[3] = uv + float2(offset2, 0);
    output.uv[4] = uv - float2(offset2, 0);

    return output;
}

half3 clipDarken(half3 rgb)
{
    return max(rgb - _Bloom_Threshold, 0);
}

half4 fragBloomVertical(VarBloom input) : SV_Target
{
    // return half4(1,0,0,1);

    half3 col = clipDarken(_BloomTex.Sample(sampler_BloomTex, input.uv[0]).rgb);
    col += clipDarken(_BloomTex.Sample(sampler_BloomTex, input.uv[1]).rgb);
    col += clipDarken(_BloomTex.Sample(sampler_BloomTex, input.uv[2]).rgb);
    col += clipDarken(_BloomTex.Sample(sampler_BloomTex, input.uv[3]).rgb);
    col += clipDarken(_BloomTex.Sample(sampler_BloomTex, input.uv[4]).rgb);
    col *= 0.2;
    return half4(col,1);
}

half4 fragBloomHorizontal(VarBloom input) : SV_Target
{
    // return half4(0,1,1,1);

    half3 col = _BloomTex.Sample(sampler_BloomTex, input.uv[0]).rgb;
    col += _BloomTex.Sample(sampler_BloomTex, input.uv[1]).rgb;
    col += _BloomTex.Sample(sampler_BloomTex, input.uv[2]).rgb;
    col += _BloomTex.Sample(sampler_BloomTex, input.uv[3]).rgb;
    col += _BloomTex.Sample(sampler_BloomTex, input.uv[4]).rgb;
    col *= 0.2;
    // col = HDR2LDR(col);
    return half4(col,1);
}

#endif