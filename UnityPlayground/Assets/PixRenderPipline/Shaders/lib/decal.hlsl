#ifndef PIX_DECAL_HLSL
#define PIX_DECAL_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes_Decal
{
    float4 positionOS : POSITION;
};

struct Varyings_Decal
{
    float4 positionCS : SV_POSITION;
};

Varyings_Decal vert(Attributes_Decal input)
{
    Varyings_Decal output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

half4 frag_stencil(Varyings_Decal input) : SV_Target
{
    return 0;
}

half4 frag_decal(Varyings_Decal input) : SV_Target
{
    return half4(1, 0, 0, 1);
}

#endif