
inline float2 PolarUV(float2 uv,float4 uvST,float roller,float time,float clampUV){
	float2 _uv = uv*2 - 1;

	float v = length(_uv)*uvST.x+uvST.z - roller.x*time;

	v = clampUV?saturate(v):frac(v);
	float u = atan2(_uv.y,_uv.x)*0.1591546*uvST.y + 0.5;

	return float2(u,v);
}

inline float2 RotateUV(float2 uv,float angle){
    angle *= 3.1415927*2;
    float s;
    float c;
    sincos(angle,s,c);
    float2x2 rot= float2x2( c,-s,s,c);

    return mul(rot,uv-0.5) + 0.5;
}

inline float2 flipbookUV(float2 texcoord,float4 tile,int index){
	float2 splitTile = float2(1.0 / tile.x, 1.0 / tile.y);
	float2 uv = texcoord / tile.xy + float2((index%tile.x)*splitTile.x, 1.0 - splitTile.y - floor(index / tile.x)*splitTile.y);
	return uv;
}


inline float2 MakeupUV(float2 uv,float4 uvST,float4 roller,float time,int coordsys){
	float2 _uv = uv;

	#ifdef _E_ADVANCE
		bool isObjUV = !sign(coordsys);
		if(isObjUV){
	#endif
			
			_uv = _uv*uvST.xy + uvST.zw;
			_uv += frac(roller.xy * time);
			

	#ifdef _E_ADVANCE
			_uv = RotateUV(_uv,roller.z + frac(roller.w*time));
		}
	#endif

	return _uv;
}

//for STrail
inline float2 MakeupUV(float4 uv,float4 uvST,float4 roller,float time,int coordsys){
	//选择拉伸或稳定的贴图坐标
	float2 _uv = coordsys? uv.yz : uv.xz;

	return MakeupUV(_uv,uvST,roller,time,coordsys);
}

float2 AssignUV(float3 normal_View,float2 baseUV,float2 uv,float2 screenUV,float4 uvST,float4 roller,float time,int clampUV,int coordsys){
	
	#ifdef _E_ADVANCE
		if(coordsys == 0)
		{
			return uv;
		}
		else if(coordsys == 1)
		{
			float2 rotatedUV = RotateUV(baseUV,roller.z + roller.w*time);
			float2 polarUV = PolarUV(rotatedUV,uvST,roller,time,clampUV);
			return polarUV;
		}
		else if(coordsys == 2)
		{

			#ifdef _E_FULLSCREEN
				return uv;
			#else
				return MakeupUV(screenUV,uvST,roller,time,0);
			#endif

		}
		else if(coordsys == 3)
		{
			return normal_View.xy*0.5+0.5;
		}
		else
		{
			return uv;
		}
	#else
		return uv;
	#endif
}

void CalculateUVInVS(appdata v,inout v2f o){
	float time = _Time.y;
	float2 baseUV = v.uv.xy;

	#ifdef IMPORT_TEXCOORD0
	baseUV = _MainCoordsys? v.uv.yz : v.uv.xz;
	#endif
	
	o.uv.xy = baseUV;
	o.uv.zw = MakeupUV(v.uv, _MainTex_ST, _MainRollUV, time, _MainCoordsys);

	#ifdef _E_ADD_TEX
		o.uv1.xy = MakeupUV(v.uv, _AddTex_ST, _AddRollUV, time, _AddCoordsys);
	#endif

	#ifdef _E_MASK
		o.uv1.zw = MakeupUV(v.uv, _MaskTex_ST, _MaskRollUV, time, _MaskCoordsys);
	#endif

	#if defined(_E_FLIPBOOK)
		o.uv2.xy = flipbookUV(baseUV,_Tile,_FlipbookIndex); 
	#elif defined(_E_DISSOLVE)
		o.uv2.xy = MakeupUV(v.uv, _DissTex_ST, _DissRollUV, time, _DissCoordsys);
	#endif

	#ifdef _E_DISTORTION
		o.uv2.zw = MakeupUV(v.uv, _DistTex_ST, _DistRollUV, time, _DistCoordsys);
	#endif

	#if defined(USE_SCREEN_UV)
		o.uv3 = ComputeScreenPos(o.pos);
	#endif
}

struct UVBundle
{
	float2 mainUV;

	float2 distortionUV;

	float2 screenUV;

#ifdef _E_MASK
	float2 maskUV;
#endif

#ifdef _E_FLIPBOOK
	float2 flipbookUV;
#endif

#ifdef _E_ADD_TEX	
	float2 addUV;
#endif

#ifdef _E_DISSOLVE
	float2 dissUV;
#endif

};

