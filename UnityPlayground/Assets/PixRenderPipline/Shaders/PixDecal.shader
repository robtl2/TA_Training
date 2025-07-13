// 这个Shader是用来画Decal的
// 演示Stencil的经典用法

Shader "Pix/Decal"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
        _ShadingModel ("Shading Model", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        ZWrite Off
        ZTest LEqual

        // 先把CubeMesh的正面通过Ztest的部分并且Stencil不为0的部分找出来+1(0是画天空盒的区域)
        Pass
        {
            Name "PixDecal_Stencil_Front"

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
            #pragma multi_compile_instancing
            #include "lib/decal.hlsl"
            ENDHLSL
        }

        // 再用背面把多余的象素stencil-1去除掉
        // 这样剩下的stencil比1大的象素就是Decal需要画的象素
        Pass
        {
            Name "PixDecal_Stencil_Back"

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
            #pragma multi_compile_instancing
            #include "lib/decal.hlsl"
            ENDHLSL
        }

        // 最后把Stencil比1大的象素画出来(Comp Less是问的ref 1是不是比stencilBuff中的值小)
        Pass
        {
            Name "PixDecal_Main"

            ZTest Always //到这里就只需要Stencil Test了
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 1
                Comp Less
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_decal
            #pragma multi_compile_instancing
            #include "lib/decal.hlsl"
            ENDHLSL
        }

        
    }
}
