#ifdef _E_ADVANCE
#define USE_SCREEN_UV
#endif

#ifdef STRAIL
#define IMPORT_TANGENT
#define IMPORT_NORMAL
#define IMPORT_TEXCOORD0
#define IMPORT_TEXCOORD1
#endif

#if defined(_E_RIM) || defined(_E_ADVANCE)
#define IMPORT_NORMAL
#endif

#if defined(_E_RIM) && defined(_E_RIM_NORMAL)
#define IMPORT_TANGENT
#endif

#if defined(_E_RIM) || defined(_E_ADVANCE)
#define EXPORT_NORMAL
#endif

#if defined(_E_ADD_TEX) || defined(_E_MASK)
#define EXPORT_TEXCOORD1
#endif

#if defined(_E_FLIPBOOK) || defined(_E_DISSOLVE) || defined(_E_DISTORTION)
#define EXPORT_TEXCOORD2
#endif

#ifdef USE_SCREEN_UV
#define EXPORT_TEXCOORD3
#endif


half _ZOffset;
half _CurBlendMode;
fixed _AsGray;

sampler2D _MainTex;
half4 _MainRollUV;
half4 _MainTex_ST;
fixed _Main_ClampUV;

fixed4 _Color;
fixed _MainAlpha;
int  _MainCoordsys;

#ifdef _E_CUSTOM_TEX_UV
	sampler2D _CustomUVTex;
	half4 _CustomUVTex_ST;
#endif


#ifdef _E_ADD_TEX
	fixed _AddTexBlendMode;
	sampler2D _AddTex;
	float4 _AddRollUV;
	float4 _AddTex_ST;
	fixed4 _AddColor;
	fixed _Add_ClampUV;
	int _AddCoordsys;
	half _DistortionByMainColor;

	#ifdef _E_DISTORTION
	fixed _Distortion_AddTex;
	fixed _Distortion_MainTex;
	#endif

	#ifdef _E_MASK
	fixed _Mask_AddTex;
	#endif

	#ifdef _E_DISSOLVE
	fixed _Dissolve_AddTex;
	#endif
#endif


#ifdef _E_MASK
	int _MaskChannel;
	int _Mask_ClampUV;
	int _Mask_Alpha;
	int _Mask_Color;
	int _MaskCoordsys;

	sampler2D _MaskTex;
	half4 _MaskTex_ST;

	half4 _MaskRollUV;
	half _MaskInt;
	half _MaskFea;
#endif


#ifdef _E_DISTORTION
	sampler2D _DistTex;
	half4 _DistTex_ST;

	half4 _DistRollUV;
	half4 _Dist;
	int _DistCoordsys;

	#ifdef _E_MASK
		fixed _Mask_Distortion;
	#endif

#endif


#ifdef _E_DISSOLVE
	fixed _DissolveChannel;
	sampler2D _DissTex;
	half4 _DissTex_ST;
	half4 _DissRollUV;
	int _DissCoordsys;

	half _DissInt;
	half _DissFea;

	fixed _Dissolve_Edge;
	fixed4 _DissColor1;
	fixed4 _DissColor2;
	half _DissOffset;
	half _DissSmooth;

	#ifdef _E_MASK
		fixed _Mask_Dissolve;
	#endif

#endif


#ifdef _E_RIM
	fixed _RimGhost;
	fixed _ReverseGhost;
	fixed4 _RimColor;
	half _RimOffset;
	half _RimSmooth;
#endif


#ifdef _E_FLIPBOOK
	half4 _Tile;
	int _FlipbookIndex;
#endif

#ifdef _E_FULLSCREEN
	int _UseFixedRatio;
	half4 _FixedRatio;
#endif

#ifdef _E_BILLBOARD
	int _BillBoard_Y;
#endif

#ifdef _E_ADVANCE
	int _UseAlpha;
#endif


struct appdata
{
	float4 vertex : POSITION;
	fixed4 color : COLOR;

	#ifdef IMPORT_TEXCOORD0
		float4 uv : TEXCOORD0;
	#else
		float2 uv : TEXCOORD0;
	#endif

	#ifdef IMPORT_TEXCOORD1
		float4 uv1 : TEXCOORD1;
	#endif

	#ifdef IMPORT_NORMAL
		float3 normal : NORMAL;
	#endif

	#ifdef IMPORT_TANGENT
		float4 tangent : TANGENT;
	#endif
};

struct v2f
{
	
	float4 pos : SV_POSITION;
	fixed4 color : COLOR;
	float4 posWorld : TEXCOORD4;

	float4 uv : TEXCOORD0;

	#ifdef EXPORT_TEXCOORD1
		float4 uv1 : TEXCOORD1;
	#endif

	#ifdef EXPORT_TEXCOORD2
		float4 uv2 : TEXCOORD2;
	#endif

	#ifdef EXPORT_TEXCOORD3
		float4 uv3 : TEXCOORD3;
	#endif

	#ifdef EXPORT_NORMAL
		float3 normal : NORMAL;
	#endif

	#if EXPORT_TANGENT
		float3 tangent : TEXCOORD5;
		float3 bitangent : TEXCOORD6;
	#endif

};