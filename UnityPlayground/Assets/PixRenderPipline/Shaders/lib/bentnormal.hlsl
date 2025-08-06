#ifndef BENT_NORMAL_INCLUDED
#define BENT_NORMAL_INCLUDED


half selfOcclusion(GBufferData gbufferData, half3 dir){
    // 就是瞎试
    half ao = gbufferData.ao;
    half ao1 = lerp(1,gbufferData.ao,0.5);
    half NoD = saturate(dot(gbufferData.bentNormal, dir));
    half occ = saturate((NoD+gbufferData.ao-0.5));
    return occ * ao1;
}

half selfOcclusion(GBufferData gbufferData, half3 dir, half roughness){
    half occ = selfOcclusion(gbufferData, dir);
    half from = lerp(0.2, 0.0, roughness);
    half to = lerp(0.3, 1, roughness);
    occ = remap01(from,to,occ);
    return occ;
}

#endif