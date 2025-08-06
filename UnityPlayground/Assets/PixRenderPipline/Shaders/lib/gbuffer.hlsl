#ifndef GBUFFER_INCLUDED
#define GBUFFER_INCLUDED

#include "common.hlsl"

#define MIN_PERCEPTUAL_ROUGHNESS 0.089
#define MIN_ROUGHNESS            0.007921

static half IOR  = half(2.0);

TEXTURE2D(_PixGBuffer_0);SAMPLER(sampler_PixGBuffer_0);float2 _PixGBuffer_0_TexelSize;
TEXTURE2D(_PixGBuffer_1);SAMPLER(sampler_PixGBuffer_1);
TEXTURE2D(_PixGBuffer_2);SAMPLER(sampler_PixGBuffer_2);
#ifdef MOTION_VECTOR_ON
TEXTURE2D(_PixGBuffer_3);SAMPLER(sampler_PixGBuffer_3);
#endif
TEXTURE2D(_PixEarlyZDepth);SAMPLER(sampler_PixEarlyZDepth);
TEXTURE2D(_PixTiledID);SAMPLER(sampler_PixTiledID);

TEXTURE2D(_PixDepthDownSample);SAMPLER(sampler_PixDepthDownSample);half2 _PixDepthDownSample_TexelSize;


float3 ReconstructWorldPos(float2 uv, float ndcDepth) 
{
    float3 ndc = float3(uv*2.0 - 1.0, ndcDepth); 

    #if UNITY_UV_STARTS_AT_TOP
    ndc.y = -ndc.y;
    #endif

    float4 worldPosH = mul(UNITY_MATRIX_I_VP, float4(ndc, 1.0));
    return worldPosH.xyz / worldPosH.w;
}

half sampleDepth(float2 uv){
    return SAMPLE_TEXTURE2D(_PixEarlyZDepth, sampler_PixEarlyZDepth, uv).r;
}

half sampleDepthDownSample(float2 uv){
    return SAMPLE_TEXTURE2D(_PixDepthDownSample, sampler_PixDepthDownSample, uv).r;
}

half3 samplePositionWS(float2 uv){
    half depth = sampleDepth(uv);
    return ReconstructWorldPos(uv, depth);
}

half3 computeDiffuseColor(const half3 albedo, half metallic) {
    return albedo * (1.0 - metallic);
}

half3 computeF0(const half3 albedo, half metallic, half reflectance) {
    return albedo * (metallic + (2*reflectance * (1.0 - metallic)));
}

half computeDielectricF0(half reflectance) {
    return 0.16 * reflectance * reflectance;
}

half iorToF0(half transmittedIor, half incidentIor) {
    return sq((transmittedIor - incidentIor) / (transmittedIor + incidentIor));
}


GBuffer PackGBuffer(half4 color, int shadingModel, half3 normalVS, half4 bentNormalVS,
                    half3 tangentWS, half roughness, half metallic, half anisotropy){
    
    // 没有考虑各向异性的金属
    anisotropy = anisotropy*0.5 + 0.5;
    if (shadingModel == SHADING_MODEL_HAIR){
        metallic = anisotropy;
    }

    half2 nor = normalVS.xy*0.5+0.5;
    half2 tan = tangentWS.xy*0.5+0.5;

    half2 bnor = bentNormalVS.xy*0.5+0.5;
    half ao = bentNormalVS.w;

    int metallicInt = (int)(metallic*31);
    float packedMetallicShadingModel = PackTwoIntToFloat(metallicInt,shadingModel);

    GBuffer gbuffer;
#ifdef PIX_STYLE_NPR
    half3 hsv = RgbToHsv(color.rgb);
    half2 rgb = PackToR5G6B5(hsv.yxz);
    gbuffer.gbuffer_0 = half4(rgb,nor);
    gbuffer.gbuffer_1 = half4(packedMetallicShadingModel,0, roughness, packedMetallicShadingModel);
#else
    gbuffer.gbuffer_0 = half4(color.rgb, ao);
    gbuffer.gbuffer_1 = half4(nor, roughness, packedMetallicShadingModel);
    #if defined EXPORT_TANGENT || defined ENABLE_BENTNORMAL
    gbuffer.gbuffer_2 = half4(tan, bnor);
    #endif
#endif
    
    return gbuffer;
}

#ifdef MOTION_VECTOR_ON
GBuffer PackGBuffer(half4 color, int shadingModel, half3 normalVS, half4 bentNormalWS,
                    half3 tangentWS, half roughness, half metallic, half anisotropy, half2 motionVector)
{
    GBuffer gbuffer = PackGBuffer(color, shadingModel, normalVS, bentNormalWS, tangentWS, roughness, metallic, anisotropy);
    
    half2 xy = PackFloatToR8G8(motionVector.x);
    half2 zw = PackFloatToR8G8(motionVector.y);
    gbuffer.gbuffer_3 = half4(xy,zw);
}
#endif

