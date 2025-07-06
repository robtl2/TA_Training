Shader "Tut/PlanarShadow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ShadowColor("ShadowColor", Color) = (0, 0, 0, 0.5)
        _PlanarHeight ("PlanarHeight", Float) = 0
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
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                o.normalWS = normalize(mul((float3x3)GetObjectToWorldMatrix(), v.normalOS));
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
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

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 7              //随便写一个参考值
                Comp notEqual      //只有stencil中记录的值不是ref才通过   
                Pass replace       //如果通过了就把stencil的值替换为ref(这样下次再有ref相同的象素来测试就通不过了)
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float4 _ShadowColor;
            float _PlanarHeight;

            Varyings vert (Attributes v)
            {
                float planarHeight = _PlanarHeight;

                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);

                float3 shadowPos = worldPos;

                //灯光方向
		        Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);

                //阴影的世界空间坐标（低于地面的部分不做改变）
                shadowPos.y = min(worldPos .y , planarHeight);
                shadowPos.xz = worldPos .xz - L.xz * max(0 , worldPos .y - planarHeight) / L.y; 

                Varyings o = (Varyings)0;
                o.positionHCS = TransformWorldToHClip(shadowPos);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return _ShadowColor;
            }
            ENDHLSL
        }
    }
}
