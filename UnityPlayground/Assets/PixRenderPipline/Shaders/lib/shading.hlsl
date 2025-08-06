#ifndef SHADING_INCLUDED
#define SHADING_INCLUDED

#include "brdf.hlsl"
#include "bentnormal.hlsl"
#include "gbuffer.hlsl"

half3 anisotropicLobe(GBufferData gbufferData, const half3 L, const half3 H,
        half NoV, half NoL, half NoH, half LoH) {

    half3 T = gbufferData.tangentWS;
    half3 B = gbufferData.bitangentWS;
    half3 V = gbufferData.viewDir;

    half ToV = dot(T, V);
    half BoV = dot(B, V);
    half ToL = dot(T, L);
    half BoL = dot(B, L);
    half ToH = dot(T, H);
    half BoH = dot(B, H);

    half anisotropy = gbufferData.anisotropy;

    half at = max(gbufferData.roughness * (1.0 + anisotropy), MIN_ROUGHNESS);
    half ab = max(gbufferData.roughness * (1.0 - anisotropy), MIN_ROUGHNESS);

    // specular anisotropic BRDF
    half D = distributionAnisotropic(at, ab, ToH, BoH, NoH);
    half v = visibilityAnisotropic(gbufferData.roughness, at, ab, ToV, BoV, ToL, BoL, NoV, NoL);
    half3  F = fresnel(gbufferData.f0, LoH);

    return  min(3 , D*v) * F;
}

half3 isotropicLobe(GBufferData gbufferData, half3 N, half NoH, half3 H, half NoV, half NoL, half LoH) {
    float D = distribution(gbufferData.roughness, N, NoH, H);
    float V = visibility(gbufferData.roughness, NoV, NoL);
    half3  F = fresnel(gbufferData.f0, LoH);
    return D * V * F;
}

half3 specularLobe(GBufferData gbufferData,half3 L, half3 N, half NoH, half3 H, half NoV, half NoL, half LoH) {
    if(gbufferData.shadingModel == SHADING_MODEL_HAIR){
        return anisotropicLobe(gbufferData, L, H, NoV, NoL, NoH, LoH);
    }else{
        return isotropicLobe(gbufferData, N, NoH, H, NoV, NoL, LoH);
    }
}

half3 diffuseLobe(GBufferData gbufferData,  float NoV, float NoL, float LoH) {
    return gbufferData.diffuse * diffuse(gbufferData.roughness, NoV, NoL, LoH);
}

// 后面ShadingModel计算的入口
void evaluateLight(PixLight light, GBufferData gbufferData, half2 srceenUV, inout half3 result) 
{
    half3 N = gbufferData.normalWS;
    half3 L = light.direction;
    half NoL_full = dot(N, L);
    
    // 逆光时可以有大片连续的象素被跳过
    if(NoL_full<0.05){
        return;
    }

    half3 V = gbufferData.viewDir;
    half3 H = normalize(L + V);
    half NoV = gbufferData.NoV;
    half3 NoL = saturate(NoL_full);
    half NoH = saturate(dot(N, H));
    float LoH = saturate(dot(L, H));

    half3 diffuse = 0;
    half shadow = 1;
    shadow *= ContactShadow(light, gbufferData);
    shadow *= VisibilityShadow(light, gbufferData);
    shadow *= ShadowMap(light, gbufferData, srceenUV);

    if(light.enableDiffuse){
        diffuse = diffuseLobe(gbufferData, NoV, NoL, LoH);
    }

    half3 specular = 0;
    if(light.enableSpecular){
        specular = specularLobe(gbufferData, L, N, NoH, H, NoV, NoL, LoH);
        half occ = selfOcclusion(gbufferData, H, gbufferData.roughness);
        specular *= occ;
    }

    result += light.color * (specular + diffuse) * shadow * NoL;
}

void evaluateLightSimple(PixLight light, half3 diffuse, half3 N, inout half3 result)
{
    half3 L = light.direction;
    half3 NoL = saturate(dot(N, L));
    result += light.color * diffuse * NoL * Fd_Lambert();
}

#endif