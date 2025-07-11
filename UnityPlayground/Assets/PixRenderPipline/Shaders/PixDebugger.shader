Shader "Pix/Debugger"
{
    Properties
    {
        _Channel("Channel", Int) = 0
        _Size("Size", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "PixDebugger"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/light.hlsl"

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
                GBufferData gbufferData = UnpackGBuffer(uv);

                half2 screenUV = posWorldToScreenUV(gbufferData.positionWS);

                half3 test = sampleDepth(screenUV);

                half3 debugColor[7] = {
                    gbufferData.albedo,
                    gbufferData.positionWS,
                    gbufferData.normalWS,
                    gbufferData.normalVS,
                    gbufferData.NoV.xxx,
                    gbufferData.depth.xxx,
                    test,
                };
                
                return half4(debugColor[_Channel], gbufferData.alpha);
            }
            ENDHLSL
        }
    }
}
