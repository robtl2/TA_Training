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

        Stencil
        {
            Ref 10
            Comp Less
        }
       
        Pass
        {
            Name "PixPost"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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

            half2 _OutLineDepthNormalThreshold;

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

            half3 Tonemap(half3 x)
            {
                half A = 1.3; 
                half B = 1; 
                half C = 3.3; 
                half D = 0.63; 
                half E = 1.15;
                half F = 4.08; 
                
                return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
            }

            half3 Sharpen(half3 color, half contrast){
                half3 luminanceWeights = half3(0.299, 0.587, 0.114);
                half gray = dot(color.rgb, luminanceWeights);

                half2 dxy = half2(ddx(gray), ddy(gray));
                half edge = dot(dxy,dxy);

                if (edge > 0.01)
                {
                    half factor = (contrast == 0.0f) ? 1.0f : (1.0f + contrast * 4.0f);
                    half midpoint = 0.5f;
                    return (color - midpoint) * factor + midpoint;
                }
                else
                    return color;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                half2 uv = input.uv;
                half4 color = SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv);

                color.rgb = LDR2HDR(color.rgb);

            #if 1
                color.rgb = Sharpen(color.rgb,0.05);
            #endif

            #if 1
                color.rgb = Tonemap(color.rgb);
            #else
                if(any(color>1))color.rgb = half3(1,0,0);
            #endif
                
                return color;
            }
            ENDHLSL
        }
    }
}
