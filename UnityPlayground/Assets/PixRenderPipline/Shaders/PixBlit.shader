Shader "Hidden/Pix/Blit"
{
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "PixBlitDepth"

            ColorMask RG

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_PixEarlyZDepth);SAMPLER(sampler_PixEarlyZDepth);

            VaryingsDepth vert(AttributesDepth input)
            {
                float2 uv = input.uv;
                float2 pos = uv*2.0-1.0;

                // #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                // #endif

                VaryingsDepth output;
                output.uv = uv;
                output.positionCS = float4(pos, 0.0, 1.0);
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                float depthRaw = SAMPLE_TEXTURE2D(_PixEarlyZDepth, sampler_PixEarlyZDepth, input.uv).r;

                // half2 packed = PackFloatToR8G8(depthRaw);
                return half4(depthRaw.xxx,0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "PixBlitColor"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_PixColorTex);SAMPLER(sampler_PixColorTex);

            VaryingsDepth vert(AttributesDepth input)
            {
                float2 uv = input.uv;
                float2 pos = uv*2.0-1.0;

                // #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                // #endif

                VaryingsDepth output;
                output.uv = uv;
                output.positionCS = float4(pos, 0.0, 1.0);
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, input.uv);

                // half2 packed = PackFloatToR8G8(depthRaw);
                return color;
            }
            ENDHLSL
        }
    }
}
