
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
            #pragma multi_compile FORWARD_PIPELINE DEFERRED_PIPELINE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/fullscreen.hlsl"
            #include "lib/common.hlsl"

            TEXTURE2D(_PixColorTex_Front);SAMPLER(sampler_PixColorTex_Front);
            TEXTURE2D(_PixMotionVectorTex);SAMPLER(sampler_PixMotionVectorTex);

            half _HistroyWeight;

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                // return half4(0,1,1,1);
                float2 uv = input.uv;

                half2 motionVector =  SAMPLE_TEXTURE2D(_PixMotionVectorTex, sampler_PixMotionVectorTex, uv).xy;
                
                #ifdef DEFERRED_PIPELINE
                motionVector.y = 1-motionVector.y;
                #endif

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
