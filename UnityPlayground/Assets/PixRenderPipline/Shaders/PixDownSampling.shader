
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "lib/fullscreen.hlsl"
            #include "lib/gbuffer.hlsl"
            #include "lib/light.hlsl"

            #ifndef SSAO_QUALITY_OFF
            #include "lib/ssao.hlsl"

            void SSAO(inout half4 result, half3 positionWS, half2 screenUV){
                half ao = calculateSSAO(screenUV, positionWS);
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

                half LoV = saturate(dot(sun.direction, V)*0.8+0.2);
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

                    float depthSrc = saturate(ndcPos.z + sun.shadowMapBias);
                    half depthDest = SampleShadowMap(sun.shadowMapIndex, uv);

                    volume += depthSrc>depthDest?1:0;
                }

                volume/=maxStep;

                volume *= _SunVolume.y;
                volume = saturate(volume);
                
                result.x = volume*fade;
            }
            #endif

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                float2 uv = input.uv;
                half depth_dest = sampleDepthDownSample(uv);
                half3 positionWS = ReconstructWorldPos(uv, depth_dest);

                half4 result = 0;

                #ifdef PP_SUN_VOLUME
                SunVolume(result, positionWS, uv);
                #endif

                #ifndef SSAO_QUALITY_OFF
                SSAO(result, positionWS, uv);
                #endif
                
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

            TEXTURE2D(_PixDownSampling);SAMPLER(sampler_PixDownSampling);float2 _PixDownSampling_TexelSize;

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                half2 uv = input.uv;

                half2 offset = half2(0, _PixDownSampling_TexelSize.y)*1.5;
                
                half2 result = SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, uv).xy;
                result += SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, uv+offset).xy;
                result += SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, uv+offset*2).xy;
                result += SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, uv-offset).xy;
                result += SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, uv-offset*2).xy;
                result *= 0.2;

                return half4(result,0,0);
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

            TEXTURE2D(_PixDownSamplingBlur);SAMPLER(sampler_PixDownSamplingBlur);float2 _PixDownSamplingBlur_TexelSize;

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                half2 uv = input.uv;

                half2 offset = half2(_PixDownSamplingBlur_TexelSize.x, 0)*1.5;
                
                half2 result = SAMPLE_TEXTURE2D(_PixDownSamplingBlur, sampler_PixDownSamplingBlur, uv).xy;
                result += SAMPLE_TEXTURE2D(_PixDownSamplingBlur, sampler_PixDownSamplingBlur, uv+offset).xy;
                result += SAMPLE_TEXTURE2D(_PixDownSamplingBlur, sampler_PixDownSamplingBlur, uv+offset*2).xy;
                result += SAMPLE_TEXTURE2D(_PixDownSamplingBlur, sampler_PixDownSamplingBlur, uv-offset).xy;
                result += SAMPLE_TEXTURE2D(_PixDownSamplingBlur, sampler_PixDownSamplingBlur, uv-offset*2).xy;
                result *= 0.2;

                return half4(result,0,0);
            }
            ENDHLSL
        }
    }
}
