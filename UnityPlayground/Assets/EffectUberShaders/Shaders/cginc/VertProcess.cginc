#ifdef STRAIL
#include "STrail.cginc"
#endif


void FullScreenProcess(appdata v,inout v2f o){
#ifdef _E_FULLSCREEN
	#if UNITY_UV_STARTS_AT_TOP
	if (_ProjectionParams.x < 0)
    {
		v.uv.y = 1-v.uv.y;
	}
	#endif

	float2 uv = (v.uv-0.5)*2;

	float2 ratioUV = uv;
	ratioUV.x *= (_ScreenParams.y*_FixedRatio.z/_ScreenParams.x);
	uv = _UseFixedRatio?ratioUV:uv;
	uv += _FixedRatio.xy*_UseFixedRatio;
	_FixedRatio.w = _UseFixedRatio?_FixedRatio.w:1;
	uv *= _FixedRatio.w;

	o.pos = float4(uv,0.9999,1);
#endif
}

void BillboadProcess(appdata v,inout v2f o){
#ifdef _E_BILLBOARD
	if(_BillBoard_Y){

		float4x4 m;
        float3 forwardWorld = WorldSpaceViewDir(float4(0, 0, 0, 1));
        forwardWorld.y = 0;
        float3 up = float3(0, 1, 0);
        float3 right = normalize(cross(forwardWorld, up));
        forwardWorld = normalize(forwardWorld);

        m[0] = float4(right, 0);
        m[1] = float4(up, 0);
        m[2] = float4(forwardWorld, 0);
        m[3] = float4(0, 0, 0, 1);
        m = mul(m, unity_ObjectToWorld);
        m[0][3] = unity_ObjectToWorld[0][3];
        m[1][3] = unity_ObjectToWorld[1][3];
        m[2][3] = unity_ObjectToWorld[2][3];
        m[3][3] = unity_ObjectToWorld[3][3];

        o.pos = mul(UNITY_MATRIX_VP, mul(m, v.vertex));

	}else{
		float4x4 mv = UNITY_MATRIX_MV;
        mv[0].xyz = float3(1, 0, 0);
        mv[1].xyz = float3(0, 1, 0);
        mv[2].xyz = float3(0, 0, 1);
        o.pos = mul(UNITY_MATRIX_P, mul(mv, v.vertex));
	}
#endif
}


void CalculateWorldPos(appdata v, inout v2f o){

	#ifdef STRAIL
		float3 p0 = v.vertex;
	 	float3 p1 = v.uv1.xyz;
	 	float3 m0 = v.normal.xyz;
	 	float3 m1 = v.tangent.xyz;
	 	half t = v.tangent.w;
	 	half uv_y = v.uv.z;
	 	o.posWorld = CalculateSTrailPos(p0,p1,m0,m1,t,uv_y);
	 	o.color.a = 1 - v.uv.w;
	#else
	    o.posWorld = mul(unity_ObjectToWorld,v.vertex);
	#endif
}


void CalculateClipPos(appdata v,inout v2f o){
	#ifdef _E_FULLSCREEN
		FullScreenProcess(v, o);
	#elif defined(_E_BILLBOARD)
		BillboadProcess(v, o);
	#else
		o.pos = UnityWorldToClipPos(o.posWorld);
	#endif

	//----------zOffset
	#if UNITY_REVERSED_Z != 1
        _ZOffset *= -2;
    #endif
   	float objScale = 1.0/length(unity_WorldToObject[0].xyz);
	o.pos.z += _ZOffset * _ProjectionParams.y * objScale/o.pos.w; 
	//------------------
}


	