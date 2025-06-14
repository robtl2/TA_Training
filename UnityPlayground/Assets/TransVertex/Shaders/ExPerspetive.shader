Shader "Tut/ExPerspective"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _DEBUG_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv        : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                float3 normalWS  : NORMAL;
                float4 debug : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            
            float4x4 _EX_VP;
            half4 _EX_PerspectiveProps;

            bool _Debug = false;


            Varyings vert (Attributes v)
            {
                float factor = _EX_PerspectiveProps.x;
                float offset = _EX_PerspectiveProps.y;
                float smooth = _EX_PerspectiveProps.z;
                float range = _EX_PerspectiveProps.w;

                float4 clipPos = TransformObjectToHClip(v.positionOS.xyz);

                float rawDepth = clipPos.z / clipPos.w;
                float z = LinearEyeDepth(rawDepth, _ZBufferParams);
                z = clamp(z, 0, range);
                z /= range;

                float4 posWorld = mul(GetObjectToWorldMatrix(), v.positionOS);
                float4 exClipPos = mul(_EX_VP, float4(posWorld.xyz, 1));

                float t = 1 - smoothstep(offset, offset+smooth, z);
                t = t*t;
                clipPos = lerp(clipPos, exClipPos, factor*t);

                Varyings o = (Varyings)0;
                o.normalWS = normalize(mul((float3x3)GetObjectToWorldMatrix(), v.normalOS));
                o.positionHCS = clipPos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.debug = float4(z, t, factor, offset);
                return o;
            }

            half4 frag (Varyings i, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                #if _DEBUG_ON
                return i.debug.yyyy;
                #endif

                float3 N = normalize(i.normalWS);
                N = isFrontFace ? N : -N;
                
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float NoL = max(0, dot(N, L));
                float mixVal = smoothstep(0, 0.02, NoL);
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                col.rgb = lerp(col.rgb * 0.5, col.rgb, mixVal);
                return col;
            }
            ENDHLSL
        }
    }
}