//有的UV和颜色还有mask相关，只好在这里把mainColor和mask一起计算了
UVBundle CalculateUVInFS(v2f i,out half4 mainColor, out half mask){
	float time = _Time.y;
	
	mask = 1;

	UVBundle uv = (UVBundle)0;

	half3 normal_View = half3(0,0,1);
	#ifdef _E_ADVANCE
	normal_View = normalize(mul((float3x3)UNITY_MATRIX_V,i.normal.xyz));
	#endif

	uv.screenUV = half2(0,0);
	#ifdef USE_SCREEN_UV
	uv.screenUV = i.uv3.xy/i.uv3.w;
	#endif
	
	#ifdef _E_MASK
		uv.maskUV = AssignUV(normal_View,i.uv.xy,i.uv1.zw,uv.screenUV,_MaskTex_ST,_MaskRollUV,time,_Mask_ClampUV,_MaskCoordsys);
		
		#ifdef _E_ADVANCE
		uv.maskUV = _Mask_ClampUV?saturate(uv.maskUV):uv.maskUV;
		#endif

		fixed4 maskColor = tex2D(_MaskTex, uv.maskUV);
		mask = maskColor[_MaskChannel];

		#ifdef _E_ADVANCE
		mask = lerp(1,mask,_MaskInt);
		mask -= lerp(0,_MaskInt-1,step(1,_MaskInt));
		mask = smoothstep(0,_MaskFea,mask+(1-_MaskInt)*_MaskFea);
		#endif

	#endif


	#ifdef _E_DISTORTION
		float2 distUV = AssignUV(normal_View,i.uv.xy,i.uv2.zw,uv.screenUV,_DistTex_ST,_DistRollUV,time,0,_DistCoordsys);

		half2 distortion =  _Dist.xy;

		#ifdef _E_ADVANCE
			half2 distortionDir = normalize(RotateUV(float2(0.5,1),_Dist.z)-0.5);
			half2 uvDir = normalize(i.uv.xy - 0.5);
			half distortionLimit = 1-(dot(distortionDir,uvDir)+1)*0.5  ;
			distortionLimit = lerp(1,distortionLimit,_Dist.w);
			distortion *= distortionLimit;
		#endif

		fixed4 distColor = tex2D(_DistTex, distUV);

		uv.distortionUV = (distColor.rg*2 - 1)  * distortion;

		#ifdef _E_MASK
		uv.distortionUV *= _Mask_Distortion?mask:1;
		#endif
	#endif


	#ifdef _E_FLIPBOOK
		uv.flipbookUV = i.uv2.xy;
	#else
		uv.mainUV = AssignUV(normal_View,i.uv.xy,i.uv.zw,uv.screenUV,_MainTex_ST,_MainRollUV,time,_Main_ClampUV,_MainCoordsys);

		#ifdef _E_ADD_TEX
			#ifdef _E_DISTORTION
			uv.mainUV = uv.mainUV + uv.distortionUV*_Distortion_MainTex;
			#else
			uv.mainUV = uv.mainUV + uv.distortionUV;
			#endif
		#else
			uv.mainUV = uv.mainUV + uv.distortionUV;
		#endif
		
		#ifdef _E_ADVANCE
		uv.mainUV = _Main_ClampUV?saturate(uv.mainUV):uv.mainUV;
		#endif
	#endif

    float2 dx = ddx(i.uv.xy);
    float2 dy = ddy(i.uv.xy);

	mainColor = tex2Dgrad(_MainTex, uv.mainUV, dx, dy);
	#ifdef _E_ADVANCE
	mainColor.a = _UseAlpha?mainColor.a:1;
	#endif


	#ifdef _E_DISSOLVE
		uv.dissUV = AssignUV(normal_View,i.uv.xy,i.uv2.xy,uv.screenUV,_DissTex_ST,_DissRollUV,time,0,_DissCoordsys);
	#endif


	#ifdef _E_ADD_TEX
		i.uv1.xy += mainColor.r*_DistortionByMainColor;
		uv.addUV = AssignUV(normal_View,i.uv.xy,i.uv1.xy,uv.screenUV,_AddTex_ST,_AddRollUV,time,_Add_ClampUV,_AddCoordsys);
		
		#ifdef _E_DISTORTION
		uv.addUV += uv.distortionUV*_Distortion_AddTex;
		#endif

		#ifdef _E_ADVANCE
		uv.addUV = _Add_ClampUV?saturate(uv.addUV):uv.addUV;
		#endif
	#endif


	return uv;
}