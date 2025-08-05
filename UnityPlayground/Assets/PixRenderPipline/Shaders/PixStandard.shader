Shader "Pix/Standard"
{
    Properties
    {
        Group#Feature("Feature", Int) = 1
            _ShadingModel ("E/ShadingModel:Unlit,Lit,Hair[Feature]", Int) = 0
            _CullMode("E/Cull:Off,Front,Back[Feature]", Int) = 2
            _BentNormal("T(ENABLE_BENTNORMAL)/BentNormal[Feature]", Int) = 0

        Group#Main("Main", Int) = 1
            _Color ("Color[Main]", Color) = (1,1,1,1)
            _MainTex ("MainTex[Main]{sRGB:on}", 2D) = "white" {}
            _AlphaClip("T(_ALPHATEST_ON)/AlphaClip[Main]", Int) = 0
            _Cutoff("Cutoff[Main,_ALPHATEST_ON]", Range(0, 1)) = 0.5
            _ParamTex("ParamTex[Main]{sRGB:off}", 2D) = "white" {}

        Group#Param("Param", Int) = 1
            _RoughnessOffset("RoughnessOffset[Param]", Range(-1,1))=0
            _MetallicOffset("MetallicOffset[Param]", Range(-1,1))=0

        Group#Nor("Normal[_ShadingModel_1|_ShadingModel_2]", Int) = 1
            _NormalTex("NormalTex[Nor,_ShadingModel_1|_ShadingModel_2]{type:Normal}", 2D) = "bump" {}
            _NormalIntensity("Intensity[Nor,_ShadingModel_1|_ShadingModel_2]", Range(0,2)) = 1

        Group#Hair("Hair[_ShadingModel_2]", Int) = 1
            _Anisotropy("Anisotropy[Hair,_ShadingModel_2]", Range(-1,1)) = 0.5

        _Rule1("K|(EXPORT_TANGENT)/_ShadingModel" ,Int ) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest Equal
        Cull[_CullMode]

        Pass
        {
            Name "PixGBuffer"
            Tags { "LightMode" = "PixGBuffer" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ALPHATEST_ON
            // #pragma multi_compile _ GPU_SKIN
            #pragma multi_compile _ TAA
            #pragma multi_compile _ MOTION_BLUR
            #pragma shader_feature PIX_STYLE_PBR PIX_STYLE_NPR 
            #pragma shader_feature EXPORT_TANGENT 
            #pragma shader_feature ENABLE_BENTNORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #if defined TAA || defined MOTION_BLUR
            #define MOTION_VECTOR_ON
            #endif

            #if defined MOTION_BLUR || defined MOTION_BLUR || defined TAA
            #define GPU_SKIN
            #endif

            #ifdef GPU_SKIN
            #include "lib/gpuskin.hlsl"
            #endif

            #include "lib/light.hlsl"
            #include "lib/gbuffer.hlsl"
            #include "lib/random.hlsl"
            
            struct Attributes
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
            };

            struct Varying
            {
                float4 positionCS   : SV_POSITION;
                float3 normalVS     : NORMAL;
                float3 tangentVS    : TANGENT;
                float3 bitangentVS  : TEXCOORD1;
                float2 uv           : TEXCOORD0;
                float3 tangentWS    : TEXCOORD2;
            #ifdef ENABLE_BENTNORMAL
                float4 bentNormal   : COLOR;
            #endif

            #ifdef MOTION_VECTOR_ON
                float4 prevPosCS    : TEXCOORD3;
            #endif

                float4 screenUV     : TEXCOORD4;
            
            };

            float4 _Color;
            int _ShadingModel;
            half _NormalIntensity;
            half _Anisotropy;
            half _RoughnessOffset;
            half _MetallicOffset;

            #ifdef _ALPHATEST_ON
            half _Cutoff;
            #endif

            #ifdef MOTION_VECTOR_ON
            float4x4 _PreviousLocalToWorld;
            float4x4 _MatrixVP_Prev;
            float4x4 _CurrentLocalToWorld;
            #endif

            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _MainTex_ST;
            TEXTURE2D(_NormalTex);SAMPLER(sampler_NormalTex);
            TEXTURE2D(_ParamTex);SAMPLER(sampler_ParamTex);

            Varying vert(Attributes input)
            {
                #ifdef GPU_SKIN
                float4 prevPosOS = input.positionOS;
                transformSkinnedPos(input.boneWeights, input.boneIndices, input.positionOS);
                transformSkinnedDir(input.boneWeights, input.boneIndices, input.normalOS);
                transformSkinnedDir(input.boneWeights, input.boneIndices, input.tangentOS.xyz);
                #endif

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 tangentWS = TransformObjectToWorldNormal(input.tangentOS.xyz)* input.tangentOS.w;
                float3 bitangentWS = cross(normalWS, tangentWS) ;
                float3 positionWS = mul(unity_ObjectToWorld, input.positionOS).xyz;

                // 计算视线空间下的法线
                float3 cameraPos = _WorldSpaceCameraPos;
                float3 viewDir = normalize(cameraPos - positionWS);
                float3 viewUp = float3(0.0, 1.0, 0.0);
                float3 up = mul((float3x3)UNITY_MATRIX_I_V, viewUp);
                float3 right = normalize(cross(viewDir, up));

                float3x3 mat_V = float3x3(right, up, viewDir);
                float3 normalVS = mul(mat_V, normalWS);
                float3 tangentVS = mul(mat_V, tangentWS);
                float3 bitangentVS = mul(mat_V, bitangentWS);

            #ifdef ENABLE_BENTNORMAL
                float4 bentNormal = input.bentNormal;
                bentNormal.xyz = bentNormal.xyz*2 - 1;

                #ifdef GPU_SKIN
                transformSkinnedDir(input.boneWeights, input.boneIndices, bentNormal.xyz);
                #endif

                float3x3 tbn = float3x3(tangentWS, bitangentWS, normalWS);
                bentNormal.xyz = mul(bentNormal.xyz, tbn);
            #endif

                Varying output;
                output.positionCS = mul(unity_MatrixVP, float4(positionWS,1));
                output.normalVS = normalVS;
                output.tangentVS = tangentVS;
                output.bitangentVS = bitangentVS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.tangentWS = tangentWS;
            #ifdef ENABLE_BENTNORMAL
                output.bentNormal = bentNormal;
            #endif

            #ifdef MOTION_VECTOR_ON
                transformPreviousSkinnedPos(input.boneWeights, input.boneIndices, prevPosOS);
                float4 prevPosWS = mul(_PreviousLocalToWorld, prevPosOS);
                output.prevPosCS = mul(_MatrixVP_Prev, prevPosWS);
                output.screenUV = output.positionCS;
            #endif

                return output;
            }

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

            FragmentOutput frag(Varying input)
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                half2 screenUV = input.screenUV.xy/input.screenUV.w;
                screenUV = screenUV*0.5+0.5;

                #if _ALPHATEST_ON
                    #ifdef TAA
                        half alpha = color.a;
                        // half c = _Cutoff*0.5;
                        alpha = smoothstep(0, 1-_Cutoff*0.5,alpha);
                        half a = dether(screenUV, alpha) - 0.001;
                        clip(a);
                    #else
                        clip(color.a - _Cutoff);
                    #endif
                #endif

                float3 normal = normalize(input.normalVS);

                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, input.uv));
                float3x3 TBN = float3x3(input.tangentVS, input.bitangentVS, input.normalVS);
                normal = lerp(normal, normalize(mul(normalTS, TBN)), _NormalIntensity);

                half3 param = SAMPLE_TEXTURE2D(_ParamTex, sampler_ParamTex, input.uv).rgb;
                half ior = 1-param.r;
                half roughness = saturate(param.g + _RoughnessOffset);
                half metalness = saturate(param.b + _MetallicOffset);
                roughness = ior*ior + roughness;

                half4 bentNormal = half4(0,0,1,1);
                
                #ifdef ENABLE_BENTNORMAL
                bentNormal = input.bentNormal;
                #endif

                FragmentOutput output;
                GBuffer gbuffer = PackGBuffer(color, _ShadingModel, normal, bentNormal, input.tangentWS, roughness, metalness, _Anisotropy);
                output.gbuffer_0 = gbuffer.gbuffer_0;
                output.gbuffer_1 = gbuffer.gbuffer_1;
                output.gbuffer_2 = gbuffer.gbuffer_2;

            #ifdef MOTION_VECTOR_ON
                float2 preNdcPos = input.prevPosCS.xy / input.prevPosCS.w;
                float2 preScreenUV = preNdcPos * 0.5 + 0.5;

                float2 motionVector = preScreenUV - screenUV;
                output.gbuffer_3 = half4(motionVector*0.5 +0.5, 0, 0);
            #endif

                return output;
            }
            ENDHLSL
        }

        Pass
        {
            Name "PixEarlyZ"
            Tags { "LightMode"="PixEarlyZ" }
            ZWrite On
            ZTest LEqual
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
            #pragma multi_compile _ _ALPHATEST_ON
            #pragma multi_compile _ TAA
            #pragma multi_compile _ GPU_SKIN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/random.hlsl"

            #ifdef TAA
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
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float4 screenUV : TEXCOORD0;

            #ifdef _ALPHATEST_ON
                float2 uv : TEXCOORD1;
            #endif
            };

            #ifdef _ALPHATEST_ON
                TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
                half4 _MainTex_ST;
                half _Cutoff;
            #endif


            VaryingsDepth vert(AttributesDepth input)
            {
                VaryingsDepth output;

                #ifdef GPU_SKIN
                transformSkinnedPos(input.boneWeights, input.boneIndices, input.positionOS);
                #endif

                float3 positionWS = mul(unity_ObjectToWorld, input.positionOS).xyz;

                output.positionCS = mul(unity_MatrixVP, float4(positionWS,1));
                
                #ifdef _ALPHATEST_ON
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                #endif

                // #ifdef TAA
                output.screenUV = output.positionCS;
                // #endif

                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
            #ifdef _ALPHATEST_ON
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;

                #ifdef TAA
                    half2 screenUV = input.screenUV.xy/input.screenUV.w;
                    screenUV = screenUV*0.5+0.5;
                    alpha = smoothstep(0,1 - _Cutoff*0.5,alpha);
                    half a = dether(screenUV, alpha) - 0.001;
                    clip(a);
                #else
                    clip(alpha - _Cutoff);
                #endif
            #endif

                return 1;
            }
            ENDHLSL
        }

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
            #pragma multi_compile _ _ALPHATEST_ON
            #pragma multi_compile _ GPU_SKIN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
            #if _ALPHATEST_ON
                float2 uv : TEXCOORD0;
            #endif
            };

            #if _ALPHATEST_ON
                TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
                half4 _MainTex_ST;
                half _Cutoff;
            #endif

            int _LightIndex;

            VaryingsDepth vert(AttributesDepth input)
            {
                #ifdef GPU_SKIN
                transformSkinnedPos(input.boneWeights, input.boneIndices, input.positionOS);
                #endif

                PixLight light = GetPixLight(_LightIndex);
                float4x4 VP = light.VP;
                float4 posWorld = mul(unity_ObjectToWorld, input.positionOS);

                VaryingsDepth output;
                output.positionCS = mul(VP,posWorld);
            #if _ALPHATEST_ON
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            #endif
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
            #if _ALPHATEST_ON
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
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
            #pragma multi_compile SSAO_QUALITY_OFF
            #pragma multi_compile _ GPU_SKIN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifdef GPU_SKIN
            #include "lib/gpuskin.hlsl"
            #endif
        
            #include "lib/light.hlsl"
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
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _MainTex_ST;

            VaryingsDepth vert(AttributesDepth input)
            {
                #ifdef GPU_SKIN
                transformSkinnedPos(input.boneWeights, input.boneIndices, input.positionOS);
                transformSkinnedDir(input.boneWeights, input.boneIndices, input.normalOS);
                #endif

                float3 positionWS = mul(unity_ObjectToWorld, input.positionOS).xyz;

                VaryingsDepth output;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionCS = mul(unity_MatrixVP, float4(positionWS,1));
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                #ifdef GPU_SKIN
                output.positionCS.z -= 0.00002;
                #endif

                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 result = half3(0,0,0);

                half3 diffuse = color.rgb * _Color.rgb;
                half3 normalWS = input.normalWS;

                PixLight light = GetPixLight(0);
                
                evaluateLightSimple(light, diffuse, normalWS, result);
                evaluateIBLSimple(diffuse, normalWS, result);

                result = HDR2LDR(result);
                // result = half3(1,0,0);

                color.a = smoothstep(0,0.5,color.a);
                // color.a = 0;
                return half4(result, color.a);
            }
            ENDHLSL
        }
    }

    CustomEditor "SShaderGUI"
}
