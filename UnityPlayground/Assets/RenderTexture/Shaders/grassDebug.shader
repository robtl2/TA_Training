Shader "Universal Render Pipeline/grassDebug"
{
    Properties
    {
        _Alpha("Alpha", Range(0, 1)) = 1
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
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Front

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
            
            TEXTURE2D(_GrassCurrRT);
            SAMPLER(sampler_GrassCurrRT);
            float _Alpha;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float2 pos = input.uv*0.2;
                // 获取当前屏幕的宽高比
                float aspect = _ScreenParams.x / _ScreenParams.y;
                pos.y *= aspect;
                pos = pos*2-1;
                output.positionHCS = float4(pos,0,1);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half2 uv = input.uv;
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1-uv.y;
                #endif  
                half4 col_curr = SAMPLE_TEXTURE2D(_GrassCurrRT, sampler_GrassCurrRT, uv);
                half2 dir = col_curr.rg;
                // return col_curr;
                return half4(dir,0, _Alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
