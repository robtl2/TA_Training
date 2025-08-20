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
            #pragma multi_compile _ PP_SUN_VOLUME
            #pragma multi_compile _ PP_SHARPEN
            #pragma multi_compile _ PP_BLOOM
            #pragma multi_compile _ PP_TONEMAPPING

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/common.hlsl"
            #include "lib/gbuffer.hlsl"
            #include "lib/light.hlsl"
            #include "lib/random.hlsl"

            TEXTURE2D(_PixColorTex);SAMPLER(sampler_PixColorTex);half2 _PixColorTex_TexelSize;

            half4 _SharpenProps;
            half _Exposure;
            half _Vagnet;


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

            void Vignet(inout half3 color, half2 uv, half intensity){
                uv = uv-0.5;
                half range = length(uv)*2;
                range = 1 - smoothstep(0.5, 2,range);
                range *= range;
                range = lerp(1, range, intensity);
                color *= range;
            }

            #ifdef PP_BLOOM

            TEXTURE2D(_BloomTex);SAMPLER(sampler_BloomTex);float2 _BloomTex_TexelSize;
            half _BloomIntensity;

            void Bloom(inout half3 color, half2 uv)
            {
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

                    weightTotle += weight*4;
                    weight *=0.8;
                }
                bloom /= weightTotle;

                color += bloom * _BloomIntensity;
            }
            #endif

            #ifdef PP_SUN_VOLUME
            // TEXTURE2D(_PixDownSampling);SAMPLER(sampler_PixDownSampling);float2 _PixDownSampling_TexelSize;
            half4 _SunVolume;
            half4 _SunVolumeColor;

            void SunVolume(inout half3 color, half2 screenUV, half mask){
                // volume sun light的计算放下采样里去了
                half3 volume = SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, screenUV).x;
                half m = lerp(mask,1,_SunVolumeColor.a);
                color = lerp(color, _SunVolumeColor.rgb, volume*m);
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

                #ifdef PP_SUN_VOLUME
                    SunVolume(rgb, uv, color.a);
                #endif

                rgb *= _Exposure;

                Vignet(rgb, uv, _Vagnet);

                #ifdef PP_TONEMAPPING
                    Tonemap(rgb);
                #endif
                
                return half4(rgb, 1);
            }
            ENDHLSL
        }
    }
}
