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

            float3 Tonemap(float3 x)
            {
                float A = 1.3; 
                float B = 1; 
                float C = 3.3; 
                float D = 0.63; 
                float E = 1.15;
                float F = 4.08; 
                
                return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                float2 uv = input.uv;
                half4 color = SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv);

                color.rgb = LDR2HDR(color.rgb, color.a*2);

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
