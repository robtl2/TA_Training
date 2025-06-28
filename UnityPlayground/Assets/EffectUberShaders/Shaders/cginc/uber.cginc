#include "UnityCG.cginc"
#include "Parameters.cginc"
#include "UVFactory.cginc"
#include "VertProcess.cginc"
#include "FragProcess.cginc"


v2f vert(appdata v)
{
	v2f o = (v2f)0;
	
	o.color = v.color;

	//posWorld
	CalculateWorldPos(v, o);

	//clipPos
	CalculateClipPos(v, o);

	//处理每个feature独立的UV坐标
	CalculateUVInVS(v, o);

	#ifdef EXPORT_NORMAL
		o.normal = UnityObjectToWorldNormal(v.normal);
	#endif

	#ifdef EXPORT_TANGENT
		o.tangent = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
		o.bitangent = normalize(cross(o.normal, o.tangent) * v.tangent.w);
	#endif

	return o;
}

half4 frag(v2f i , float facing : VFACE) : SV_Target
{
	half mask;
	half4 mainColor;
	UVBundle uv = CalculateUVInFS(i, mainColor, mask);

	half4 vColor;
	half4 col = InitColor(i, mainColor, uv, mask, vColor);

	MaskProcess(mask, col);

	half dissolve;
	fixed3 edge1;
	fixed3 edge2;
	DissolveProcess(uv, vColor.a, mask, col, dissolve, edge1, edge2);

	AddTexProcess(uv, mask, dissolve, edge1, edge2, col);

	RimProcess(i, facing, col);
	
	col.a = saturate(col.a);

	//处理正片叠底的透明区域
	if(_CurBlendMode == 2)
		col = half4(lerp(1, col.rgb, col.a),1);
		
	return col;
}