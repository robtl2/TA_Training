// 最后阶段的屏幕滤镜效果

Shader "Pix/Filter"
{
    Properties
    {
        _IsSceneView ("Is Scene View", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest Always
       
        Pass
        {
            Name "PixFilter"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            int _IsSceneView;

            TEXTURE2D(_PixColorTex);SAMPLER(sampler_PixColorTex);

            VaryingsDepth vert(AttributesDepth input)
            {
                VaryingsDepth output;
                float2 uv = input.uv;
                float2 pos = uv*2.0-1.0;
                output.positionCS = float4(pos, 0.0, 1.0);
                
                if (_IsSceneView){
                    uv.y = 1.0 - uv.y;
                }

                output.uv = uv;

                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, input.uv);
                return color;
            }
            ENDHLSL
        }
    }
}
