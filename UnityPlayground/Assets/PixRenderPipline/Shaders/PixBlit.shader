Shader "Hidden/Pix/Blit"
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

        Pass
        {
            Name "PixBlitDepth"

            ColorMask RG

            HLSLPROGRAM
            #pragma vertex vertFullScreen
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/fullscreen.hlsl"

            TEXTURE2D(_PixEarlyZDepth);SAMPLER(sampler_PixEarlyZDepth);

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                float depthRaw = SAMPLE_TEXTURE2D(_PixEarlyZDepth, sampler_PixEarlyZDepth, input.uv).r;
                return half4(depthRaw.xxx,0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "PixBlitColorForTAA"

            HLSLPROGRAM
            #pragma vertex vertFullScreen
            #pragma fragment fragFullScreen
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/fullscreen.hlsl"

            // TEXTURE2D(_PixOpaqueTex);SAMPLER(sampler_PixOpaqueTex);

            // half4 frag(VarFullScreenQuad input) : SV_Target
            // {
            //     half4 color = SAMPLE_TEXTURE2D(_PixOpaqueTex, sampler_PixOpaqueTex, input.uv);
            //     return color;
            // }
            ENDHLSL

            // HLSLPROGRAM
            // #pragma vertex vertFullScreen
            // #pragma fragment frag
            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #include "lib/vert.hlsl"

            // TEXTURE2D(_PixOpaqueTex);SAMPLER(sampler_PixOpaqueTex);

            // half4 frag(VarFullScreenQuad input) : SV_Target
            // {
            //     half4 color = SAMPLE_TEXTURE2D(_PixOpaqueTex, sampler_PixOpaqueTex, input.uv);
            //     return color;
            // }
            // ENDHLSL
        }

        Pass
        {
            Name "PixBlitBloomVertical"

            HLSLPROGRAM
            #pragma vertex vertBloomVertical
            #pragma fragment fragBloomVertical
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/bloom.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "PixBlitBloomHorizontal"

            HLSLPROGRAM
            #pragma vertex vertBloomHorizontal
            #pragma fragment fragBloomHorizontal
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/bloom.hlsl"
            ENDHLSL
        }



        //     HLSLPROGRAM
        //     #pragma vertex vert
        //     #pragma fragment frag
        //     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        //     struct AttributesDepth
        //     {
        //         float4 positionOS : POSITION;
        //         float2 uv : TEXCOORD0;
        //     };

        //     struct VaryingsDepth
        //     {
        //         float4 positionCS : SV_POSITION;
        //         float2 uv : TEXCOORD0;
        //     };

        //     TEXTURE2D(_PixColorTex);SAMPLER(sampler_PixColorTex);half2 _PixColorTex_TexelSize;
        //     half _Bloom_Threshold;

        //     VaryingsDepth vert(AttributesDepth input)
        //     {
        //         float2 uv = input.uv;
        //         float2 pos = uv*2.0-1.0;

        //         // #if UNITY_UV_STARTS_AT_TOP
        //         uv.y = 1.0 - uv.y;
        //         // #endif

        //         VaryingsDepth output;
        //         output.uv = uv;
        //         output.positionCS = float4(pos, 0.0, 1.0);
        //         return output;
        //     }

        //     half4 frag(VaryingsDepth input) : SV_Target
        //     {
        //         half2 uv = input.uv;
        //         half2 offset = _PixColorTex_TexelSize*0.75;
        //         half2 diagonal = offset * 1.4142; // sqrt(2) 让对角线采样覆盖相同的范围
        //         half3 bloom = SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv).xyz;
        //         // bloom += SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv + half2(-diagonal.x, diagonal.y)).xyz;
        //         // bloom += SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv + half2(diagonal.x, -diagonal.y)).xyz;
        //         // bloom += SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv + half2(-diagonal.x, -diagonal.y)).xyz;
        //         // bloom = bloom * 0.25;

        //         return half4(bloom, 1);
        //     }
        //     ENDHLSL
        // }




    }

    
}