GBufferData UnpackGBuffer(float2 uv)
{
    half4 gbuffer_0 = SAMPLE_TEXTURE2D(_PixGBuffer_0, sampler_PixGBuffer_0, uv);
    half4 gbuffer_1 = SAMPLE_TEXTURE2D(_PixGBuffer_1, sampler_PixGBuffer_1, uv);
    half4 gbuffer_2 = SAMPLE_TEXTURE2D(_PixGBuffer_2, sampler_PixGBuffer_2, uv);
#ifdef MOTION_VECTOR_ON
    half4 gbuffer_3 = SAMPLE_TEXTURE2D(_PixGBuffer_3, sampler_PixGBuffer_3, uv);
#endif
    float ndcDepth = SAMPLE_TEXTURE2D(_PixEarlyZDepth, sampler_PixEarlyZDepth, uv).r;

#ifdef PIX_STYLE_NPR
    half3 shv = UnpackFromR5G6B5(gbuffer_0.xy);
    half3 albedo = HsvToRgb(shv.yxz);
    half3 normalVS = UnpackNormal(gbuffer_0.zw);
    // TODO:
    half3 tangentWS = half3(0,0,0);
#else
    half3 albedo = gbuffer_0.rgb;
    half3 normalVS = UnpackNormal(gbuffer_1.xy);
    half3 tangentWS = UnpackNormal(gbuffer_2.xy);
#endif

    float3 worldPos = ReconstructWorldPos(uv, ndcDepth);

    half3 cameraPos = _WorldSpaceCameraPos;
    half3 viewDir = cameraPos - worldPos;
    half depth = length(viewDir);
    viewDir /= depth;

    half3 viewUp = half3(0.0, 1.0, 0.0);
    half3 up = mul((half3x3)UNITY_MATRIX_I_V, viewUp);
    half3 right = normalize(cross(viewDir, up));
    up = cross(right, viewDir);
    half3x3 viewToWorld = half3x3(right, up, viewDir);
    half3 normalWS = mul(normalVS, viewToWorld);
    half3 bitangentWS = cross(normalWS, tangentWS);

    half2 params = gbuffer_1.zw;
    half perceptualRoughness = max(MIN_PERCEPTUAL_ROUGHNESS, params.x);
    half roughness = perceptualRoughness*perceptualRoughness;
    half packedMetallicShadingModel = params.y;
    int metallicInt, shadingModel;
    UnpackFloatToTwoInt(packedMetallicShadingModel, metallicInt, shadingModel);
    half metallic = (half)metallicInt/31.0;

    half ior = IOR;

    // shadingModel是Hair时，是用metallic来装的anisotropy
    half anisotropy = metallic*2 - 1;
    if (shadingModel == SHADING_MODEL_HAIR){
        metallic = 0;
        ior *= 1.5;
    }
    
    half reflectance = iorToF0(max(1.0, ior), 1.0);
    half3 f0 = computeF0(albedo, metallic, reflectance);

    if (shadingModel == SHADING_MODEL_HAIR)
        f0*=1.5;

    half3 diffuse = computeDiffuseColor(albedo, metallic);

    half ao = gbuffer_0.a;
    half3 bentNormalVS = UnpackNormal(gbuffer_2.zw);
    half3 bentNormalWS = mul(bentNormalVS, viewToWorld);

    half fresnel = 1-normalVS.z;
    fresnel = pow5(fresnel);

    GBufferData gbufferData;
#if 1
    gbufferData.shadingModel = shadingModel;
    gbufferData.albedo = albedo;
    gbufferData.diffuse = diffuse;
    gbufferData.f0 = f0;
    gbufferData.perceptualRoughness = perceptualRoughness;
    gbufferData.roughness = roughness;
    gbufferData.metallic = metallic;
    gbufferData.anisotropy = anisotropy;
    gbufferData.fresnel = lerp(f0, 0.95, -roughness*fresnel + fresnel);
#else
    gbufferData.shadingModel = 1;
    gbufferData.albedo = 0.5;
    gbufferData.diffuse = 0.5;
    gbufferData.f0 = 0;
    gbufferData.perceptualRoughness = 1;
    gbufferData.roughness = 1;
    gbufferData.metallic = 0;
    gbufferData.anisotropy = 0;
    gbufferData.fresnel = 0;
#endif
    gbufferData.ao = ao;
    gbufferData.bentNormal = bentNormalWS;
    gbufferData.positionWS = worldPos;
    gbufferData.normalWS = normalWS;
    gbufferData.tangentWS = tangentWS;
    gbufferData.bitangentWS = bitangentWS;
    gbufferData.normalVS = normalVS;
    gbufferData.viewDir = viewDir;
    gbufferData.reflectDir = reflect(-viewDir, normalWS);
    gbufferData.NoV = normalVS.z;
    gbufferData.ndcDepth = ndcDepth;
    gbufferData.depth = depth;

#ifdef MOTION_VECTOR_ON
    gbufferData.motionVector = gbuffer_3.xy*2-1;
#endif

    return gbufferData;
}

#endif