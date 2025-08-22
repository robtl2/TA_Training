#ifndef SSS_INCLUDED
#define SSS_INCLUDED

#include "struct.hlsl"

#define MAX_PROFILE_COUNT 32

int _SSS_ProfileCount;
half4x4 _SSS_Profiles[MAX_PROFILE_COUNT];

PixSSSProfile GetPixSSSProfile(int index){
    int i = max(0, index-1);
    i = min(i, MAX_PROFILE_COUNT-1);

    half4x4 props = _SSS_Profiles[i];

    bool enabled = index>0 && index<=MAX_PROFILE_COUNT;

    half scatteringRadius = props._m00;
    half transmissionRadius = props._m01;
    int type = (int)props._m02;
    

    half3 scatteringColor = props[1].rgb;
    half3 transmissionColor = props[2].rgb;

    half scatteringIntensity = props[1].a;
    half transmissionIntensity = props[2].a;

    if(all(half2(scatteringIntensity, transmissionIntensity) < 0.01)) enabled = false;

    PixSSSProfile sssProfile;
    sssProfile.enabled = enabled;
    sssProfile.type = type;
    sssProfile.scatteringColor = scatteringColor;
    sssProfile.scatteringRadius = scatteringRadius;
    sssProfile.scatteringIntensity = scatteringIntensity;
    sssProfile.transmissionColor = transmissionColor;
    sssProfile.transmissionRadius = transmissionRadius;
    sssProfile.transmissionIntensity = transmissionIntensity;
    sssProfile.sssNormal = half3(0,0,1);
    return sssProfile;
}

half3 GetSSSNormalFromBentNorm(MaterialData matData, half3 L, half NoL){
    PixSSSProfile sssProfile = matData.sssProfile;

    half radius = sssProfile.scatteringRadius*2;
    half3 NoL_b = dot(matData.bentNormal, L)+radius;
    NoL_b = remap01(0, 1+radius, NoL_b);

    NoL = lerp(NoL, NoL_b, sssProfile.scatteringColor * sssProfile.scatteringIntensity);
    return NoL;
}



#endif