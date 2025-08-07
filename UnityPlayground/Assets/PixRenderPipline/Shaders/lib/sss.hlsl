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
    half scatteringIntensity = props._m02;
    half transmissionIntensity = props._m03;

    half4 scatteringColor = props[1];
    half4 transmissionColor = props[2];

    if(all(half2(scatteringIntensity, transmissionIntensity) < 0.01)) enabled = false;

    PixSSSProfile sssProfile;
    sssProfile.enabled = enabled;
    sssProfile.scatteringColor = scatteringColor;
    sssProfile.scatteringRadius = scatteringRadius;
    sssProfile.scatteringIntensity = scatteringIntensity;
    sssProfile.transmissionColor = transmissionColor;
    sssProfile.transmissionRadius = transmissionRadius;
    sssProfile.transmissionIntensity = transmissionIntensity;
    return sssProfile;
}



#endif