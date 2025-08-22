
Shader "Hidden/Pix/DownSampling"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        ZWrite Off
        ZTest Always
        Cull Off
       
        Pass
        {
            Name "PixDownSampling"

            HLSLPROGRAM
            #pragma vertex vertFullScreen
            #pragma fragment frag

            #pragma multi_compile SSAO_QUALITY_OFF SSAO_QUALITY_POOR SSAO_QUALITY_LOW SSAO_QUALITY_MEDIUM SSAO_QUALITY_HIGH
            #pragma multi_compile _ PP_SUN_VOLUME
            #pragma multi_compile FORWARD_PIPELINE DEFERRED_PIPELINE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/fullscreen.hlsl"
            #include "lib/gbuffer.hlsl"
            #include "lib/light.hlsl"

            #ifndef SSAO_QUALITY_OFF
            #include "lib/ssao.hlsl"

            void SSAO(inout half4 result, half depth, half3 positionWS, half3 normalWS, half2 screenUV){
                half ao = calculateSSAO(screenUV, depth, positionWS, normalWS);
                result.y = ao;
            }
            #endif

            #ifdef PP_SUN_VOLUME
            half4 _SunVolume;

            void SunVolume(inout half4 result, half3 positionWS, half2 screenUV){
                PixLight sun = GetPixLight((int)_SunVolume.x);

                float3 cameraPos = _WorldSpaceCameraPos;
                half3 V = positionWS - cameraPos;
                half len = length(V);
                V = normalize(V); 
                
                half fade = len;
                fade = remap01(5,50,len);
                fade *= fade;

                half LoV = saturate(dot(sun.direction, V)*0.8 + 0.2);
                fade = lerp(fade,1,LoV);

                uint maxStep = (uint)_SunVolume.z;
                float3 stepLen = V*_SunVolume.w;
                float3 origin = cameraPos;
                half volume = 0;

                uint step = 0;
                [loop]
                while(step < maxStep){
                    step ++;
                    stepLen *= 1.1;
                    half3 rayEnd = V*stepLen*step;
                    origin += stepLen;

                    half3 jitter_p = hash33(origin)*stepLen*0.3;
                    origin += jitter_p;
                    
                    if(length(origin - cameraPos) > len){
                        break;
                    }

                    float4 clipPos = mul(sun.VP, float4(origin,1));
                    float3 ndcPos = clipPos.xyz / clipPos.w;
                    float2 uv = ndcPos.xy * 0.5 + 0.5;
                    uv.y = 1-uv.y;

                    if(any(uv<0 || uv>1)){
                        volume += 1;
                        continue;
                    }

                    if(sun.shadowMapJitter)
                        uv += hash22(screenUV).xy*sun.shadowMapSize.y;

                    float depthSrc = ndcPos.z;
                    half depthDest = SampleShadowMap(sun.shadowMapIndex, uv);

                    volume += depthSrc>depthDest?1:0;
                }

                volume/=maxStep;

                volume *= _SunVolume.y;
                volume = saturate(volume);
                
                result.x = volume*fade;
            }
            #endif

            half PackShadow(int shadowResult[4]){
                uint packedValue = (shadowResult[0] << 6) | (shadowResult[1] << 4) | (shadowResult[2] << 2) | shadowResult[3];
                return packedValue/255.0;
            }

            // 暂时没用这个
            /*
            void EvaluateShadow(inout half4 result, half3 positionWS, half2 screenUV){
                int shadowIndex[4] = {-1,-1,-1,-1};
                int lightIndex[4] = {-1,-1,-1,-1};

                // 如果阴影只采样一次，那结果就只会是0或1，而这里记录为0-3(0,1,2,3)是预留将来最多阴影可以采样4次
                // 因为4个shadowMap的shadow颜色有4阶的话，需要记录的数据就到了4的4次方=256了，而我又只想用一个8bit来记录shadow
                // 所以抠门的PixRenderPipeline只会支持4个shadowMap,最多4次采样
                int shadowResult[4] = {3,3,3,3}; 

                if(PIX_LIGHT_COUNT > 0){
                    int index = 0;

                    [loop]
                    for(int i = 0; i<PIX_LIGHT_COUNT; i++)
                    {
                        PixLight light = GetPixLight(i);

                        // [branch]
                        if(light.enabled && light.shadowMapIndex > -1)
                        {
                            if(index>3)break;

                            lightIndex[index] = i;
                            shadowIndex[index] = light.shadowMapIndex;
                            index++;
                        }
                    }
                }

                for(int i = 0; i < 4; i++){
                    if(lightIndex[i] != -1 && shadowIndex[i] != -1){
                        PixLight light = GetPixLight(lightIndex[i]);

                        half shadow = 1;
                        shadow *= ContactShadow(light, positionWS);
                        shadow *= ShadowMap(light, positionWS, screenUV);

                        int shadowInt = (int)(floor(shadow * 3.00001));

                        shadowResult[light.shadowMapIndex] = shadowInt;
                    } 
                }

                result.b = PackShadow(shadowResult);
            }
            */

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                float2 uv = input.uv;
                
                // 事情集中起来干能方便的避免重复计算
                half3 positionWS;
                half3 normalWS;
                half depth = sampleDepth(uv, positionWS, normalWS);

                half4 result = 1;

                #ifdef PP_SUN_VOLUME
                SunVolume(result, positionWS, uv);
                #endif

                #ifndef SSAO_QUALITY_OFF
                SSAO(result, depth, positionWS, normalWS, uv);
                #endif

                // shadow效果还有点不对，暂时不在这里做
                // EvaluateShadow(result, positionWS, uv);
                
                return result;
            }
            ENDHLSL
        }

        Pass
        {
            Name "PixDownSampling_Blur_V"

            HLSLPROGRAM
            #pragma vertex vertFullScreen
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/fullscreen.hlsl"

            half _BlurDownsample;

            TEXTURE2D(_PixDownSampling);SAMPLER(sampler_PixDownSampling);float2 _PixDownSampling_TexelSize;

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                half2 uv = input.uv;

                half3 result = SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, uv).xyz;

                half2 offset = half2(0, _PixDownSampling_TexelSize.y)*_BlurDownsample;
                result.xy += SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, uv+offset).xy;
                result.xy += SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, uv+offset*2).xy;
                result.xy += SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, uv-offset).xy;
                result.xy += SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, uv-offset*2).xy;
                result.xy *= 0.2;

                return half4(result,0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "PixDownSampling_Blur_H"

            HLSLPROGRAM
            #pragma vertex vertFullScreen
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/fullscreen.hlsl"

            half _BlurDownsample;

            TEXTURE2D(_PixDownSamplingBlur);SAMPLER(sampler_PixDownSamplingBlur);float2 _PixDownSamplingBlur_TexelSize;

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                half2 uv = input.uv;
                half3 result = SAMPLE_TEXTURE2D(_PixDownSamplingBlur, sampler_PixDownSamplingBlur, uv).xyz;

                half2 offset = half2(_PixDownSamplingBlur_TexelSize.x, 0)*_BlurDownsample;
                result.xy += SAMPLE_TEXTURE2D(_PixDownSamplingBlur, sampler_PixDownSamplingBlur, uv+offset).xy;
                result.xy += SAMPLE_TEXTURE2D(_PixDownSamplingBlur, sampler_PixDownSamplingBlur, uv+offset*2).xy;
                result.xy += SAMPLE_TEXTURE2D(_PixDownSamplingBlur, sampler_PixDownSamplingBlur, uv-offset).xy;
                result.xy += SAMPLE_TEXTURE2D(_PixDownSamplingBlur, sampler_PixDownSamplingBlur, uv-offset*2).xy;
                result.xy *= 0.2;

                result.y *= result.y;

                return half4(result,0);
            }
            ENDHLSL
        }
    }
}
