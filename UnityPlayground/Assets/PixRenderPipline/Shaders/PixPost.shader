// 最后阶段的屏幕滤镜效果

Shader "Hidden/Pix/Post"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "PixPost"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ PP_SHARPEN
            #pragma multi_compile _ PP_TONEMAPPING

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/common.hlsl"
            #include "lib/gbuffer.hlsl"

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
            half2 _PixColorTex_TexelSize;

            half _SharpenAmount;

            VaryingsDepth vert(AttributesDepth input)
            {
                VaryingsDepth output;
                float2 uv = input.uv;
                float2 pos = uv*2.0-1.0;

                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                #endif
                
                output.positionCS = float4(pos, 0.0, 1.0);
                output.uv = uv;
                return output;
            }

            void Tonemap(inout half3 x)
            {
                half A = 1.3; 
                half B = 1; 
                half C = 3.3; 
                half D = 0.63; 
                half E = 1.15;
                half F = 4.08; 

                half3 rgb_t = ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
                
                x = rgb_t;
            }

            void Sharpen(inout half3 color, half2 uv, half mask)
            {
                // 模拟一个3x3的高斯模糊采样
                float3 blurred = color;

                float2 offset = _PixColorTex_TexelSize;

                blurred += SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv + half2(offset.x, 0)).rgb;
                blurred += SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv + half2(-offset.x,0)).rgb;
                blurred += SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv + half2(0,offset.y)).rgb;
                blurred += SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv + half2(0,-offset.y)).rgb;
                blurred /= 5;

                float3 diff = color - blurred;

                color = color + diff*mask*_SharpenAmount;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                half2 uv = input.uv;
                half4 color = SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv);

                half3 rgb = color.rgb;

                #ifdef PP_SHARPEN
                    Sharpen(rgb, uv, color.a);
                #endif

                rgb = LDR2HDR(rgb);

                #ifdef PP_TONEMAPPING
                    Tonemap(rgb);
                #endif

                #if 0
                    if(any(color>1))color.rgb = half3(1,0,0);
                #endif
                
                return half4(rgb, 1);
            }
            ENDHLSL
        }
    }
}
