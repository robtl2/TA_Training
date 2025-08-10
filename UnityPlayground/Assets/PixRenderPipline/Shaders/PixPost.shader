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
            #pragma vertex vertFullScreen
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/fullscreen.hlsl"

            #pragma multi_compile _ TAA

            #pragma multi_compile _ PP_SHARPEN
            #pragma multi_compile _ PP_BLOOM
            #pragma multi_compile _ PP_TONEMAPPING

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/common.hlsl"
            #include "lib/gbuffer.hlsl"


            TEXTURE2D(_PixColorTex);SAMPLER(sampler_PixColorTex);
            half2 _PixColorTex_TexelSize;

            half4 _SharpenProps;
            half _Exposure;

            #ifdef PP_BLOOM
                TEXTURE2D(_BloomTex);SAMPLER(sampler_BloomTex);float2 _BloomTex_TexelSize;
                half _BloomIntensity;
            #endif

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
                half d = Luminance(diff);
                half b = Luminance(blurred);
                half m = d*(b<_SharpenProps.y);
                m = saturate(1-m*30);
                m = sq(m);

                diff *= _SharpenProps.x;
                color = diff*mask*m + color;
            }

            #ifdef PP_BLOOM
            void Bloom(inout half3 color, half2 uv)
            {
                // #ifdef TAA
                // uv += _TAA_Jitter*_BloomTex_TexelSize;
                // #endif

                half3 bloom = 0;
                int mipCount = 4;
                half weightTotle = 0;
                half weight = 1;
                for(int i=0;i<mipCount;i++){

                    half2 diagonal = _BloomTex_TexelSize * 3; 
                    bloom += SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, uv).xyz*weight;
                    bloom += SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, uv + half2(-diagonal.x, diagonal.y)).xyz*weight;
                    bloom += SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, uv + half2(diagonal.x, -diagonal.y)).xyz*weight;
                    bloom += SAMPLE_TEXTURE2D(_BloomTex, sampler_BloomTex, uv + half2(-diagonal.x, -diagonal.y)).xyz*weight;

                    // bloom += SAMPLE_TEXTURE2D_LOD(_BloomTex, sampler_BloomTex, uv, i).rgb*weight;
                    weightTotle += weight*4;
                    weight *=0.8;
                }
                bloom /= weightTotle;

                color += bloom * _BloomIntensity;
            }
            #endif

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                half2 uv = input.uv;
                half4 color = SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv);

                half3 rgb = color.rgb;

                #ifdef PP_SHARPEN
                    Sharpen(rgb, uv, color.a);
                #endif

                #ifdef PP_BLOOM
                    Bloom(rgb, uv);
                #endif

                rgb = LDR2HDR(rgb);
                rgb *= _Exposure;

                #ifdef PP_TONEMAPPING
                    Tonemap(rgb);
                #endif

                // rgb = detherColor(rgb, uv,64);

                // #if 0
                //     if(any(color>1))color.rgb = half3(1,0,0);
                // #endif
                
                return half4(rgb, 1);
            }
            ENDHLSL
        }
    }
}
