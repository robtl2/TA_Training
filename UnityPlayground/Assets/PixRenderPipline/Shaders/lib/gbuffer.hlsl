#ifndef GBUFFER_INCLUDED
#define GBUFFER_INCLUDED

#include "common.hlsl"
#include "sss.hlsl"

#define MIN_PERCEPTUAL_ROUGHNESS 0.089
#define MIN_ROUGHNESS            0.007921

static half IOR  = half(2.0);

TEXTURE2D(_PixGBuffer_0);SAMPLER(sampler_PixGBuffer_0);float2 _PixGBuffer_0_TexelSize;
TEXTURE2D(_PixGBuffer_1);SAMPLER(sampler_PixGBuffer_1);
TEXTURE2D(_PixGBuffer_2);SAMPLER(sampler_PixGBuffer_2);
#ifdef MOTION_VECTOR_ON
TEXTURE2D(_PixGBuffer_3);SAMPLER(sampler_PixGBuffer_3);
#endif
TEXTURE2D(_PixDepthNormal);SAMPLER(sampler_PixDepthNormal);
TEXTURE2D(_PixTiledID);SAMPLER(sampler_PixTiledID);

// TEXTURE2D(_PixDepthDownSample);SAMPLER(sampler_PixDepthDownSample);half2 _PixDepthDownSample_TexelSize;
TEXTURE2D(_PixDownSampling);SAMPLER(sampler_PixDownSampling);float2 _PixDownSampling_TexelSize;

TEXTURE2D(_PixDepthNormalDownSample);SAMPLER(sampler_PixDepthNormalDownSample);float2 _PixDepthNormalDownSample_TexelSize;


#ifdef DEBUG_LIGHT
    half _DebugBrightness;
#endif

#ifdef PP_SUN_VOLUME
half4 _SunVolumeColor;
void evaluateSunVolume(inout half3 color, half2 screenUV){
    // volume sun light的计算放下采样里去了
    half3 volume = SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, screenUV).x;
    color = lerp(color, _SunVolumeColor.rgb, volume);
}
#endif


float3 ReconstructWorldPos(float2 uv, float ndcDepth) 
{
    float3 ndc = float3(uv*2.0h - 1.0h, ndcDepth); 

    #if UNITY_UV_STARTS_AT_TOP
    ndc.y = -ndc.y;
    #endif

    float4 worldPosH = mul(UNITY_MATRIX_I_VP, float4(ndc, 1.0));
    return worldPosH.xyz / worldPosH.w;
}

half sampleDepth(float2 uv){
    float2 depth = SAMPLE_TEXTURE2D(_PixDepthNormal, sampler_PixDepthNormal, uv).rg;
    return DecodeFloatRG(depth);
    // return SAMPLE_TEXTURE2D(_PixDepthNormal, sampler_PixDepthNormal, uv).r;
}

half sampleDepthDownSample(float2 uv, out half3 positionWS, out half3 normalWS){
    half4 depthNormal = SAMPLE_TEXTURE2D(_PixDepthNormalDownSample, sampler_PixDepthNormalDownSample, uv);
    half depth = DecodeFloatRG(depthNormal.rg);
    positionWS = ReconstructWorldPos(uv, depth);

    half3 normalVS = UnpackNormalHemiOctEncode(depthNormal.zw*2-1);
    float3x3 Matrix_V = GetMatrix_WorldToView(positionWS);
    normalWS = mul(normalVS, Matrix_V);

    // #ifdef UNITY_REVERSED_Z
    //     depth = 1.0 - depth;
    // #else
    //     depth = depth * 2 - 1;
    // #endif

    return depth;
}

half sampleDepthDownSample(float2 uv){
    half4 depthNormal = SAMPLE_TEXTURE2D(_PixDepthNormalDownSample, sampler_PixDepthNormalDownSample, uv);
    half depth = DecodeFloatRG(depthNormal.rg);
    // #ifdef UNITY_REVERSED_Z
    //     depth = 1.0 - depth;
    // #else
    //     depth = depth * 2 - 1;
    // #endif
    return depth;
}


half3 samplePositionWS(float2 uv){
    half depth = sampleDepth(uv);
    return ReconstructWorldPos(uv, depth);
}

half3 computeDiffuseColor(const half3 albedo, half metallic) {
    return albedo * (1.0h - metallic);
}

half3 computeF0(const half3 albedo, half metallic, half reflectance) {
    return albedo * (metallic + (2*reflectance * (1.0h - metallic)));
}

