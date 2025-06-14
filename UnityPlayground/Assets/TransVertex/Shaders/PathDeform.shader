Shader "Tut/PathDeform"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Pivot ("Pivot", Vector) = (0, 0, 0, 0)
        _Progress ("Progress", Float) = 0
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
            #include "bezier.hlsl"

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
            float _Progress;
            float4 _Scale;

            float3 _PathKnots[MAX_KNOT_COUNT];
            float3 _PathNormals[MAX_KNOT_COUNT];
            float3 _PathTangentsIn[MAX_KNOT_COUNT];
            float3 _PathTangentsOut[MAX_KNOT_COUNT];
            int _PathKnotCount;
            float _PathLength;

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                o.debug = float4(_Pivot.xyz, 0);

            #ifdef _PIVOT_ON
                float4 worldPos = mul(GetObjectToWorldMatrix(), v.positionOS);
                worldPos.xyz -= _Pivot.xyz;
                float p = worldPos.y*_Scale.y/_PathLength + _Progress;
                p = clamp(p, 0, 0.999f);

                float3 position, upVector, tangent;
                EvaluateSpline(p, _PathKnots, _PathNormals, _PathTangentsIn, _PathTangentsOut, _PathKnotCount, position, upVector, tangent);

                float3 T = tangent;
                float3 N = upVector;
                float3 B = normalize(cross(N, T));

                float4x4 M = float4x4(
                    float4(N, 0),
                    float4(T, 0), //角色的y方向与T方向一致
                    float4(B, 0),
                    float4(position, 1)
                );  
                M = transpose(M);//构造时是用的行主序，所以需要转置
                
                worldPos.xz *= _Scale.x;  
                float3 pos = float3(worldPos.x, 0, worldPos.z);
                float3 posLocal = mul(M, float4(pos, 1)).xyz;

                float4 clipPos = TransformObjectToHClip(posLocal);

                o.normalWS = normalize(mul((float3x3)M, v.normalOS));
                o.debug.a = p;
            #else
                float4 clipPos = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = normalize(mul((float3x3)GetObjectToWorldMatrix(), v.normalOS));
            #endif
                
                o.positionHCS = clipPos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                return o;
            }

            half4 frag (Varyings i, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                clip(0.998 - i.debug.a);

                float3 N = normalize(i.normalWS);
                N = isFrontFace ? N : -N;
                
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float NoL = max(0, dot(N, L));
                float mixVal = smoothstep(0, 0.02, NoL);
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                col.rgb = lerp(col.rgb * 0.5, col.rgb, mixVal);

                float p = smoothstep(0.96, 1, i.debug.a);
                col.rgb = lerp(col.rgb, float3(0, 1, 1), p);

                return col;
            }
            ENDHLSL
        }
    }
}
