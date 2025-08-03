#ifndef IBL_INCLUDED
#define IBL_INCLUDED

#include "bentnormal.hlsl"

#ifndef SSAO_QUALITY_OFF
    #include "lib/ssao.hlsl"
#endif

half4 _SkyColor;
TEXTURECUBE(_SkyTex);SAMPLER(sampler_SkyTex);

half _SkyTexMipCount;
half _RotateSky;
half3 _SkySH[9];

half _AO_Factor;

float perceptualRoughnessToLod(float perceptualRoughness) {
    return _SkyTexMipCount * perceptualRoughness  - perceptualRoughness;
}

half3 getAnisotropicReflectionDir(half3 V, half3 T, half3 B, half3 N, half roughness, half anisotropic)
{
    half3 anisotropicDirection = anisotropic >= 0 ? B : T;
    half3 anisotropicTangent = cross(anisotropicDirection, V);
    half3 anisotropicNormal = cross(anisotropicTangent, anisotropicDirection);
    half bendFactor = abs(anisotropic) * saturate(8 * roughness);
    half3 bentNormal = normalize(lerp(N, anisotropicNormal, bendFactor));
    half3 L = reflect(-V, bentNormal);
    return L;
}

half3 getSpecularDominantDir(half3 N, half3 R, half roughness)
{
    half smoothness = 1 - roughness;
    half factor = smoothness * (sqrt(smoothness) + roughness);
    return lerp(N, R, factor);
}

half3 getPrefilterAnisotropicSpecularLD(GBufferData gbufferData)
{
    half3 V = gbufferData.viewDir;
    half3 T = gbufferData.tangentWS;
    half3 B = gbufferData.bitangentWS;
    half3 N = gbufferData.normalWS;

    half mipLevel = perceptualRoughnessToLod(gbufferData.perceptualRoughness * (1 - abs(gbufferData.anisotropy) * 0.8));

    half3 L = getAnisotropicReflectionDir(V, T, B, N, gbufferData.perceptualRoughness , gbufferData.anisotropy);
    half3 dominantDir = getSpecularDominantDir(N, L, gbufferData.roughness);
    dominantDir = lerp(dominantDir, L, abs(gbufferData.anisotropy));

    half v = selfOcclusion(gbufferData, dominantDir, gbufferData.perceptualRoughness);

    dominantDir = rotate_y(dominantDir, _RotateSky);
    
    return SAMPLE_TEXTURECUBE_LOD(_SkyTex, sampler_SkyTex, dominantDir, mipLevel) * gbufferData.fresnel*v;
}

half3 prefilteredRadiance(GBufferData gbufferData) {
    half v = selfOcclusion(gbufferData, gbufferData.reflectDir, gbufferData.perceptualRoughness);

    half3 r = gbufferData.reflectDir;
    r = rotate_y(r, _RotateSky);
    float lod = perceptualRoughnessToLod(gbufferData.perceptualRoughness);

    return SAMPLE_TEXTURECUBE_LOD(_SkyTex, sampler_SkyTex, r, lod) * gbufferData.fresnel*v;
}


#define DEBUG_SH 0
// 其实吧。。也不一定就非得把bake时的系数套进去
// 咱们TA都是以视觉效果为主, 这里只套基函数效果还好些
half3 diffuseIrradiance(half3 n) {
    n = rotate_y(n, _RotateSky);
    
#if DEBUG_SH
    half Y00 = 0.282095f;
    half Y1_1 = 0.488603f * n.y;
    half Y10 = 0.488603f * n.z;
    half Y11 = 0.488603f * n.x;
    half Y2_2 = 1.092548f * n.x * n.y;
    half Y2_1 = 1.092548f * n.y * n.z;
    half Y20 = 0.315392f * (3.0f * n.z * n.z - 1.0f);
    half Y21 = 1.092548f * n.x * n.z;
    half Y22 = 0.546274f * (n.x * n.x - n.y * n.y);

    half3 sh = _SkySH[0] * Y00;
#else
    half Y1_1 = n.y;
    half Y10 = n.z;
    half Y11 = n.x;
    half Y2_2 = n.x * n.y;
    half Y2_1 = n.y * n.z;
    half Y20 = 3.0f * n.z * n.z - 1.0f;
    half Y21 = n.x * n.z;
    half Y22 = n.x * n.x - n.y * n.y;

    half3 sh = _SkySH[0];
#endif

    sh +=
    _SkySH[1] * Y1_1
    + _SkySH[2] * Y10
    + _SkySH[3] * Y11;

    sh +=
    _SkySH[4] * Y2_2
    + _SkySH[5] * Y2_1
    + _SkySH[6] * Y20
    + _SkySH[7] * Y21
    + _SkySH[8] * Y22;

#if DEBUG_SH
    sh *= 3.545;
#endif

    return max(sh, 0.0);
}

void evaluateIBL(GBufferData gbufferData, half2 uv, inout half3 result) {
    half3 Fr = 0;
    if(gbufferData.shadingModel == SHADING_MODEL_HAIR)
        Fr = getPrefilterAnisotropicSpecularLD(gbufferData);
    else
        Fr = prefilteredRadiance(gbufferData);
    
    #if 0
    half3 Fd = gbufferData.diffuse * diffuseIrradiance(gbufferData.bentNormal);
    #else
    half3 Fd = gbufferData.diffuse * diffuseIrradiance(gbufferData.normalWS);
    #endif

    half ao = gbufferData.ao;
    if(_AO_Factor<0.999)
        ao = lerp(1,ao,_AO_Factor);

    #ifndef SSAO_QUALITY_OFF
        half ssao = calculateSSAO(uv, gbufferData);
        ao *= ssao;
    #endif

    Fd *= ao;

    result += (Fr + Fd) * _SkyColor.rgb;
}

void evaluateIBLSimple(half3 diffuse, half3 normalWS, inout half3 result) {
    half3 Fd = diffuse * diffuseIrradiance(normalWS);
    result += Fd * _SkyColor.rgb;
}


#endif