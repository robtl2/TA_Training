#ifndef GPUSKIN_INCLUDED
#define GPUSKIN_INCLUDED

// TODO: fullfil GpuSkin on DrawInstanced

#define MAX_BONE_COUNT  256
UNITY_INSTANCING_BUFFER_START(Props_gpu_skin)
float4x4 _CurrentPoses[MAX_BONE_COUNT];
float4x4 _PreviousPoses[MAX_BONE_COUNT];
UNITY_INSTANCING_BUFFER_END(Props_gpu_skin)

void transformSkinnedPos(float4 boneWeights, uint4 boneIndices, inout float4 positionOS){
    float4 pos = 0;
    float4x4 skinMatrix;

    float4x4 currentPoses[MAX_BONE_COUNT] = UNITY_ACCESS_INSTANCED_PROP(Props_gpu_skin, _CurrentPoses);

    skinMatrix = currentPoses[boneIndices.x];
    pos += mul(skinMatrix, positionOS) * boneWeights.x;

    skinMatrix = currentPoses[boneIndices.y];
    pos += mul(skinMatrix, positionOS) * boneWeights.y;

    skinMatrix = currentPoses[boneIndices.z];
    pos += mul(skinMatrix, positionOS) * boneWeights.z;

    skinMatrix = currentPoses[boneIndices.w];
    pos += mul(skinMatrix, positionOS) * boneWeights.w;

    positionOS = pos;
}

void transformPreviousSkinnedPos(float4 boneWeights, uint4 boneIndices, inout float4 positionOS){
    float4 pos = 0;
    float4x4 skinMatrix;

    float4x4 previousPoses[MAX_BONE_COUNT] = UNITY_ACCESS_INSTANCED_PROP(Props_gpu_skin, _PreviousPoses);

    skinMatrix = previousPoses[boneIndices.x];
    pos += mul(skinMatrix, positionOS) * boneWeights.x;

    skinMatrix = previousPoses[boneIndices.y];
    pos += mul(skinMatrix, positionOS) * boneWeights.y;

    skinMatrix = previousPoses[boneIndices.z];
    pos += mul(skinMatrix, positionOS) * boneWeights.z;

    skinMatrix = previousPoses[boneIndices.w];
    pos += mul(skinMatrix, positionOS) * boneWeights.w;

    positionOS = pos;
}

void transformSkinnedDir(float4 boneWeights, uint4 boneIndices, inout float3 normalOS ){
    float3 dir = 0;
    float3x3 skinMatrix;

    float4x4 currentPoses[MAX_BONE_COUNT] = UNITY_ACCESS_INSTANCED_PROP(Props_gpu_skin, _CurrentPoses);

    skinMatrix = (float3x3)currentPoses[boneIndices.x];
    dir += mul(skinMatrix, normalOS) * boneWeights.x;

    skinMatrix = (float3x3)currentPoses[boneIndices.y];
    dir += mul(skinMatrix, normalOS) * boneWeights.y;

    skinMatrix = (float3x3)currentPoses[boneIndices.z];
    dir += mul(skinMatrix, normalOS) * boneWeights.z;

    skinMatrix = (float3x3)currentPoses[boneIndices.w];
    dir += mul(skinMatrix, normalOS) * boneWeights.w;

    normalOS = dir;
}


#endif