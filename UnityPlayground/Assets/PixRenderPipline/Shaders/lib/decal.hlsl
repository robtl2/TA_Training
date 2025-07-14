#ifndef PIX_DECAL_HLSL
#define PIX_DECAL_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "lib/gbuffer.hlsl"

struct Attributes_Decal
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings_Decal
{
    float4 positionCS : SV_POSITION;
    float4 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);

UNITY_INSTANCING_BUFFER_START(Props)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _ShadingModel)
    UNITY_DEFINE_INSTANCED_PROP(float, _BlendMode)
    UNITY_DEFINE_INSTANCED_PROP(float, _Alpha)
UNITY_INSTANCING_BUFFER_END(Props)

Varyings_Decal vert(Attributes_Decal input)
{
    Varyings_Decal output;
    UNITY_SETUP_INSTANCE_ID(input);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    half4 screenPos = ComputeScreenPos(output.positionCS);
    // 把除法放顶点里，象素里计算能快一丢丢
    screenPos.w = 1/screenPos.w;
    output.uv = screenPos;

    UNITY_TRANSFER_INSTANCE_ID(input, output);
    return output;
}

half4 frag_stencil(Varyings_Decal input) : SV_Target
{
    return 0;
}

half4 frag_decal(Varyings_Decal input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    // 使用实例化数据
    float4 mainTex_ST = UNITY_ACCESS_INSTANCED_PROP(Props, _MainTex_ST);
    float shadingModel = UNITY_ACCESS_INSTANCED_PROP(Props, _ShadingModel);
    float blendMode = UNITY_ACCESS_INSTANCED_PROP(Props, _BlendMode);
    float alpha = UNITY_ACCESS_INSTANCED_PROP(Props, _Alpha);
    
    // 通过屏幕坐标UI拿取GBuffer数据
    half2 screenUV = input.uv.xy * input.uv.w;
    GBufferData gbufferData = UnpackGBuffer(screenUV);

    // 把原场景象素的世界坐标转换到Decal的局部坐标
    half3 pos = gbufferData.positionWS;
    half4 posLocal = mul(UNITY_MATRIX_I_M, half4(pos, 1));

    // 把局部坐标转换到UV空间
    half2 uv = posLocal.xy+0.5;
    
    // 应用Atlas局部uv
    half2 local_uv = uv * mainTex_ST.xy + mainTex_ST.zw;

    half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, local_uv);
    col.a *= alpha;

    // 清理uv超过象限的部分
    if(any(uv < 0.001 || uv > 0.999))
        col.a = 0;

    // 正片叠底颜色调整
    if (blendMode == 2)
        col.rgb = lerp(1,col.rgb, col.a);

    return col;
}

#endif