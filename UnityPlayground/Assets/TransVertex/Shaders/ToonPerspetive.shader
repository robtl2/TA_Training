Shader "Tut/ToonPerspective"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Pivot ("Pivot", Vector) = (0, 0, 0, 0)
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
            #pragma shader_feature_local _PIVOT_ON

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
                float3 normalWS  : TEXCOORD1;
                float4 debug     : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Pivot;

            Varyings vert (Attributes v)
            {
            #ifdef _PIVOT_ON
                float4 pivot = _Pivot;
                float4 worldPos = mul(GetObjectToWorldMatrix(), v.positionOS);
                float4 viewPos = mul(GetWorldToViewMatrix(), worldPos);
                float4 viewPivot = mul(GetWorldToViewMatrix(), pivot);
                viewPos.xy -= viewPivot.xy;
                float4 clipPos = mul(GetViewToHClipMatrix(), viewPos);
                float4 clipPivot = mul(GetViewToHClipMatrix(), viewPivot);
                viewPivot.xy = 0;
                float4 clipViewPivot = mul(GetViewToHClipMatrix(), viewPivot);
                float2 offset = clipPivot.xy/clipPivot.w - clipViewPivot.xy/clipViewPivot.w;
                clipPos.xy += offset*clipPos.w;
            #else
                float4 clipPos = TransformObjectToHClip(v.positionOS.xyz);
            #endif

                Varyings o = (Varyings)0;
                o.normalWS = normalize(mul((float3x3)GetObjectToWorldMatrix(), v.normalOS));
                o.positionHCS = clipPos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.debug = float4(_Pivot.xyz, 1);
                return o;
            }

            half4 frag (Varyings i, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
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
