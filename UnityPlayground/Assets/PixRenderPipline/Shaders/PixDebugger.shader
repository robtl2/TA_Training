Shader "Hidden/Pix/Debugger"
{
    Properties
    {
        _Channel("Channel", Int) = 0
        _Size("Size", Range(0, 1)) = 1
        _DepthScale("DepthScale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref 0
            Comp Equal
        }


        Pass
        {
            Name "PixDebugger"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile PIX_STYLE_PBR PIX_STYLE_NPR
            #pragma multi_compile _ TAA
            #pragma multi_compile _ MOTION_VECTOR_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/light.hlsl"
            #include "lib/sss.hlsl"
            #include "lib/shading.hlsl"
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
                float4 tiled_id : TEXCOORD1;
            };


            int _Channel;
            float _Size;
            half _DepthScale;

            VaryingsDepth vert(AttributesDepth input)
            {
                float2 uv = input.uv;
                uv *= _Size;

                float2 pos = uv*2.0-1.0;
                uv = input.uv;
                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                #endif

                half4 tiled_id = SAMPLE_TEXTURE2D_LOD(_PixTiledID, sampler_PixTiledID, uv, 0);

                VaryingsDepth output;
                output.uv = uv;
                output.tiled_id = tiled_id;
                output.positionCS = float4(pos, 0.0, 1.0);
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                float2 uv = input.uv;
                MaterialData matData = UnpackGBuffer(uv);

                half3 debugColor[12] = {
                    matData.albedo,
                    matData.diffuse,
                    matData.f0,
                    matData.ao.xxx,
                    matData.bentNormal,
                    matData.positionWS,
                    matData.normalWS,
                    matData.normalVS,
                    matData.tangentWS,
                    matData.viewDir,
                    matData.NoV.xxx,
                    matData.depth.xxx,
                };

                half3 rgb = debugColor[_Channel];
                if(_Channel == 11)
                    rgb *= _DepthScale;

                half a = any(matData.albedo > 0);
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
}
