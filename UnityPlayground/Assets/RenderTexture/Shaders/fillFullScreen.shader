Shader "Universal Render Pipeline/fillFullScreen"
{
    Properties
    {
        _FadeOut ("FadeOut", Float) = 1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }
        LOD 100
        
        // Blend One OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Back

        Pass
        {
            Name "ForwardLit"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
            };
            
            TEXTURE2D(_GrassPrevRT);
            SAMPLER(sampler_GrassPrevRT);

            float _FadeOut;
            float _DeltaTime;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float2 pos = input.uv;
                pos = pos*2-1;
                output.positionHCS = float4(pos,0,1);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // TODO: 这里的uv应该用上一帧和当前帧aabb的差距计算出偏移
                half2 uv = input.uv;
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1-uv.y;
                #endif  

                half2 col = SAMPLE_TEXTURE2D(_GrassPrevRT, sampler_GrassPrevRT, uv).rg;
                half2 defautDir = float2(0.5,0.5);
                half delta = _DeltaTime*_FadeOut;
                col = lerp(defautDir,col, 1-delta);
                return half4(col,1,1);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
