Shader "Pix/DemoGround"
{
    Properties
    {
        Group#Main("Main", Int) = 1
            _Color ("Color[Main]", Color) = (1,1,1,1)
            _ColorMultiply("ColorMultiply[Main]", Float) = 1

            _GrassTex ("Grassex[Main]{sRGB:on}", 2D) = "white" {}
            _SoilTex ("SoilTex[Main]{sRGB:on}", 2D) = "white" {}
            _BrickTex ("BrickTex[Main]{sRGB:on}", 2D) = "white" {}

            _AlphaClip("T(_ALPHATEST_ON)/AlphaClip[Main]", Int) = 0
            _Cutoff("Cutoff[Main,_ALPHATEST_ON]", Range(0, 1)) = 0.5
            _ParamTex("ParamTex[Main]{sRGB:off}", 2D) = "white" {}

        Group#Param("Param", Int) = 1
            _Roughness("V/Roughness:grass(0,1),soil(0,1),brick(0,1)[Param]", Vector)= (0,0,0,0)

        Group#Nor("Normal[!_ShadingModel_0]", Int) = 1
            _NormalTex("NormalTex[Nor,!_ShadingModel_0]{type:Normal}", 2D) = "bump" {}
            _NormalIntensity("Intensity[Nor,!_ShadingModel_0]", Range(0,2)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest Equal
        Cull Back

        // 0 forward
        Pass
        {
            ZWrite Off
            ZTest Equal
            Cull [_CullMode]

            Name "PixForward"
            Tags { "LightMode" = "PixForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ TAA
            #pragma multi_compile _ FOG
            #pragma shader_feature PIX_STYLE_PBR PIX_STYLE_NPR 
            #pragma shader_feature EXPORT_TANGENT 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #if defined TAA || defined MOTION_BLUR
                #define MOTION_VECTOR_ON
            #endif

            #include "../../Shaders/lib/gbuffer.hlsl"
            #include "../../Shaders/lib/ibl.hlsl"
            #include "../../Shaders/lib/light.hlsl"
            #include "../../Shaders/lib/shading.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                half4 color         : COLOR;
            };


            struct Varying
            {
                float4   positionCS   : SV_POSITION;
                float3x3 tbnVS        : NORMAL;
                float3x3 tbnWS        : TANGENT;
                float4   uv1          : TEXCOORD0;
                float4   uv2          : TEXCOORD1;
                float3   positionWS    : TEXCOORD2;
                float4   color        : COLOR;

            #ifdef MOTION_VECTOR_ON
                float4 prevPosCS    : TEXCOORD4;
            #endif

                float4 screenUV     : TEXCOORD5;
            };

            float4 _Color;
            half _ColorMultiply;
            half _NormalIntensity;
            half3 _Roughness;
            float4x4 _MatrixVP;

            #ifdef MOTION_VECTOR_ON
            float4x4 _PreviousLocalToWorld;
            float4x4 _MatrixVP_Prev;
            #endif

            TEXTURE2D(_GrassTex);SAMPLER(sampler_GrassTex);float4 _GrassTex_ST;
            TEXTURE2D(_SoilTex);SAMPLER(sampler_SoilTex);float4 _SoilTex_ST;
            TEXTURE2D(_BrickTex);SAMPLER(sampler_BrickTex);float4 _BrickTex_ST;

            TEXTURE2D(_NormalTex);SAMPLER(sampler_NormalTex);
            TEXTURE2D(_ParamTex);SAMPLER(sampler_ParamTex);

            Varying vert(Attributes input)
            {
                float4 prevPosOS = float4(input.positionOS.xyzw);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 tangentWS = TransformObjectToWorldNormal(input.tangentOS.xyz)* input.tangentOS.w;
                float3 bitangentWS = cross(normalWS, tangentWS) ;
                float3 positionWS = mul(UNITY_MATRIX_M, input.positionOS).xyz;

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

                half4 color = input.color;

                Varying output;
                output.positionCS = mul(_MatrixVP, float4(positionWS,1));
                output.tbnVS = float3x3(tangentVS, bitangentVS, normalVS);
                output.tbnWS = float3x3(tangentWS, bitangentWS, normalWS);
                output.positionWS = positionWS;

                float4 uv1;
                float4 uv2;
                uv1.xy = input.uv;
                uv1.zw = TRANSFORM_TEX(input.uv, _GrassTex);
                uv2.xy = TRANSFORM_TEX(input.uv, _SoilTex);
                uv2.zw = TRANSFORM_TEX(input.uv, _BrickTex);

                output.uv1 = uv1;
                output.uv2 = uv2;
                output.color = color;

                output.screenUV = output.positionCS;

                #ifdef MOTION_VECTOR_ON
                float4 prevPosWS = mul(_PreviousLocalToWorld, prevPosOS);
                output.prevPosCS = mul(_MatrixVP_Prev, prevPosWS);
                #endif

                return output;
            }

            // MRT 输出
            struct FragmentOutput
            {
                half4 color : SV_Target0;    
                half4 motionVector : SV_Target1; 
            };

            FragmentOutput frag(Varying input, float facing : VFACE)
            {
                half3 grassColor = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, input.uv1.zw).rgb;
                half3 soilColor = SAMPLE_TEXTURE2D(_SoilTex, sampler_SoilTex, input.uv2.xy).rgb*0.7;
                half3 brickColor = SAMPLE_TEXTURE2D(_BrickTex, sampler_BrickTex, input.uv2.zw).rgb;
                half2 mix = input.color.rb;

                half3 color = lerp(soilColor,brickColor,  mix.r);
                color = lerp(color, grassColor, mix.g);
                color *= _Color.rgb;
                color *= _ColorMultiply;

                half3 albedo = color;

                half2 uv = input.uv1.xy;

                half roughness = lerp(_Roughness.g, _Roughness.b, mix.r);
                roughness = lerp(roughness, _Roughness.r, mix.g);

                half3 ndcPos = input.screenUV.xyz/input.screenUV.w;
                half2 screenUV = ndcPos.xy*0.5+0.5;
                screenUV.y = 1-screenUV.y;
                half ndcDepth = ndcPos.z;

                #if _ALPHATEST_ON
                    TestAlpha(color.a, _Cutoff, screenUV);
                #endif

                float3 normal = normalize(input.tbnWS[2]);
                float3 tangent = input.tbnWS[0];
                float3 bitangent = input.tbnWS[1];

                half metallic = 0;
                half perceptualRoughness = max(MIN_PERCEPTUAL_ROUGHNESS, roughness);
                roughness = perceptualRoughness*perceptualRoughness;

                half4 bentNormal = half4(normal, 1);
                float2 motionVector = float2(0, 0);

                #ifdef MOTION_VECTOR_ON
                float2 preNdcPos = input.prevPosCS.xy / input.prevPosCS.w;
                float2 preScreenUV = preNdcPos * 0.5 + 0.5;
                preScreenUV.y = 1-preScreenUV.y;
                motionVector = preScreenUV - screenUV;
                #endif

                half ior = 2;
                half reflectance = iorToF0(max(1.0, ior), 1.0);
                half3 f0 = computeF0(albedo, metallic, reflectance);

                half3 positionWS = input.positionWS.xyz;
                half3 cameraPos = _WorldSpaceCameraPos;
                half3 viewDir = cameraPos - positionWS;
                half depth = length(viewDir);
                viewDir /= depth;

                half3 normalVS = input.tbnVS[2];
                normalVS.z = max(0.01, normalVS.z);
                half NoV = normalVS.z;
                half fresnel = 1-NoV;
                fresnel = pow5(fresnel);

                half anisotropy = 0;

                PixSSSProfile sssProfile = GetPixSSSProfile(0);

                half2 downSampleJitter = hash22(screenUV)*_PixDownSampling_TexelSize;
                half3 downSampleColor = SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, screenUV+downSampleJitter);
                half sunVolume = downSampleColor.r;
                half ssao = downSampleColor.g;
                half4 shadow = DecodeShadow(downSampleColor.b);
                half shadows[4] = {shadow.x, shadow.y, shadow.z, shadow.w};

                GBufferData gbufferData;
                    gbufferData.shadingModel = 1;
                    gbufferData.albedo = albedo;
                    gbufferData.metallic = metallic;
                    gbufferData.roughness = roughness;
                    gbufferData.perceptualRoughness = perceptualRoughness;
                    gbufferData.anisotropy = anisotropy;
                    gbufferData.diffuse = computeDiffuseColor(albedo, metallic);
                    gbufferData.f0 = f0;
                    gbufferData.ao = bentNormal.a;
                    gbufferData.bentNormal = bentNormal.xyz;
                    
                    gbufferData.positionWS = positionWS;
                    gbufferData.normalWS = normal;
                    gbufferData.tangentWS = input.tbnWS[0];
                    gbufferData.bitangentWS = input.tbnWS[1];

                    gbufferData.normalVS = normalVS;
                    gbufferData.viewDir = viewDir;
                    gbufferData.reflectDir = reflect(-viewDir, gbufferData.normalWS);
                    gbufferData.NoV = NoV;
                    gbufferData.fresnel = lerp(f0, 0.95, -roughness*fresnel + fresnel); 

                    gbufferData.ndcDepth = ndcDepth;
                    gbufferData.depth = depth;
                    gbufferData.viewToWorld = input.tbnVS;

                    gbufferData.motionVector = motionVector;
                    gbufferData.sssProfile = sssProfile;

                    gbufferData.sunVolume = sunVolume;
                    gbufferData.ssao = ssao;
                    gbufferData.shadows = shadows;

                    #ifdef DEBUG_LIGHT
                    gbufferData.albedo = _DebugBrightness;
                    gbufferData.diffuse = _DebugBrightness;
                    #endif

                // return half4(gbufferData.ssao.xxx, 1);
                //----------------shading
                half3 result = half3(0, 0, 0);

                if(gbufferData.shadingModel == SHADING_MODEL_UNLIT){
                    result = gbufferData.albedo;
                }else{
                    evaluateIBL(gbufferData, screenUV, result);
                
                    if(PIX_LIGHT_COUNT > 0){
                        [loop]
                        for(int i = 0;i<PIX_LIGHT_COUNT;i++)
                        {
                            PixLight light = GetPixLight(i);

                            if(light.enabled)
                                evaluateLight(light, gbufferData, screenUV, result);
                        }
                    }

                    #ifdef FOG
                    evaluateFog(gbufferData, result);
                    #endif
                }

                half3 ldr = HDR2LDR(result);

                FragmentOutput output;
                output.color = half4(ldr,1);
                output.motionVector = half4(motionVector*0.5+0.5,0,0);
                
                return output;
                
            }
            ENDHLSL
        }

        // 1 gbuffer
        Pass
        {
            Name "PixGBuffer"
            Tags { "LightMode" = "PixGBuffer" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ TAA
            #pragma shader_feature PIX_STYLE_PBR PIX_STYLE_NPR 
            #pragma shader_feature EXPORT_TANGENT 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #if defined TAA || defined MOTION_BLUR
                #define MOTION_VECTOR_ON
            #endif

            #include "../../Shaders/lib/light.hlsl"
            #include "../../Shaders/lib/gbuffer.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float4 color        : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varying
            {
                float4 positionCS   : SV_POSITION;
                float3 normalVS     : NORMAL;
                float3 tangentVS    : TANGENT;
                float4 uv1          : TEXCOORD0;
                float4 uv2          : TEXCOORD1;
                float3 tangentWS    : TEXCOORD2;
                float3 bitangentVS  : TEXCOORD3;
                float4 color        : COLOR;

            #ifdef MOTION_VECTOR_ON
                float4 prevPosCS    : TEXCOORD4;
            #endif

                float4 screenUV     : TEXCOORD5;
            
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _Color;
            half _ColorMultiply;
            half _NormalIntensity;
            half3 _Roughness;
            float4x4 _MatrixVP;

            #ifdef MOTION_VECTOR_ON
            float4x4 _PreviousLocalToWorld;
            float4x4 _MatrixVP_Prev;
            float4x4 _CurrentLocalToWorld;
            #endif

            TEXTURE2D(_GrassTex);SAMPLER(sampler_GrassTex);float4 _GrassTex_ST;
            TEXTURE2D(_SoilTex);SAMPLER(sampler_SoilTex);float4 _SoilTex_ST;
            TEXTURE2D(_BrickTex);SAMPLER(sampler_BrickTex);float4 _BrickTex_ST;

            TEXTURE2D(_NormalTex);SAMPLER(sampler_NormalTex);
            TEXTURE2D(_ParamTex);SAMPLER(sampler_ParamTex);

            Varying vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 prevPosOS = float4(input.positionOS.xyzw);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 tangentWS = TransformObjectToWorldNormal(input.tangentOS.xyz)* input.tangentOS.w;
                float3 bitangentWS = cross(normalWS, tangentWS) ;
                float3 positionWS = mul(UNITY_MATRIX_M, input.positionOS).xyz;

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

                half4 color = input.color;

                Varying output;
                output.positionCS = mul(_MatrixVP, float4(positionWS,1));
                output.normalVS = normalVS;
                output.tangentVS = tangentVS;
                output.bitangentVS = bitangentVS;

                float4 uv1;
                float4 uv2;
                uv1.xy = input.uv;
                uv1.zw = TRANSFORM_TEX(input.uv, _GrassTex);
                uv2.xy = TRANSFORM_TEX(input.uv, _SoilTex);
                uv2.zw = TRANSFORM_TEX(input.uv, _BrickTex);

                output.uv1 = uv1;
                output.uv2 = uv2;
                output.tangentWS = tangentWS;
                output.color = color;

                #ifdef MOTION_VECTOR_ON
                float4 prevPosWS = mul(_PreviousLocalToWorld, prevPosOS);
                output.prevPosCS = mul(_MatrixVP_Prev, prevPosWS);
                output.screenUV = output.positionCS;
                #endif

                UNITY_TRANSFER_INSTANCE_ID(input, output);
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
                UNITY_SETUP_INSTANCE_ID(input);
                // half4 color = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, input.uv) * _Color;
                half3 grassColor = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, input.uv1.zw).rgb;
                half3 soilColor = SAMPLE_TEXTURE2D(_SoilTex, sampler_SoilTex, input.uv2.xy).rgb*0.7;
                half3 brickColor = SAMPLE_TEXTURE2D(_BrickTex, sampler_BrickTex, input.uv2.zw).rgb;
                half2 mix = input.color.rb;

                half3 color = lerp(soilColor,brickColor,  mix.r);
                color = lerp(color, grassColor, mix.g);

                half roughness = lerp(_Roughness.g, _Roughness.b, mix.r);
                roughness = lerp(roughness, _Roughness.r, mix.g);

                color *= _Color.rgb;
                color *= _ColorMultiply;

                half2 screenUV = input.screenUV.xy/input.screenUV.w;
                screenUV = screenUV*0.5+0.5;

                half2 uv = input.uv1.xy;

                float3 normal = normalize(input.normalVS);

                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, uv));
                float3x3 TBN = float3x3(input.tangentVS, input.bitangentVS, input.normalVS);
                normal = lerp(normal, normalize(mul(normalTS, TBN)), _NormalIntensity);

                

                half4 bentNormal = half4(normal, 1);
                
                #ifdef ENABLE_BENTNORMAL
                bentNormal = input.bentNormalVS;
                #endif

                float2 motionVector = float2(0, 0);

                #ifdef MOTION_VECTOR_ON
                float2 preNdcPos = input.prevPosCS.xy / input.prevPosCS.w;
                float2 preScreenUV = preNdcPos * 0.5 + 0.5;
                motionVector = preScreenUV - screenUV;
                #endif

                FragmentOutput output;
                GBuffer gbuffer = PackGBuffer(half4(color, 1), 1, normal, bentNormal, input.tangentWS, 
                    roughness, 0, motionVector, 0, 0);
                
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

        UsePass "Pix/Standard/PixEarlyZ"
        UsePass "Pix/Standard/PixForwardEarlyZ"
        UsePass "Pix/Standard/PixShadowCaster"
    }

    CustomEditor "PixShaderGUI"
}
