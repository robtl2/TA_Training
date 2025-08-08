
Shader "Hidden/Pix/TAA"
{
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
            #pragma vertex vertFullScreen
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/fullscreen.hlsl"
            #include "lib/common.hlsl"

            TEXTURE2D(_PixColorTex_Front);SAMPLER(sampler_PixColorTex_Front);
            TEXTURE2D(_PixGBuffer_3);SAMPLER(sampler_PixGBuffer_3);

            half _HistroyWeight;

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                float2 uv = input.uv;

                half2 motionVector =  SAMPLE_TEXTURE2D(_PixGBuffer_3, sampler_PixGBuffer_3, uv).xy;
                motionVector.y = 1-motionVector.y;
                motionVector = motionVector*2-1;

                uv += motionVector;

                if(any(uv<0 || uv>1))
                    return half4(0,0,0,0);
                
                half4 color = SAMPLE_TEXTURE2D(_PixColorTex_Front, sampler_PixColorTex_Front, uv);
                
                return half4(color.rgb,_HistroyWeight);
            }
            ENDHLSL
        }
    }
}
