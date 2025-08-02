#ifndef BENT_NORMAL_INCLUDED
#define BENT_NORMAL_INCLUDED


half selfOcclusion(GBufferData gbufferData, half3 dir, half smooth = 1){
#if 1
    // 就是瞎试
    half ao = gbufferData.ao;
    half3 bentNormal = gbufferData.bentNormal;
    half NoD = saturate(dot(bentNormal,dir));
    half occ = saturate(NoD - (1-sqrt(ao)));
    occ = saturate(occ * lerp(1,5,smooth) + smooth*0.2 );
    occ = sq(occ);
    occ = smoothstep(0,smooth,occ);
    return occ;
#else
    return 1;
#endif
}

#endif