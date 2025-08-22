
Shader "Hidden/Pix/Deferred"
{
    Properties
    {
        _Debug ("Debug", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest Always

        Stencil
        {
            Ref 0
            Comp LESS
        }

        Pass
        {
            Name "PixDeferred"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ORTHOGRAPHIC
            #pragma multi_compile _ TAA
            #pragma multi_compile _ FOG
            #pragma multi_compile _ PP_SUN_VOLUME
            #pragma multi_compile PIX_STYLE_PBR PIX_STYLE_NPR
            #pragma multi_compile SSAO_QUALITY_OFF SSAO_QUALITY_POOR SSAO_QUALITY_LOW SSAO_QUALITY_MEDIUM SSAO_QUALITY_HIGH
            
            #pragma shader_feature DEBUG_LIGHT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/light.hlsl"
            #include "lib/gbuffer.hlsl"
            #include "lib/sss.hlsl"
            #include "lib/shading.hlsl"
            #include "lib/ibl.hlsl"

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 tiled_id : TEXCOORD1;
            };

            VaryingsDepth vert(AttributesDepth input)
            {
                float2 uv = input.uv;
                float2 pos = uv*2.0-1.0;

                // #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                // #endif

                half4 tiled_id = SAMPLE_TEXTURE2D_LOD(_PixTiledID, sampler_PixTiledID, uv, 0);

                VaryingsDepth output;
                output.uv = uv;
                output.tiled_id = tiled_id;
                output.positionCS = float4(pos, 0.0, 1.0);
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                float2 uv = input.uv;
                MaterialData matData = UnpackGBuffer(uv);

                if(matData.shadingModel == SHADING_MODEL_UNLIT)
                    return half4(matData.albedo, 1);

                // return selfOcclusion(matData, matData.reflectDir, matData.roughness);

                half3 result = half3(0, 0, 0);

                evaluateIBL(matData, uv, result);
                
                if(PIX_LIGHT_COUNT > 0){
                    [loop]
                    for(int i = 0;i<PIX_LIGHT_COUNT;i++)
                    {
                        PixLight light = GetPixLight(i);

                        if(light.enabled)
                            evaluateLight(light, matData, uv, result);
                    }
                }

                #ifdef FOG
                evaluateFog(matData, result);
                #endif

                

                half3 ldr = HDR2LDR(result);

                #ifdef PP_SUN_VOLUME
                evaluateSunVolume(ldr, uv);
                #endif
                
                return half4(ldr,1);
            }
            ENDHLSL
        }
    }
}
