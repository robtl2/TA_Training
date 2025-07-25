Shader "Hidden/Pix/Sky"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _SkyType ("SkyType", Int) = 0
        _SkyTex ("SkyTex", Cube) = "white" {}
        _BlurLevel ("BlurLevel", Float) = 0
        _RotateSky ("RotateSky", Float) = 0
        _FovScale ("FovScale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest Always
        Cull Front

        Stencil
        {
            Ref 0
            Comp Equal
        }

        Pass
        {
            Name "PixPost"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/comman.hlsl"

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float3 viewDir : TEXCOORD0;
            };

            int _SkyType;
            half4 _Color;
            half _RotateSky;
            half _FovScale;
            half _BlurLevel;
            TEXTURECUBE(_SkyTex);SAMPLER(sampler_SkyTex);

            VaryingsDepth vert(AttributesDepth input)
            {
                float2 uv = input.uv;
                float2 pos = uv*2.0-1.0;
                // 这里的远裁面0.001可能因为平台差异有不同的参数，先Mark一下
                float3 positionVS = float3(pos, 0.001);  //留下V空间的坐标后面转世界空间方便些
                float3 positionCS = mul(UNITY_MATRIX_P, positionVS);

                positionVS.xy *= -positionVS.z;
                positionVS.xy *= _FovScale;
                float4 posWorld = mul(UNITY_MATRIX_I_V, float4(positionVS, 1.0));
                float3 viewDir = normalize(_WorldSpaceCameraPos - posWorld.xyz);
                viewDir = rotate_y(viewDir, _RotateSky);

                VaryingsDepth output;
                output.positionCS = float4(positionCS, 1.0);
                output.viewDir = viewDir;
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                half3 sky = _Color.rgb;

                if (_SkyType == 1)
                    sky *= SAMPLE_TEXTURECUBE_LOD(_SkyTex, sampler_SkyTex, input.viewDir, _BlurLevel).rgb;
                
                return half4(sky, 1);
            }
            ENDHLSL
        }
    }
}
