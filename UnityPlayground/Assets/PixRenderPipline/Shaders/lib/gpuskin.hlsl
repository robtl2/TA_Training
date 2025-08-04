#ifndef GPUSKIN_INCLUDED
#define GPUSKIN_INCLUDED

#define MOTION_VECTOR_ON

#define MAX_BONE_COUNT  256

int _BindPoseCount;
float4x4 _BindPoses[MAX_BONE_COUNT];
float4x4 _CurrentPoses[MAX_BONE_COUNT];
float4x4 _PreviousPoses[MAX_BONE_COUNT];

void transformSkinnedPos(float4 boneWeights, uint4 boneIndices, inout float4 positionOS){
    float4 pos = 0;
    float4x4 skinMatrix;

    skinMatrix = _CurrentPoses[boneIndices.x];
    pos += mul(skinMatrix, positionOS) * boneWeights.x;

    skinMatrix = _CurrentPoses[boneIndices.y];
    pos += mul(skinMatrix, positionOS) * boneWeights.y;

    skinMatrix = _CurrentPoses[boneIndices.z];
    pos += mul(skinMatrix, positionOS) * boneWeights.z;

    skinMatrix = _CurrentPoses[boneIndices.w];
    pos += mul(skinMatrix, positionOS) * boneWeights.w;

    positionOS = pos;
}

void transformSkinnedDir(float4 boneWeights, uint4 boneIndices, inout float3 normalOS ){
    float3 dir = 0;
    float3x3 skinMatrix;

    skinMatrix = (float3x3)_CurrentPoses[boneIndices.x];
    dir += mul(skinMatrix, normalOS) * boneWeights.x;

    skinMatrix = (float3x3)_CurrentPoses[boneIndices.y];
    dir += mul(skinMatrix, normalOS) * boneWeights.y;

    skinMatrix = (float3x3)_CurrentPoses[boneIndices.z];
    dir += mul(skinMatrix, normalOS) * boneWeights.z;

    skinMatrix = (float3x3)_CurrentPoses[boneIndices.w];
    dir += mul(skinMatrix, normalOS) * boneWeights.w;

    normalOS = dir;
}


#endif