half computeDielectricF0(half reflectance) {
    return 0.16h * reflectance * reflectance;
}

half iorToF0(half transmittedIor, half incidentIor) {
    return sq((transmittedIor - incidentIor) / (transmittedIor + incidentIor));
}

half4 DecodeShadow(half encodedFloat)
{
    // 反归一化到[0, 255]范围
    uint packedValue = round(encodedFloat * 255.0);
    // 提取每个2位字段
    int a = (packedValue >> 6) & 0x3; // 提取最高2位
    int b = (packedValue >> 4) & 0x3; // 提取次高2位
    int c = (packedValue >> 2) & 0x3; // 提取次低2位
    int d = packedValue & 0x3;        // 提取最低2位

    half4 shadow = half4(a,b,c,d);
    return shadow/3.0;
}

half3 GetScreenSpaceBlurredNormal(PixSSSProfile sssProfile, half3x3 viewToWorld, half depth, half2 uv, half2 texelSize){
    half2 offsetScale = texelSize * sssProfile.scatteringRadius*16 / depth;
    half2 jitter = hash22(uv) * offsetScale;
    half4 gbuffer_nor = SAMPLE_TEXTURE2D(_PixGBuffer_1, sampler_PixGBuffer_1, uv+jitter);
    gbuffer_nor += SAMPLE_TEXTURE2D(_PixGBuffer_1, sampler_PixGBuffer_1, uv-jitter);
    gbuffer_nor += SAMPLE_TEXTURE2D(_PixGBuffer_1, sampler_PixGBuffer_1, uv+jitter.yx);
    gbuffer_nor += SAMPLE_TEXTURE2D(_PixGBuffer_1, sampler_PixGBuffer_1, uv-jitter.yx);
    gbuffer_nor *= 0.25;

    half3 normalVS = UnpackNormalHemiOctEncode(gbuffer_nor.xy*2-1);
    half3 normalWS = mul(normalVS, viewToWorld);

    return normalWS;
}

GBuffer PackGBuffer(half4 color, int shadingModel, half3 normalVS, half4 bentNormalVS,
                    half3 tangentWS, half roughness, half metallic, half2 motionVector, half anisotropy, int sssProfileIndex){
    
    // 没有考虑各向异性的金属
    anisotropy = anisotropy*0.5 + 0.5;
    if (shadingModel == SHADING_MODEL_HAIR){
        metallic = anisotropy;
    } else if(shadingModel == SHADING_MODEL_SSS){
        metallic = sssProfileIndex/(uint)31;
    }

    half2 nor = PackNormalHemiOctEncode(normalVS)*0.5 + 0.5;
    half2 tan = PackNormalHemiOctEncode(tangentWS)*0.5 + 0.5;
    half2 bnor = PackNormalHemiOctEncode(bentNormalVS)*0.5 + 0.5;

    half ao = bentNormalVS.w;

    int metallicInt = (int)(metallic*31);
    float packedMetallicShadingModel = PackTwoIntToFloat(metallicInt,shadingModel);

    GBuffer gbuffer;
    gbuffer.gbuffer_0 = half4(color.rgb, ao);
    gbuffer.gbuffer_1 = half4(nor, bnor);
    gbuffer.gbuffer_2 = half4(tan, roughness, packedMetallicShadingModel);
    gbuffer.gbuffer_3 = half4(motionVector.xy*0.5 + 0.5, 0, 0); // motion vector
    
    return gbuffer;
}

