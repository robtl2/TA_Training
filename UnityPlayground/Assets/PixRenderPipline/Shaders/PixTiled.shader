// 最后阶段的屏幕滤镜效果

Shader "Hidden/Pix/Tiled"
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
            Name "PixTiled"

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

            TEXTURE2D(_PixGBuffer0);SAMPLER(sampler_PixGBuffer0);

            VaryingsDepth vert(AttributesDepth input)
            {
                VaryingsDepth output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_PixGBuffer0, sampler_PixGBuffer0, input.uv);
                return color;
            }
            ENDHLSL
        }
    }
}
