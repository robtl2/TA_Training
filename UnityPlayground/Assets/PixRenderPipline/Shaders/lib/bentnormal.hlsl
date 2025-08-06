#ifndef BENT_NORMAL_INCLUDED
#define BENT_NORMAL_INCLUDED


half selfOcclusion(GBufferData gbufferData, half3 dir, half roughness = 1){
#if 1
    // 就是瞎试
    half ao = gbufferData.ao;
    half ao1 = lerp(1,gbufferData.ao,0.8);
    half3 bentNormal = gbufferData.bentNormal;
    half NoD = saturate(dot(bentNormal,dir));
    half occ = saturate((NoD+ao1*0.5-0.5));
    occ *= ao1;
    half from = lerp(0.0, 0.0, roughness);
    half to = lerp(0.5, 1, roughness);
    occ = remap01(from,to,occ);
    return occ;
#else
    return 1;
#endif
}

#endif