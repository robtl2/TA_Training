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
    // if(light.isPositive < 0){
    //     result = half3(1,0,0);
    //     return;
    // }

    half effect = 1.0h;
    if(light.enableAreaEffect){
        float4 posInArea = mul(light.effectArea, float4(gbufferData.positionWS, 1.0f));
        
        if(any(posInArea>1 || posInArea<-1)){
            //在影响范围外
            return;
        }else if(light.areaFadeRange>0){
            // 在影响范围内,计算羽化过度
            half pos_max = vmax(abs(posInArea.xyz));
            half dis_to_border = 1- pos_max;
            effect = remap01(0, light.areaFadeRange, dis_to_border);
        }
    }

    half3 N = gbufferData.normalWS;
    half3 L = light.direction;
    half NoL_full = dot(N, L);
    
    // 逆光时可以有大片连续的象素被跳过
    if(NoL_full<-0.1){
        return;
    }

    half3 V = gbufferData.viewDir;
    half3 H = normalize(L + V);
    half NoV = gbufferData.NoV;
    half3 NoL = saturate(NoL_full);
    half NoH = saturate(dot(N, H));
    float LoH = saturate(dot(L, H));

    half shadow = 1;
    shadow *= ContactShadow(light, gbufferData);
    shadow *= VisibilityShadow(light, gbufferData);
    shadow *= ShadowMap(light, gbufferData, srceenUV);

    half3 diffuse = 0;
    if(light.enableDiffuse)
        diffuse = diffuseLobe(gbufferData, NoV, NoL, LoH);

    half3 specular = 0;
    if(light.enableSpecular && light.isPositive){
        half f0 = gbufferData.f0;

        gbufferData.f0 *= light.f0;
        specular = specularLobe(gbufferData, L, N, NoH, H, NoV, NoL, LoH);
        gbufferData.f0 = f0;

        half occ = selfOcclusion(gbufferData, H, gbufferData.roughness);
        specular *= occ;
    }

    if(gbufferData.shadingModel == SHADING_MODEL_SSS){
        PixSSSProfile sssProfile = gbufferData.sssProfile;

        half radius = sssProfile.scatteringRadius*2;
        half3 NoL_b = dot(sssProfile.sssNormal, L)+radius;
        NoL_b = remap01(0, 1+radius, NoL_b);

        NoL = lerp(NoL, NoL_b, sssProfile.scatteringColor * sssProfile.scatteringIntensity);
    }

    if(light.isPositive)
        result += light.color * (specular + diffuse) * shadow * NoL * effect;
    else
        result *= lerp(1, light.color, effect);


    result = max(result, 0.0h);
}

void evaluateLightSimple(PixLight light, half3 diffuse, half3 N, inout half3 result)
{
    half3 L = light.direction;
    half NoL_full = dot(N, L);
    
    if(NoL_full<0.05)
        return;

    half3 NoL = saturate(NoL_full);
    result += light.color * diffuse * NoL * Fd_Lambert();
}

#endif