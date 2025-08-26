#ifndef STANDARD_INCLUDED
#define STANDARD_INCLUDED

#if defined TAA || defined MOTION_BLUR
    #define MOTION_VECTOR_ON
#endif

#ifdef SKINNED_MESH
    #define GPU_SKIN
#endif

#ifdef GPU_SKIN
#include "gpuskin.hlsl"
#endif

#include "common.hlsl"
#include "gbuffer.hlsl"
#include "ibl.hlsl"
#include "light.hlsl"
#include "shading.hlsl"


float4x4 _MatrixVP;
half _DebugBrightness;

#ifdef MOTION_VECTOR_ON
float4x4 _MatrixVP_Prev;
#endif

UNITY_INSTANCING_BUFFER_START(Props_main)
#ifdef MOTION_VECTOR_ON
    UNITY_DEFINE_INSTANCED_PROP(float4x4, _PreviousLocalToWorld)
#endif
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
UNITY_INSTANCING_BUFFER_END(Props_main)

struct Attr_Standard
{
    float4 positionOS   : POSITION;
    float2 uv           : TEXCOORD0;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
#ifdef ENABLE_BENTNORMAL
    float4 bentNormal   : COLOR;
#endif

#ifdef GPU_SKIN
    float4 boneWeights  : BLENDWEIGHTS; 
    uint4 boneIndices   : BLENDINDICES; 
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Vary_Standard
{
    float4 positionCS   : SV_POSITION;
    float3x3 tbnVS      : NORMAL;
    float3x3 tbnWS      : TANGENT;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;

#ifdef ENABLE_BENTNORMAL
    float4 bentNormalWS   : TEXCOORD2;
#endif

#ifdef MOTION_VECTOR_ON
    float4 prevPosCS    : TEXCOORD3;
#endif

    float4 screenUV     : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Vary_Standard vert_Standard(Attr_Standard input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 prevPosOS = float4(input.positionOS.xyzw);

    #ifdef GPU_SKIN
        transformSkinnedPos(input.boneWeights, input.boneIndices, input.positionOS);
        transformSkinnedDir(input.boneWeights, input.boneIndices, input.normalOS);
        transformSkinnedDir(input.boneWeights, input.boneIndices, input.tangentOS.xyz);
    #endif

    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float3 tangentWS = TransformObjectToWorldNormal(input.tangentOS.xyz)* input.tangentOS.w;
    float3 bitangentWS = cross(normalWS, tangentWS) ;
    tangentWS = cross(bitangentWS, normalWS);
    float3 positionWS = mul(UNITY_MATRIX_M, input.positionOS).xyz;
    float3x3 mat_V = GetMatrix_WorldToView(positionWS);
    float3 normalVS = mul(mat_V, normalWS);
    float3 tangentVS = mul(mat_V, tangentWS);
    float3 bitangentVS = mul(mat_V, bitangentWS);
    float3x3 tbnWS = float3x3(tangentWS, bitangentWS, normalWS);
    float3x3 tbnVS = float3x3(tangentVS, bitangentVS, normalVS);
    float4 mainTex_st = UNITY_ACCESS_INSTANCED_PROP(Props_main, _MainTex_ST);

    Vary_Standard output;
    output.positionCS = mul(_MatrixVP, float4(positionWS,1));
    output.tbnVS = tbnVS;
    output.tbnWS = tbnWS;
    output.uv = input.uv*mainTex_st.xy + mainTex_st.zw;
    output.screenUV = output.positionCS;
    output.positionWS = positionWS;

    #ifdef ENABLE_BENTNORMAL
        float4 bentNormalRaw = input.bentNormal;
        float3 bentNormal =bentNormalRaw.xyz*2 - 1; 
        float len = length(bentNormal);
        float ao = saturate((len + bentNormalRaw.w)*2.5);
        ao = sq(ao);
        bentNormal /= len;

        #ifdef GPU_SKIN
            transformSkinnedDir(input.boneWeights, input.boneIndices, bentNormal);
        #endif
        
        float3 bentNormalWS = mul(bentNormal, tbnWS);
        output.bentNormalWS = half4(bentNormalWS, ao);
    #endif

    #ifdef MOTION_VECTOR_ON
        #ifdef GPU_SKIN
            transformPreviousSkinnedPos(input.boneWeights, input.boneIndices, prevPosOS);
        #endif

        float4x4 previousLocalToWorld = UNITY_ACCESS_INSTANCED_PROP(Props_main, _PreviousLocalToWorld);
        float4 prevPosWS = mul(previousLocalToWorld, prevPosOS);
        output.prevPosCS = mul(_MatrixVP_Prev, prevPosWS);
    #endif

    UNITY_TRANSFER_INSTANCE_ID(input, output);
    return output;
}

void evaluateAll(MaterialData matData, half2 screenUV, inout half3 result){
    if(matData.shadingModel == SHADING_MODEL_UNLIT){
        result = matData.albedo;
    }else{
        evaluateIBL(matData, screenUV, result);
    
        if(PIX_LIGHT_COUNT > 0){
            [loop]
            for(int i = 0;i<PIX_LIGHT_COUNT;i++)
            {
                PixLight light = GetPixLight(i);

                if(light.enabled)
                    evaluateLight(light, matData, screenUV, result);
            }
        }

        #ifdef FOG
        evaluateFog(matData, result);
        #endif
    }

    result = HDR2LDR(result);

    #ifdef PP_SUN_VOLUME
       
        evaluateSunVolume(result, screenUV);
    #endif
}



#endif