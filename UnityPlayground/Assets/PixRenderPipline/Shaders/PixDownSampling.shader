
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

            #ifdef PP_SUN_VOLUME
            half4 _SunVolume;

            half SunVolume(half2 screenUV){
                PixLight sun = GetPixLight((int)_SunVolume.x);

                half depth_dest = sampleDepthDownSample(screenUV);
                half3 pos_dest = ReconstructWorldPos(screenUV, depth_dest);
                float3 cameraPos = _WorldSpaceCameraPos;
                half3 V = pos_dest - cameraPos;
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

                    half3 jitter_p = hash33(origin)*stepLen*0.1;
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

                return volume*fade;
            }
            #endif

            half4 frag(VarFullScreenQuad input) : SV_Target
            {
                float2 uv = input.uv;

                #ifdef PP_SUN_VOLUME
                half sunVolume = SunVolume(uv);
                return half4(sunVolume.xxx,1);
                #endif

                
                return 1;
                
            }
            ENDHLSL
        }
    }
}
