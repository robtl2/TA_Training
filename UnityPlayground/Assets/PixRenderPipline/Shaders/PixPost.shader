// 最后阶段的屏幕滤镜效果

Shader "Hidden/Pix/Post"
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
        Cull Off

        Stencil
        {
            Ref 10
            Comp Less
        }
       
        Pass
        {
            Name "PixPost"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/gbuffer.hlsl"

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

            TEXTURE2D(_PixColorTex);SAMPLER(sampler_PixColorTex);
            half2 _PixColorTex_TexelSize;

            half2 _OutLineDepthNormalThreshold;

            VaryingsDepth vert(AttributesDepth input)
            {
                VaryingsDepth output;
                float2 uv = input.uv;
                float2 pos = uv*2.0-1.0;

                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                #endif
                
                output.positionCS = float4(pos, 0.0, 1.0);
                output.uv = uv;
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                float2 uv = input.uv;
                half4 color = SAMPLE_TEXTURE2D(_PixColorTex, sampler_PixColorTex, uv);
                GBufferData gbufferData = UnpackGBuffer(uv);
                half depth = gbufferData.depth;
                half3 normalWS = gbufferData.normalWS;


                // 采样周围像素
                half2 offset = _PixColorTex_TexelSize.xy;
                half edge = 1;

                // 四方向采样
                // [unroll]
                // for (int i = 0; i < 4; i++)
                // {
                //     float2 dir = 0;
                //     if (i == 0) dir = float2(offset.x, 0);
                //     if (i == 1) dir = float2(-offset.x, 0);
                //     if (i == 2) dir = float2(0, offset.y);
                //     if (i == 3) dir = float2(0, -offset.y);

                //     GBufferData neighbor = UnpackGBuffer(uv + dir);

                //     // 深度差异
                //     float depthDiff = abs(depth - neighbor.depth);
                //     if (depthDiff > _OutLineDepthNormalThreshold.x)
                //         edge = 0;

                //     // 法线差异
                //     float normalDiff = 1 - dot(normalWS, neighbor.normalWS);
                //     if (normalDiff > _OutLineDepthNormalThreshold.y)
                //         edge = 0;
                // }



                return color*edge;
            }
            ENDHLSL
        }
    }
}
