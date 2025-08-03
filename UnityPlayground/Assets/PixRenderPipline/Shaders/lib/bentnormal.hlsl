#ifndef BENT_NORMAL_INCLUDED
#define BENT_NORMAL_INCLUDED


half selfOcclusion(GBufferData gbufferData, half3 dir, half roughness = 1){
#if 1
    // 就是瞎试
    half ao = gbufferData.ao;
    half3 bentNormal = gbufferData.bentNormal;
    half NoD = saturate(dot(bentNormal,dir));
    half occ = saturate(NoD + ao*0.5);
    half from = lerp(0.5, 0, roughness);
    half to = lerp(0.6, 1, roughness);
    occ = smoothstep(from,to,occ);
    return occ;
#else
    return 1;
#endif
}

#endif