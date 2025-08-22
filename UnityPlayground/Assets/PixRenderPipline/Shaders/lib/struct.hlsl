#ifndef STRUCT_INCLUDED
#define STRUCT_INCLUDED

// --------预定义的宏也常常被用来当枚举用--------
#define LIGHT_TYPE_DIRECTION    0
#define LIGHT_TYPE_SPOT         1
#define LIGHT_TYPE_POINT        2

#define SAMPLE_QUALITY_POOR     0
#define SAMPLE_QUALITY_LOW      1
#define SAMPLE_QUALITY_MEDIUM   2
#define SAMPLE_QUALITY_HIGH     3

#define SHADING_MODEL_UNLIT     0
#define SHADING_MODEL_LIT       1
#define SHADING_MODEL_HAIR      2
#define SHADING_MODEL_SSS       3

#define SSS_TYPE_BENTNORMAL     0
#define SSS_TYPE_SCREENSPACE    1

// ----------------------------------------------

struct PixSSSProfile{
    bool enabled;

    int type;

    half3 scatteringColor;
    half scatteringRadius;
    half scatteringIntensity;

    half3 transmissionColor;
    half transmissionRadius;
    half transmissionIntensity;

    half3 sssNormal;
};

struct PixLight{
    bool enabled;
    bool isPositive;

    int lightType;
    int shadowMapIndex;
    half2 shadowMapSize;

    half halfAngle;
    half range;

    half3 position;
    half3 direction;
    half3 color;

    half contactShadow;
    int contactSampleCount;
    half contactBias;
    half contactShadowJitter;

    half shadowMapBias;
    bool shadowMapJitter;

    bool enableDiffuse;
    bool enableSpecular;
    half f0;
    // half f90;

    half visibilityShadow;

    bool enableAreaEffect;
    float4x4 effectArea; 
    float areaFadeRange;

    float4x4 VP;
};

// TODO: 把normal和tangent放一组
struct GBuffer
{
    half4 gbuffer_0; //rgb:albedo a:ao
    half4 gbuffer_1; //rg:normal ba:bentNormal
    half4 gbuffer_2; //rg:tangent b:roughness a:(metallic|anisotropy|sssProfileIndex)&shadingModel[5:3]
    half4 gbuffer_3; // movtionVector
};


struct MaterialData
{
    int shadingModel;
    half3 albedo;
    half3 diffuse;
    half3 f0;
    half ao;
    half3 bentNormal;

    half sunVolume;
    half ssao;
    half shadows[4];

    float3 positionWS;
    half3 normalWS;
    half3 tangentWS;
    half3 bitangentWS;
    half3 normalVS;
    half3 viewDir;
    half3 reflectDir;

    half NoV;
    half3 fresnel;
    half ndcDepth;
    half depth;
    half perceptualRoughness;
    half roughness;
    half metallic;
    half anisotropy;

    half3x3 viewToWorld;

    PixSSSProfile sssProfile;

    half2 motionVector;
};


#endif