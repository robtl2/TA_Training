Shader "URP/Unlit/billboardGrass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WindNoise ("WindNoise", 2D) = "white" {}
        _WindDirection ("WindDirection", Vector) = (0, 0, 0, 0)
        [Toggle(_ALPHATEST_ON)] _AlphaTestOn ("Alpha Test", Float) = 0

        _TipColor ("TipColor", Color) = (1, 1, 1, 1)
        _RootColor ("RootColor", Color) = (0.2, 0.2, 0.2, 1)

        _RootOffset ("RootOffset", Float) = 0.5
        _TipScale ("TipScale", Float) = 1
        _Thickness ("Thickness", Float) = 0.05
        _Length ("Length", Float) = 0.2

        _Debug ("Debug", Vector) = (100, 0.1, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "BillboardGrassCommon.hlsl"
            

            float4 _TipColor;
            float4 _RootColor;
            float _RootOffset;


            half4 frag (Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
                half t = i.uv.y;
                t /= _RootOffset;
                t = saturate(t);
                half4 rootColor = lerp(_RootColor, _TipColor, t);
                return col * rootColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_depth
            #pragma multi_compile_instancing
            #pragma multi_compile _ _ALPHATEST_ON
            #include "BillboardGrassCommon.hlsl"

            float frag_depth(Varyings i) : SV_Target
            {
            #if defined(_ALPHATEST_ON)
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                clip(col.a - 0.5);
            #endif

                return 0;
            }
        
            ENDHLSL
        }
    }
}
