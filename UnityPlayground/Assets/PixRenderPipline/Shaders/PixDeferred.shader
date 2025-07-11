
Shader "Pix/Deferred"
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

        // Stencil
        // {
        //     Ref 1
        //     Comp Equal
        // }
       
        Pass
        {
            Name "PixDeferred"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/gbuffer.hlsl"
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

            TEXTURE2D(_PixTiledID);SAMPLER(sampler_PixTiledID);

            VaryingsDepth vert(AttributesDepth input)
            {
                float2 uv = input.uv;
                float2 pos = uv*2.0-1.0;

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
                MainLight mainLight = GetMainLight();

                half3 N = gbufferData.normalWS;
                half3 L = mainLight.direction;

                half3 NoL = saturate(dot(N, L));

                NoL = smoothstep(0.0, 0.01, NoL);


                half3 lit = mainLight.color * NoL + _PixAmbientLightColor;

                half3 col = gbufferData.albedo * lit;

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
