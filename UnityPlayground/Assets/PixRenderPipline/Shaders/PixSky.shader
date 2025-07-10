Shader "Pix/Sky"
{
    Properties
    {
        _SkyTex ("SkyTex", 2D) = "white" {}
        _RotateSky ("RotateSky", Float) = 0
        _SkyIntensity ("SkyIntensity", Float) = 1
        _SkyFovScale ("SkyFovScale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest LEqual
        Cull Front

        Pass
        {
            Name "PixPost"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
                float4 positionVS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            TEXTURE2D(_SkyTex);SAMPLER(sampler_SkyTex);
            half _RotateSky;
            half _SkyIntensity;
            half _SkyFovScale;

            half fast_atan2(half y, half x)
            {
                const half n1 = 0.97239411f;
                const half n2 = -0.19194795f;
                
                const half epsilon = 1e-5;
                x = abs(x) < epsilon ? sign(x) * epsilon : x;
                
                half abs_z = abs(y / x);
                
                half result = 0.0;
                if (abs_z <= 1.0) {
                    half z2 = abs_z * abs_z;
                    result = ((n2 * z2 + n1) * abs_z);
                } else {
                    half z2 = 1.0 / (abs_z * abs_z);
                    result = HALF_PI - ((n2 * z2 + n1) / abs_z);
                }
                
                if (x < 0.0)
                    result = PI - result;
                
                return (y < 0.0) ? -result : result;
            }

            // TODO: fast_acos
            half2 DirToThetaPhi(float3 dir)
            {
                dir = normalize(dir);
                                
                half theta = acos(dir.y);
                half phi = fast_atan2(dir.x, dir.z);
                
                half2 uv;
                uv.y = theta / PI;
                uv.x = (phi + PI) / (2.0 * PI);

                uv = 1-uv;
                
                return uv;
            }

            // TODO: fast_sincos
            half3 rotate_y(half3 v, half angle)
            {
                half sin_angle;
                half cos_angle;
                sincos(angle, sin_angle, cos_angle);

                half3x3 rotationMatrix = half3x3(
                    half3(cos_angle, 0, -sin_angle),
                    half3(0, 1, 0),
                    half3(sin_angle, 0, cos_angle)    
                );

                return mul(rotationMatrix, v);
            }

            VaryingsDepth vert(AttributesDepth input)
            {
                float2 uv = input.uv;
                float2 pos = uv*2.0-1.0;
                // 这里的远裁面0.001可能因为平台差异有不同的参数，先Mark一下
                float3 positionVS = float3(pos, 0.001);  //留下V空间的坐标后面转世界空间方便些
                float3 positionCS = mul(UNITY_MATRIX_P, positionVS);

                VaryingsDepth output;
                output.positionCS = float4(positionCS, 1.0);
                output.positionVS = float4(positionVS, 1.0);
                output.uv = uv;
                return output;
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                half2 uv = input.uv;
                half4 positionVS = input.positionVS;
                positionVS.xy *= -0.001;
                positionVS.xy *= _SkyFovScale;

                float4 posWorld = mul(UNITY_MATRIX_I_V, positionVS);
                float3 viewDir = normalize(_WorldSpaceCameraPos - posWorld.xyz);

                viewDir = rotate_y(viewDir, _RotateSky);
                half2 thetaPhi = DirToThetaPhi(viewDir);
                half3 sky = SAMPLE_TEXTURE2D_GRAD(_SkyTex, sampler_SkyTex, thetaPhi, ddx(uv), ddy(uv)).rgb;
                sky *= _SkyIntensity;
                
                return half4(sky, 1);
            }
            ENDHLSL
        }
    }
}
