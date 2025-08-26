Shader "Pix/Standard"
{
    Properties
    {
        Group#Feature("Feature", Int) = 1
            _IsSkinnedMesh("T(SKINNED_MESH)/isSkinnedMesh[Main]", Int) = 0
            _ShadingModel ("E/ShadingModel:Unlit,Lit,Hair,SSS[Feature]", Int) = 0
            _CullMode("E/Cull:Off,Front,Back[Feature]", Int) = 2
            _BentNormal("T(ENABLE_BENTNORMAL)/BentNormal[Feature]", Int) = 0

        Group#SSS("SSS[_ShadingModel_3]", Int) = 1
            _SSS_Profile("SSS/profile[SSS,_ShadingModel_3]", Int) = 0

        Group#Main("Main", Int) = 1
            _Color ("Color[Main]", Color) = (1,1,1,1)
            _ColorMultiply("ColorMultiply[Main]", Float) = 1
            _MainTex ("MainTex[Main]{sRGB:on}", 2D) = "white" {}
            _AlphaClip("T(_ALPHATEST_ON)/AlphaClip[Main]", Int) = 0
            _Cutoff("Cutoff[Main,_ALPHATEST_ON]", Range(0, 1)) = 0.5
            _ParamTex("ParamTex[Main,!_ShadingModel_0]{sRGB:off}", 2D) = "white" {}

        Group#Param("Param[!_ShadingModel_0]", Int) = 1
            _RoughnessOffset("RoughnessOffset[Param,!_ShadingModel_0]", Range(-1,1))=0
            _MetallicOffset("MetallicOffset[Param,!_ShadingModel_0]", Range(-1,1))=0

        Group#Nor("Normal[!_ShadingModel_0]", Int) = 1
            _NormalTex("NormalTex[Nor,!_ShadingModel_0]{type:Normal}", 2D) = "bump" {}
            _NormalIntensity("Intensity[Nor,!_ShadingModel_0]", Range(0,2)) = 1

        Group#Hair("Hair[_ShadingModel_2]", Int) = 1
            _Anisotropy("Anisotropy[Hair,_ShadingModel_2]", Range(-1,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest Equal
        Cull [_CullMode]

        // 0 forward
        Pass
        {
            ZWrite Off
            ZTest Equal
            Cull [_CullMode]

            Name "PixForward"
            Tags { "LightMode"="PixForward" }

            HLSLPROGRAM
            #pragma vertex vert_Standard
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _ALPHATEST_ON
            #pragma multi_compile _ SKINNED_MESH
            #pragma multi_compile _ TAA
            #pragma multi_compile _ FOG
            #pragma multi_compile _ PP_SUN_VOLUME
            #pragma shader_feature ENABLE_BENTNORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/standard.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                half _ColorMultiply;
                int _ShadingModel;
                int _SSS_Profile;
                half _NormalIntensity;
                half _Anisotropy;
                half _RoughnessOffset;
                half _MetallicOffset;

                #ifdef _ALPHATEST_ON
                half _Cutoff;
                #endif
            CBUFFER_END
            

            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalTex);SAMPLER(sampler_NormalTex);
            TEXTURE2D(_ParamTex);SAMPLER(sampler_ParamTex);

            #ifdef MOTION_VECTOR_ON
            struct FragmentOutput
            {
                half4 color : SV_Target0;    
                half4 motionVector : SV_Target1; 
            };

            FragmentOutput frag(Vary_Standard input, float facing : VFACE)
            #else
            half4 frag(Vary_Standard input, float facing : VFACE): SV_Target
            #endif
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 ndcPos = input.screenUV.xyz/input.screenUV.w;
                half2 screenUV = ndcPos.xy*0.5+0.5;
                screenUV.y = 1-screenUV.y;
                half ndcDepth = ndcPos.z;

                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                #if _ALPHATEST_ON
                    TestAlpha(color.a, _Cutoff, screenUV);
                #endif
                
                color.rgb *= _ColorMultiply;

                half3 albedo = color.rgb;
                float3 normal = normalize(input.tbnWS[2]);
                float3 tangent = input.tbnWS[0];
                float3 bitangent = input.tbnWS[1];

                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, input.uv));
                normal = lerp(normal, normalize(mul(normalTS, float3x3(tangent,bitangent,normal))), _NormalIntensity);

                half3 param = SAMPLE_TEXTURE2D(_ParamTex, sampler_ParamTex, input.uv).rgb;
                half linearRoughness = saturate(param.g + _RoughnessOffset);
                half metallic = saturate(param.b + _MetallicOffset);
                half4 bentNormal = half4(normal, 1);
                
                #ifdef ENABLE_BENTNORMAL
                    bentNormal = input.bentNormalWS;
                    bentNormal.xyz = mul(input.tbnVS, bentNormal.xyz);
                #endif

                half3 positionWS = input.positionWS.xyz;
                half3 normalVS = input.tbnVS[2];
                normalVS.z = max(0.01, normalVS.z);

                half anisotropy = _Anisotropy;
                if (_ShadingModel != SHADING_MODEL_SSS){
                    anisotropy = 0;
                }

                MaterialData matData = PackMaterialData(screenUV, _ShadingModel, albedo, metallic, linearRoughness, anisotropy,
                    bentNormal, positionWS, input.tbnWS[0], input.tbnWS[1], normal, normalVS, input.tbnVS, _SSS_Profile);

                //----------------shading
                
                half3 result = half3(0, 0, 0);
                evaluateAll(matData, screenUV, result);

                #ifdef MOTION_VECTOR_ON
                    float2 preNdcPos = input.prevPosCS.xy / input.prevPosCS.w;
                    float2 preScreenUV = preNdcPos * 0.5 + 0.5;
                    screenUV.y = 1-screenUV.y;
                    half2 motionVector = preScreenUV - screenUV;
                    motionVector.y = -motionVector.y;

                    FragmentOutput output;
                    output.color = half4(result, 1);
                    output.motionVector = half4(motionVector*0.5 + 0.5, 0, 0);
                    
                    return output;
                #else
                    return half4(result, 1);
                #endif
            }
            ENDHLSL
        }

        // 1 gbuffer
        Pass
        {
            Name "PixGBuffer"
            Tags { "LightMode" = "PixGBuffer" }

            HLSLPROGRAM
            #pragma vertex vert_Standard
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _ALPHATEST_ON
            #pragma multi_compile _ SKINNED_MESH
            #pragma multi_compile _ TAA
            #pragma shader_feature ENABLE_BENTNORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/standard.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4      _Color;
                half        _ColorMultiply;
                int         _ShadingModel;
                int         _SSS_Profile;
                half        _NormalIntensity;
                half        _Anisotropy;
                half        _RoughnessOffset;
                half        _MetallicOffset;
                half        _FlipNormal;

                #ifdef _ALPHATEST_ON
                half        _Cutoff;
                #endif
            CBUFFER_END

            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalTex);SAMPLER(sampler_NormalTex);
            TEXTURE2D(_ParamTex);SAMPLER(sampler_ParamTex);
            
            // MRT 输出
            struct FragmentOutput
            {
                half4 gbuffer_0 : SV_Target0;    
                half4 gbuffer_1 : SV_Target1; 
                half4 gbuffer_2 : SV_Target2;
            #ifdef MOTION_VECTOR_ON
                half4 gbuffer_3 : SV_Target3;
            #endif
            };

            FragmentOutput frag(Vary_Standard input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                color.rgb *= _ColorMultiply;

                half2 screenUV = input.screenUV.xy/input.screenUV.w;
                screenUV = screenUV*0.5+0.5;

                #if _ALPHATEST_ON
                    TestAlpha(color.a, _Cutoff, screenUV);
                #endif

                float3 normal = normalize(input.tbnVS[2]);

                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, input.uv));
                float3x3 TBN = input.tbnVS;
                normal = lerp(normal, normalize(mul(normalTS, TBN)), _NormalIntensity);

                half3 param = SAMPLE_TEXTURE2D(_ParamTex, sampler_ParamTex, input.uv).rgb;
                half roughness = saturate(param.g + _RoughnessOffset);
                half metalness = saturate(param.b + _MetallicOffset);

                half4 bentNormal = half4(normal, 1);
                #ifdef ENABLE_BENTNORMAL
                bentNormal = input.bentNormalWS;
                bentNormal.xyz = mul(input.tbnVS, bentNormal.xyz);
                #endif

                float2 motionVector = float2(0, 0);

                #ifdef MOTION_VECTOR_ON
                float2 preNdcPos = input.prevPosCS.xy / input.prevPosCS.w;
                float2 preScreenUV = preNdcPos * 0.5 + 0.5;
                motionVector = preScreenUV - screenUV;
                #endif

                FragmentOutput output;
                GBuffer gbuffer = PackGBuffer(color, _ShadingModel, normal, bentNormal, input.tbnWS[0], 
                    roughness, metalness, motionVector, _Anisotropy, _SSS_Profile);
                
                output.gbuffer_0 = gbuffer.gbuffer_0;
                output.gbuffer_1 = gbuffer.gbuffer_1;
                output.gbuffer_2 = gbuffer.gbuffer_2;

                #ifdef MOTION_VECTOR_ON
                output.gbuffer_3 = gbuffer.gbuffer_3;
                #endif

                return output;
            }
            ENDHLSL
        }

        // 2 earlyZ (暂时没有使用了，用的是下面那个forwardEarlyZ)
        Pass
        {
            Name "PixEarlyZ"
            Tags { "LightMode"="PixEarlyZ" }
            ZWrite On
            ZTest LESS
            ColorMask 0

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _ALPHATEST_ON
            #pragma multi_compile _ TAA
            #pragma multi_compile _ SKINNED_MESH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/common.hlsl"
            
            #ifdef SKINNED_MESH
            #define GPU_SKIN
            #endif

            #ifdef GPU_SKIN
            #include "lib/gpuskin.hlsl"
            #endif

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
                #if _ALPHATEST_ON
                float2 uv : TEXCOORD0;
                #endif

                #ifdef GPU_SKIN
                float4 boneWeights  : BLENDWEIGHTS; 
                uint4 boneIndices   : BLENDINDICES; 
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float4 screenUV : TEXCOORD0;

            #ifdef _ALPHATEST_ON
                float2 uv : TEXCOORD1;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            #ifdef _ALPHATEST_ON
                TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);

                UNITY_INSTANCING_BUFFER_START(Props)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
                    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
                UNITY_INSTANCING_BUFFER_END(Props)
            #endif

            float4x4 _MatrixVP;


            VaryingsDepth vert(AttributesDepth input)
            {
                VaryingsDepth output;

                UNITY_SETUP_INSTANCE_ID(input);

                #ifdef GPU_SKIN
                transformSkinnedPos(input.boneWeights, input.boneIndices, input.positionOS);
                #endif

                float3 positionWS = mul(UNITY_MATRIX_M, input.positionOS).xyz;

                output.positionCS = mul(_MatrixVP, float4(positionWS,1));
                output.screenUV = output.positionCS;
                
                #ifdef _ALPHATEST_ON
                float4 main_st = UNITY_ACCESS_INSTANCED_PROP(Props, _MainTex_ST);
                output.uv = input.uv*main_st.xy + main_st.zw;
                #endif

                UNITY_TRANSFER_INSTANCE_ID(input, output);
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                #ifdef _ALPHATEST_ON
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                half cutoff = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutoff);
                TestAlpha(alpha, cutoff, input.screenUV);
                #endif

                return 0;
            }
            ENDHLSL
        }

        // 3 forwardEarlyZ
        Pass
        {
            Name "PixForwardEarlyZ"
            Tags { "LightMode"="PixForwardEarlyZ" }
            ZWrite On
            ZTest LEqual
            Cull [_CullMode]

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _ALPHATEST_ON
            #pragma multi_compile _ SKINNED_MESH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/common.hlsl"
            
            #ifdef SKINNED_MESH
            #define GPU_SKIN
            #endif

            #ifdef GPU_SKIN
            #include "lib/gpuskin.hlsl"
            #endif

            struct AttributesDepth
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;

                #if _ALPHATEST_ON
                float2 uv           : TEXCOORD0;
                #endif

                #ifdef GPU_SKIN
                float4 boneWeights  : BLENDWEIGHTS; 
                uint4 boneIndices   : BLENDINDICES; 
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDepth
            {
                float4 positionCS   : SV_POSITION;
                float4 screenUV     : TEXCOORD0;

                float3 normalVS     : NORMAL;

            #ifdef _ALPHATEST_ON
                float2 uv           : TEXCOORD1;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            #ifdef _ALPHATEST_ON
                TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);

                UNITY_INSTANCING_BUFFER_START(Props)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
                    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
                UNITY_INSTANCING_BUFFER_END(Props)
            #endif

            float4x4 _MatrixVP;

            VaryingsDepth vert(AttributesDepth input)
            {
                VaryingsDepth output;

                UNITY_SETUP_INSTANCE_ID(input);

                #ifdef GPU_SKIN
                transformSkinnedPos(input.boneWeights, input.boneIndices, input.positionOS);
                #endif

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = mul(UNITY_MATRIX_M, input.positionOS).xyz;

                float3x3 mat_V = GetMatrix_WorldToView(positionWS);
                float3 normalVS = mul(mat_V, normalWS);

                output.positionCS = mul(_MatrixVP, float4(positionWS,1));
                output.screenUV = output.positionCS;
                output.normalVS = normalVS;
                
                #ifdef _ALPHATEST_ON
                float4 main_st = UNITY_ACCESS_INSTANCED_PROP(Props, _MainTex_ST);
                output.uv = input.uv*main_st.xy + main_st.zw;
                #endif

                UNITY_TRANSFER_INSTANCE_ID(input, output);
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                float3 ndcPos = input.screenUV.xyz/input.screenUV.w;
                float2 screenUV = ndcPos.xy*0.5 + 0.5;
                screenUV.y = 1-screenUV.y;

                UNITY_SETUP_INSTANCE_ID(input);
                #ifdef _ALPHATEST_ON
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                half cutoff = UNITY_ACCESS_INSTANCED_PROP(Props, _Cutoff);
                TestAlpha(alpha, cutoff, screenUV);
                #endif

                half3 normal = normalize(input.normalVS);
                half depth = ndcPos.z;

                return half4(EncodeFloatRG(depth), PackNormalHemiOctEncode(normal)*0.5+0.5);
            }
            ENDHLSL
        }

        // 4 shadowCaster
        Pass
        {
            Name "PixShadowCaster"
            Tags { "LightMode"="PixShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _ALPHATEST_ON
            #pragma multi_compile _ SKINNED_MESH
            #pragma multi_compile _ TAA
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifdef SKINNED_MESH
                #define GPU_SKIN
            #endif

            #ifdef GPU_SKIN
            #include "lib/gpuskin.hlsl"
            #endif

            #include "lib/struct.hlsl"
            #include "lib/light.hlsl"

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
            #if _ALPHATEST_ON
                float2 uv : TEXCOORD0;
            #endif

            #ifdef GPU_SKIN
                float4 boneWeights  : BLENDWEIGHTS; 
                uint4 boneIndices   : BLENDINDICES; 
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
            #if _ALPHATEST_ON
                float2 uv : TEXCOORD0;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            #if _ALPHATEST_ON
                TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);

                UNITY_INSTANCING_BUFFER_START(Props_sc)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
                    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
                UNITY_INSTANCING_BUFFER_END(Props_sc)
            #endif

            int _LightIndex;

            VaryingsDepth vert(AttributesDepth input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                #ifdef GPU_SKIN
                transformSkinnedPos(input.boneWeights, input.boneIndices, input.positionOS);
                #endif

                PixLight light = GetPixLight(_LightIndex);
                float4 posWorld = mul(UNITY_MATRIX_M, input.positionOS);

                VaryingsDepth output;
                output.positionCS = mul(light.VP,posWorld);
            #if _ALPHATEST_ON
                float4 main_st = UNITY_ACCESS_INSTANCED_PROP(Props_sc, _MainTex_ST);
                output.uv = input.uv*main_st.xy + main_st.zw;
            #endif
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
            #if _ALPHATEST_ON
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                half cutoff = UNITY_ACCESS_INSTANCED_PROP(Props_sc, _Cutoff);
                clip(alpha - cutoff);
            #endif

                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "PixTransparent"
            Tags { "LightMode"="PixTransparent" }

            ZWrite Off
            ZTest LESS
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile SSAO_QUALITY_OFF
            #pragma multi_compile _ TAA
            #pragma multi_compile _ SKINNED_MESH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifdef SKINNED_MESH
            #define GPU_SKIN
            #endif

            #ifdef GPU_SKIN
            #include "lib/gpuskin.hlsl"
            #endif
        
            #include "lib/light.hlsl"
            #include "lib/sss.hlsl"
            #include "lib/shading.hlsl"
            #include "lib/ibl.hlsl"

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;

            #ifdef GPU_SKIN
                float4 boneWeights  : BLENDWEIGHTS; 
                uint4 boneIndices   : BLENDINDICES; 
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            float4x4 _MatrixVP;

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            int _ShadingModel;
            float4 _Color;
            half _ColorMultiply;
            CBUFFER_END

            VaryingsDepth vert(AttributesDepth input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                #ifdef GPU_SKIN
                transformSkinnedPos(input.boneWeights, input.boneIndices, input.positionOS);
                transformSkinnedDir(input.boneWeights, input.boneIndices, input.normalOS);
                #endif

                float3 positionWS = mul(UNITY_MATRIX_M, input.positionOS).xyz;

                VaryingsDepth output;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionCS = mul(_MatrixVP, float4(positionWS,1));
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                #ifdef GPU_SKIN
                output.positionCS.z -= 0.00001;
                #endif

                UNITY_TRANSFER_INSTANCE_ID(input, output);
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 result = half3(0,0,0);

                half3 diffuse = color.rgb * _Color.rgb * _ColorMultiply;

                if(_ShadingModel!=0){
                    half3 normalWS = input.normalWS;
                    PixLight light = GetPixLight(0);
                    evaluateLightSimple(light, diffuse, normalWS, result);
                    evaluateIBLSimple(diffuse, normalWS, result);
                }else{
                    result = diffuse;
                }

                result = HDR2LDR(result);
                // color.a = smoothstep(0,0.5,color.a);

                return half4(result, color.a);
            }
            ENDHLSL
        }
    }

    CustomEditor "PixShaderGUI"
}
