Shader "Hidden/Pix/Blit"
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
            Name "PixBlitDepth"

            ColorMask RG

            HLSLPROGRAM
            #pragma vertex vertFullScreen
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/fullscreen.hlsl"

            TEXTURE2D(_PixEarlyZDepth);SAMPLER(sampler_PixEarlyZDepth);

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                float depthRaw = SAMPLE_TEXTURE2D(_PixEarlyZDepth, sampler_PixEarlyZDepth, input.uv).r;
                return half4(depthRaw.xxx,0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "PixBlitColor"

            HLSLPROGRAM
            #pragma vertex vertFullScreen
            #pragma fragment fragFullScreen
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/fullscreen.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "PixBlitBloomVertical"

            HLSLPROGRAM
            #pragma vertex vertBloomVertical
            #pragma fragment fragBloomVertical
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/bloom.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "PixBlitBloomHorizontal"

            HLSLPROGRAM
            #pragma vertex vertBloomHorizontal
            #pragma fragment fragBloomHorizontal
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/bloom.hlsl"
            ENDHLSL
        }
    }
    
}
