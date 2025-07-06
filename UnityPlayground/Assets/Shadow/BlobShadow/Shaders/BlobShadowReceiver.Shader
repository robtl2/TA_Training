Shader "Test/BlobShadowReceiver"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _FadeStart ("Fade Start", Float) = 0
        _FadeEnd ("Fade End", Float) = 1
        _Softness ("Softness", Range(0, 1)) = 0.25
        _Ambient ("Ambient", Color) = (0.5,0.5,0.7,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // 这里保持与C#里的一致
            #define MAX_BLOBS 64
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varying
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
            };

            float4 _BlobRects[MAX_BLOBS];
            float _BlobHeights[MAX_BLOBS];
            int _BlobCount;
            float4 _Color;
            float _FadeStart;
            float _FadeEnd;
            float _Softness;
            float4 _Ambient;

            Varying vert(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                Varying output;
                output.positionWS = positionWS;
                // 有世界空间坐标就没必要TransformObjectToHClip了
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            // 将值重映射到0-1之间
            inline float remapTo01(float value, float start, float end)
            {
                return saturate((value - start) / (end - start));
            }

            // 斑点阴影,支持高度渐变和调节软硬
            // 可以采样一张小的贴图，但没这么方便调效果,而且权衡下来也不划算
            half blobShadow(float2 uv, float height, float3 positionWS)
            {
                // 高度渐变
                half start = height;
                half end = height-_FadeEnd;
                half fade = remapTo01(positionWS.y+_FadeStart, end, start);

                // 把比start还高的阴影给裁掉
                half clip = remapTo01((start - positionWS.y), 0.0, 0.5);
                fade *= clip;

                // 让裁掉消隐更自然
                half scale = 1-clip;
                scale *= scale;

                // 圆形斑点
                // 如果采样贴图也只能代替这么一丁点ALU
                float2 center = float2(0.5, 0.5);
                float2 dir = uv - center;
                float blob = (2+scale)*length(dir); 
                blob = remapTo01(blob,1-_Softness,1);  // 软硬度

                // 应用高度渐变
                blob = lerp(1, blob, fade);

                return blob;
            }

            half4 frag(Varying input) : SV_Target
            {
                float2 uv = float2(0, 0);
                float3 positionWS = input.positionWS;
                float shadow = 1;
                
                //遍历CPU传入的矩形
                for (int i = 0; i < _BlobCount; i++)
                {
                    float4 rect = _BlobRects[i];
                    float height = _BlobHeights[i];

                    // 这里会根_Softness值的增加而去增加矩形的大小
                    // 这个计算放到C#里更合理
                    float2 center = rect.xy + rect.zw * 0.5;
                    float2 offset = rect.xy - center;
                    rect = float4(offset,rect.zw);
                    rect *= (1+_Softness*0.8);
                    rect.xy += center;

                    // AABB比Rect做下面的hitTest时更方便
                    float2 rectMin = rect.xy;
                    float2 rectMax = rect.xy + rect.zw;

                    // 判断当前世界坐标是否在矩形内部
                    if (all(positionWS.xz >= rectMin && positionWS.xz <= rectMax))
                    {
                        // 在矩形内部，计算局部·UV坐标
                        float2 localPos = positionWS.xz - rectMin;
                        uv = localPos / rect.zw;

                        shadow = min(shadow, blobShadow(uv, height, positionWS));
                        // 因为可能有多个阴影叠加，所以这里不能break出去
                    }
                }

                // 来个简单光照, 免得看起来太磕碜
                float3 N = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;
                float NoL = saturate(dot(N, L));
                half3 Lit = mainLight.color * NoL;

                // 好了，搓一块看效果
                half3 color = Lit * _Color.rgb * shadow;
                color += _Ambient;

                return half4(color, 1);
            }
            ENDHLSL
        }

        // 如果开了EarlyZ, 这个Pass就用得上了
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AttributesDepth
            {
                float4 positionOS : POSITION;
            };

            struct VaryingsDepth
            {
                float4 positionCS : SV_POSITION;
            };

            VaryingsDepth vertDepth(AttributesDepth input)
            {
                VaryingsDepth output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 fragDepth(VaryingsDepth input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
