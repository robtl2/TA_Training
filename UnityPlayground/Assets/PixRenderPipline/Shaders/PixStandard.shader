Shader "Pix/Standard"
{
    Properties
    {
        _ShadingModel ("E/ShadingModel:Unlit,Lit", Int) = 0

        Group#Main("Main", Int) = 1
            _Color ("Color[Main]", Color) = (1,1,1,1)
            _MainTex ("MainTex[Main]", 2D) = "white" {}

        Group#Lit("Lit[_ShadingModel_1]", Int) = 1
            _Specular("Specular[Lit,_ShadingModel_1]", Range(0,1)) = 0
            _Rim("Rim[Lit,_ShadingModel_1]", Range(0,1)) = 0


    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest Equal

        Pass
        {
            Name "PixGBuffer"
            Tags { "LightMode" = "PixGBuffer" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/gbuffer.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varying
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float2 normalVS     : TEXCOORD1;
            };

            float4 _Color;
            int _ShadingModel;

            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            Varying vert(Attributes input)
            {
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = mul(unity_ObjectToWorld, input.positionOS).xyz;

                // 计算视线空间下的法线
                float3 cameraPos = _WorldSpaceCameraPos;
                float3 viewDir = normalize(cameraPos - positionWS);
                float3 viewUp = float3(0.0, 1.0, 0.0);
                float3 up = mul((float3x3)UNITY_MATRIX_I_V, viewUp);
                float3 right = normalize(cross(viewDir, up));
                up = cross(right, viewDir);
                float x = dot(normalWS, right);
                float y = dot(normalWS, up);

                Varying output;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalVS = float2(x, y)*0.5+0.5;
                output.normalWS = normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            // MRT 输出
            struct FragmentOutput
            {
                half4 gbuffer_0 : SV_Target0;    
                half4 gbuffer_1 : SV_Target1; 
            };

            FragmentOutput frag(Varying input)
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                FragmentOutput output;
                GBuffer gbuffer = PackGBuffer(color, _ShadingModel, input.normalVS);
                output.gbuffer_0 = gbuffer.gbuffer_0;
                output.gbuffer_1 = gbuffer.gbuffer_1;
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
                Ref 255
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
            #if _ALPHATEST_ON
                float2 uv : TEXCOORD0;
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

            VaryingsDepth vert(AttributesDepth input)
            {
                VaryingsDepth output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
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
    }

    CustomEditor "SShaderGUI"
}
