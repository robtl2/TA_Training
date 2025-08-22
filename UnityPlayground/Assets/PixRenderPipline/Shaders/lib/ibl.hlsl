#ifndef IBL_INCLUDED
#define IBL_INCLUDED

#include "bentnormal.hlsl"

half4 _SkyColor;
TEXTURECUBE(_SkyTex);SAMPLER(sampler_SkyTex);

half    _SkyTexMipCount;
half    _RotateSky;
half3   _SkySH[9];
half    _IrradianceColor;
half    _IrradianceIntensity;
half    _AO_Factor;

float perceptualRoughnessToLod(float perceptualRoughness) {
    return _SkyTexMipCount * perceptualRoughness  - perceptualRoughness - 1;
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

half3 getPrefilterAnisotropicSpecularLD(MaterialData matData, half2 uv)
{
    half3 V = matData.viewDir;
    half3 T = matData.tangentWS;
    half3 B = matData.bitangentWS;
    half3 N = matData.normalWS;

    half mipLevel = perceptualRoughnessToLod(matData.perceptualRoughness * (1 - abs(matData.anisotropy) * 0.8));

    half3 L = getAnisotropicReflectionDir(V, T, B, N, matData.perceptualRoughness , matData.anisotropy);
    half3 dominantDir = getSpecularDominantDir(N, L, matData.roughness);
    dominantDir = lerp(dominantDir, L, abs(matData.anisotropy));

    half v = selfOcclusion(matData, dominantDir, matData.roughness) * _SkyColor.a;
    dominantDir = rotate_y(dominantDir, _RotateSky);
    
    return SAMPLE_TEXTURECUBE_LOD(_SkyTex, sampler_SkyTex, dominantDir, mipLevel) * matData.fresnel * v;
}

half3 prefilteredRadiance(MaterialData matData, half2 uv) {
    half v = selfOcclusion(matData, matData.reflectDir, matData.roughness) * _SkyColor.a;

    half3 r = matData.reflectDir;
    r = rotate_y(r, _RotateSky);
    float lod = perceptualRoughnessToLod(matData.perceptualRoughness);

    return SAMPLE_TEXTURECUBE_LOD(_SkyTex, sampler_SkyTex, r, lod) * matData.fresnel * v;
}


// 其实吧。。也不一定就非得把bake时的系数套进去
// 咱们TA都是以视觉效果为主, 有时只套基函数效果还好些
// 加入参数让美术自己决定Irradiance的颜色影响强度
half3 diffuseIrradiance(half3 n) {
    n = rotate_y(n, _RotateSky);
    n = n.xzy;
    half coff_0 = lerp(1,0.282h, _IrradianceColor);
    half4 coff_1 = half4(0.489h, 0.489h, 0.489h, 1.1h);
    half4 coff_2 = half4(1.1h, 0.315h, 1.1h, 0.55h);
    coff_1 = lerp(1, coff_1, _IrradianceColor);
    coff_2 = lerp(1, coff_2, _IrradianceColor);
    half m = lerp(1, 4, _IrradianceColor*_IrradianceColor);
    
    half Y00 = coff_0;
    half Y1_1 = coff_1.x * n.y;
    half Y10 = coff_1.y * n.z;
    half Y11 = coff_1.z * n.x;
    half Y2_2 = coff_1.w * n.x * n.y;
    half Y2_1 = coff_2.x * n.y * n.z;
    half Y20 = coff_2.y * (3.0f * n.z * n.z - 1.0f);
    half Y21 = coff_2.z * n.x * n.z;
    half Y22 = coff_2.w * (n.x * n.x - n.y * n.y);

    half3 sh = _SkySH[0] * Y00;
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

    return max(sh * m * _IrradianceIntensity, 0.0h);
}

void evaluateIBL(MaterialData matData, half2 uv, inout half3 result) {
    half3 Fr = 0;
    if(matData.shadingModel == SHADING_MODEL_HAIR)
        Fr = getPrefilterAnisotropicSpecularLD(matData, uv);
    else
        Fr = prefilteredRadiance(matData, uv);

    half3 Fd = matData.diffuse * diffuseIrradiance(matData.normalWS);
    half3 ao = selfOcclusion(matData, matData.bentNormal, 1);
    
    if(_AO_Factor<0.999)
        ao = lerp(1,ao,_AO_Factor);
    
    Fd *= ao;
    
    if(matData.shadingModel == SHADING_MODEL_SSS)
    {
        PixSSSProfile sssProfile = matData.sssProfile;

        half3 Fd_b = matData.diffuse * diffuseIrradiance(sssProfile.sssNormal);

        half3 ao_b = selfOcclusion(matData, sssProfile.sssNormal, 1);
        ao_b = remap01(0, 1-sssProfile.scatteringRadius, ao_b);
        
        if(_AO_Factor<0.999)
            ao_b = lerp(1,ao_b,_AO_Factor);

        Fd_b *= ao_b;

        Fd = lerp(Fd, Fd_b, sssProfile.scatteringColor*sssProfile.scatteringIntensity);
    }

    #ifndef SSAO_QUALITY_OFF
        ao *= matData.ssao;
    #endif

    Fd *= ao;

    result += (Fr + Fd) * _SkyColor.rgb;
}

void evaluateIBLSimple(half3 diffuse, half3 normalWS, inout half3 result) {
    half3 Fd = diffuse * diffuseIrradiance(normalWS);
    result += Fd * _SkyColor.rgb;
}


#endif