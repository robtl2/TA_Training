
Shader "Hidden/Pix/TAA"
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
            Blend srcAlpha OneMinusSrcAlpha, Zero One
            Name "PixPost"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "lib/common.hlsl"

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

            TEXTURE2D(_PixColorTex_Front);SAMPLER(sampler_PixColorTex_Front);
            TEXTURE2D(_PixGBuffer_3);SAMPLER(sampler_PixGBuffer_3);

            half _HistroyWeight;

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

            half4 frag(VaryingsDepth input) : SV_Target
            {
                float2 uv = input.uv;

                half2 motionVector =  SAMPLE_TEXTURE2D(_PixGBuffer_3, sampler_PixGBuffer_3, uv).xy;
                motionVector.y = 1-motionVector.y;
                motionVector = motionVector*2-1;

                uv += motionVector;

                if(any(uv<0 || uv>1))
                    return half4(0,0,0,0);
                
                half4 color = SAMPLE_TEXTURE2D(_PixColorTex_Front, sampler_PixColorTex_Front, uv);
                // color.rgb = LDR2HDR(color.rgb);

                half weight = _HistroyWeight;
                
                
                return half4(color.rgb,weight);
            }
            ENDHLSL
        }
    }
}
