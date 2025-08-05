Shader "Hidden/Pix/Sky"
{
    Properties
    {
        _SkyType ("SkyType", Int) = 0
        _SkyDisplayColor("Color", Color) = (1,1,1,1)
        _BlurLevel ("BlurLevel", Float) = 0
        _FovScale ("FovScale", Float) = 1
        _Dethering("Dethering", Int) = 0
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
            #include "lib/common.hlsl"
            #include "lib/random.hlsl"

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float3 viewDir : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            int _SkyType;
            half4 _SkyColor;
            half4 _SkyDisplayColor;
            int _Dethering;
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
                output.uv = uv;
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                half3 sky = _SkyColor.rgb * _SkyDisplayColor;

                if (_SkyType == 1)
                    sky *= SAMPLE_TEXTURECUBE_LOD(_SkyTex, sampler_SkyTex, input.viewDir, _BlurLevel).rgb;

                half3 ldr = HDR2LDR(sky);

                if(_Dethering>0){
                    ldr *= _Dethering;
                    half3 rgb = floor(ldr);
                    half3 f = frac(ldr);

                    half3 d = dether(input.uv, f);

                    rgb = lerp(rgb-1,rgb,d);
                    rgb/=_Dethering;

                    ldr = rgb;
                }

                return half4(ldr,_SkyDisplayColor.a*0.5);
            }
            ENDHLSL
        }
    }
}
