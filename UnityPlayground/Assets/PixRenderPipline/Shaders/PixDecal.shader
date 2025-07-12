// 这个Shader是用来画Decal的
// 演示Stencil的经典用法

Shader "Pix/Decal"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        ZWrite Off
        ZTest LEqual

        // 先把CubeMesh的正面通过Ztest的部分并且Stencil为1的部分找出来+1
        Pass
        {
            Tags { "LightMode" = "PixDecal_Stencil_Front" }

            Cull Back
            ColorMask 0

            Stencil
            {
                Ref 0
                Comp NotEqual
                Pass IncrSat
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_stencil
            #include "lib/decal.hlsl"
            ENDHLSL
        }

        // 再用背面把多余的象素stencil-1去除掉
        // 这样剩下的stencil为2的象素就是Decal需要画的象素
        Pass
        {
            Tags { "LightMode" = "PixDecal_Stencil_Back" }

            Cull Front
            ColorMask 0

            Stencil
            {
                Ref 0
                Comp NotEqual
                Pass DecrSat
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_stencil
            #include "lib/decal.hlsl"
            ENDHLSL
        }

        // 最后把Stencil等于2的象素画出来
        // 画完了再把Stencil变回1，别的Decal可以继续画
        Pass
        {
            Tags { "LightMode" = "PixDecal_Main" }

            Cull Back

            Stencil
            {
                Ref 1
                Comp Less
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_decal
            #include "lib/decal.hlsl"
            ENDHLSL
        }

        
    }
}
