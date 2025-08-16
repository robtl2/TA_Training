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
            half4 _Scattering;
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

            half3 _SunDirection;
            half4 _SunProps; // r,g,b为颜色，a为大小

            void proceduralSky(float3 dir, inout half3 sky)
            {
                float2 theta_phi = DirToThetaPhi(dir);

                half phi = theta_phi.y*2-1;
                half under = 1-saturate(-phi);
                under = remap01(0.8, 1, under);
                
                phi = 1.0 - saturate(phi); 
                phi = saturate(pow5(phi));

                sky = lerp(sky*0.21,sky*5, phi);
                
                half scatteringOuter = _Scattering.x;
                half scatteringInter = _Scattering.z;
                half scatteringOuterIntensity = _Scattering.y;
                half scatteringInterIntensity = _Scattering.w;
                float NoL = dot(dir, _SunDirection);
                float scatter_outer = sq(remap01(scatteringOuter, 1, NoL))*scatteringOuterIntensity;
                scatter_outer *= scatter_outer;
                
                float scatter_inter = pow5(remap01(scatteringInter, 1.0, NoL))*scatteringInterIntensity;
                scatter_inter *= scatter_inter;

                phi = pow5(phi);
                scatter_outer += phi*scatteringOuterIntensity*0.25;
                scatter_inter += remap01(0.5, 1, sq(phi))*scatteringInterIntensity;

                under = pow5(under);
                scatter_outer *= under*0.8+0.2;
                scatter_inter *= sq(under);

                // scatter_outer*=1.2;

                half3 scatter = lerp(scatter_outer.xxx, 0, _SkyDisplayColor.rgb);
                sky += scatter;

                scatter = lerp(scatter_inter.xxx, 0, _SkyDisplayColor.rgb);
                sky += scatter;

                sky *= lerp(under,half3(0.5,0.55,0.4),0.5);

                if(_SunProps.a>0){
                    half sun = remap01(1-_SunProps.a,1, NoL);
                    sun = sq(sun);

                    sky += _SunProps.rgb*sun;
                }
                
            }

            half4 frag(VaryingsDepth input) : SV_Target
            {
                half3 sky = _SkyColor.rgb * _SkyDisplayColor;

                if (_SkyType == 1)
                    sky *= SAMPLE_TEXTURECUBE_LOD(_SkyTex, sampler_SkyTex, input.viewDir, _BlurLevel).rgb;
                else if (_SkyType == 2)
                    proceduralSky(normalize(input.viewDir), sky);

                half3 ldr = HDR2LDR(sky);

                if(_Dethering>0)
                    ldr = detherColor(ldr, input.uv, _Dethering);

                return half4(ldr,0);
            }
            ENDHLSL
        }
    }
}