MaterialData UnpackGBuffer(float2 uv)
{
    half4 gbuffer_0 = SAMPLE_TEXTURE2D(_PixGBuffer_0, sampler_PixGBuffer_0, uv);
    half4 gbuffer_1 = SAMPLE_TEXTURE2D(_PixGBuffer_1, sampler_PixGBuffer_1, uv);
    half4 gbuffer_2 = SAMPLE_TEXTURE2D(_PixGBuffer_2, sampler_PixGBuffer_2, uv);
#ifdef MOTION_VECTOR_ON
    half4 gbuffer_3 = SAMPLE_TEXTURE2D(_PixGBuffer_3, sampler_PixGBuffer_3, uv);
#endif
    // float2 ndcPos = SAMPLE_TEXTURE2D(_PixEarlyZDepth, sampler_PixEarlyZDepth, uv).zw;
    float ndcDepth = sampleDepth(uv);

    half3 albedo = gbuffer_0.rgb;
    half3 normalVS = UnpackNormalHemiOctEncode(gbuffer_1.xy*2-1);
    half3 tangentWS = UnpackNormalHemiOctEncode(gbuffer_2.xy*2-1);

    float3 worldPos = ReconstructWorldPos(uv, ndcDepth);

    half3 cameraPos = _WorldSpaceCameraPos;
    half3 viewDir = cameraPos - worldPos;
    half depth = length(viewDir);
    viewDir /= depth;
    
    half3x3 viewToWorld = GetMatrix_WorldToView(worldPos);
    half3 normalWS = mul(normalVS, viewToWorld);
    half3 bitangentWS = cross(normalWS, tangentWS);

    half2 params = gbuffer_2.zw;
    half perceptualRoughness = max(MIN_PERCEPTUAL_ROUGHNESS, params.x);
    half roughness = perceptualRoughness*perceptualRoughness;
    half packedMetallicShadingModel = params.y;
    int metallicInt, shadingModel;
    UnpackFloatToTwoInt(packedMetallicShadingModel, metallicInt, shadingModel);
    half metallic = (half)metallicInt/31.0;

    half ior = IOR;

    int sssProfileIndex = 0;
    PixSSSProfile sssProfile;
    // shadingModel是Hair时，是用metallic来装的anisotropy
    half anisotropy = metallic*2 - 1;
    if (shadingModel == SHADING_MODEL_HAIR){
        metallic = 0;
        ior *= 1.5;
    }else if (shadingModel == SHADING_MODEL_SSS){
        metallic = 0;
        sssProfileIndex = metallicInt;
    }
    sssProfile = GetPixSSSProfile(sssProfileIndex);

    half reflectance = iorToF0(max(1.0, ior), 1.0);
    half3 f0 = computeF0(albedo, metallic, reflectance);

    if (shadingModel == SHADING_MODEL_HAIR)
        f0*=1.2;

    half3 diffuse = computeDiffuseColor(albedo, metallic);

    half ao = gbuffer_0.a;
    half3 bentNormalVS = UnpackNormalHemiOctEncode(gbuffer_1.zw*2-1);
    half3 bentNormalWS = mul(bentNormalVS, viewToWorld);

    half fresnel = 1-normalVS.z;
    fresnel = pow5(fresnel);


    half3 downSampleColor = SAMPLE_TEXTURE2D(_PixDownSampling, sampler_PixDownSampling, uv).rgb;
    half sunVolume = downSampleColor.r;
    half ssao = downSampleColor.g;
    half4 shadow = DecodeShadow(downSampleColor.b);
    half shadows[4] = {shadow.x, shadow.y, shadow.z, shadow.w};

    MaterialData matData;
    matData.sunVolume = sunVolume;
    matData.ssao = ssao;
    matData.shadows = shadows;
    matData.shadingModel = shadingModel;
    matData.albedo = albedo;
    matData.diffuse = diffuse;
    matData.f0 = f0;
    matData.perceptualRoughness = perceptualRoughness;
    matData.roughness = roughness;
    matData.metallic = metallic;
    matData.anisotropy = anisotropy;
    matData.fresnel = lerp(f0, 0.95, -roughness*fresnel + fresnel);
    matData.ao = ao;
    matData.bentNormal = bentNormalWS;
    matData.positionWS = worldPos;
    matData.normalWS = normalWS;
    matData.tangentWS = tangentWS;
    matData.bitangentWS = bitangentWS;
    matData.normalVS = normalVS;
    matData.viewDir = viewDir;
    matData.reflectDir = reflect(-viewDir, normalWS);
    matData.NoV = normalVS.z;
    matData.ndcDepth = ndcDepth;
    matData.depth = depth;
    matData.viewToWorld = viewToWorld;
    matData.sssProfile = sssProfile;

#ifdef DEBUG_LIGHT
    matData.albedo = _DebugBrightness;
    matData.diffuse = _DebugBrightness;
#endif

#ifdef MOTION_VECTOR_ON
    matData.motionVector = gbuffer_3.xy*2-1;
#else
    matData.motionVector = half2(0, 0);
#endif

    if (shadingModel == SHADING_MODEL_SSS && sssProfile.type == 1){
        half3 sssNormal = GetScreenSpaceBlurredNormal(sssProfile, viewToWorld, depth, uv, _PixGBuffer_0_TexelSize);
        sssProfile.sssNormal = sssNormal;
    }
    else
        sssProfile.sssNormal = bentNormalWS;

    return matData;
}



#endif