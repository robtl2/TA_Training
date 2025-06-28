
half4 InitColor(v2f i, half4 mainColor, UVBundle uv, float mask, out half4 vColor){

	vColor = i.color;
	
	#ifdef _E_MASK
		vColor *= (_Mask_Color?lerp(1,_Color,mask):_Color);
	#else
		vColor *= _Color;
	#endif

	#ifdef _E_FLIPBOOK
		half4 col = tex2D(_MainTex, uv.flipbookUV);
	#else
		half4 col = mainColor;
	#endif

	#ifdef _E_ADVANCE
	col.rgb = lerp(col.rgb, dot(col.rgb,half3(0.299,0.587,0.184)), _AsGray);
	#endif

	col *= vColor;
	col.a *= _MainAlpha;

	return col;
}

void MaskProcess(float mask, inout half4 col){
	#ifdef _E_MASK

		#if defined(_E_ADD_TEX) && defined(_E_ADVANCE)
		col.a *= _Mask_Alpha?(_Mask_AddTex?1:mask):1; 
		#else
		col.a *= _Mask_Alpha?mask:1;

		#endif
	#endif
}

void DissolveProcess(UVBundle uv, half alpha, half mask, inout half4 col, inout half dissolve, inout fixed3 edge1, inout float3 edge2){
	dissolve = 0;
	edge1 = 0;
	edge2 = 1;

	#ifdef _E_DISSOLVE

		fixed4 dissolveColor = tex2D(_DissTex, uv.dissUV);

		dissolve = dissolveColor[_DissolveChannel];

		fixed pt = 1.0 - alpha;

		#ifdef _E_MASK
		half _mask = 1.0-mask;
		_DissInt = _Mask_Dissolve?lerp(_mask,1,_DissInt):_DissInt;
		#endif

		dissolve = dissolve - _DissInt - pt ;
		dissolve =  smoothstep(0,_DissFea, dissolve+(1-_DissInt)*_DissFea);

		//-------dissolve edge
		#ifdef _E_ADVANCE
			_DissColor2 = _Dissolve_Edge?_DissColor2:float4(1,1,1,1); 
			edge2 = lerp(_DissColor2.rgb,fixed3(1,1,1),dissolve);

			fixed edge1Intensity = dissolve - _DissOffset;
			edge1Intensity = smoothstep(edge1Intensity,edge1Intensity+_DissSmooth,_DissSmooth);
			edge1 = _DissColor1.rgb*edge1Intensity;
			edge1 *= _Dissolve_Edge;

			dissolve =  _Dissolve_Edge?smoothstep(0,_DissSmooth*0.5,dissolve):dissolve;

			#if defined(_E_ADD_TEX) && defined(_E_ADVANCE)
				col.rgb *= _Dissolve_AddTex?1:edge2;
				col.rgb += _Dissolve_AddTex?0:edge1;
			#else
				col.rgb *= edge2;
				col.rgb += edge1;
			#endif
		#endif
		
		#ifdef _E_ADD_TEX
		col.a *= _Dissolve_AddTex?1:dissolve;
		#else
		col.a *= dissolve;
		#endif

	#endif

}

void AddTexProcess(UVBundle uv, float mask, float dissolve, fixed3 edge1, fixed3 edge2, inout half4 col){
	#ifdef _E_ADD_TEX

		fixed4 colAdd = tex2D(_AddTex, uv.addUV);

		#ifdef _E_ADVANCE
		colAdd *= _AddColor;
		#endif

		#ifdef _E_MASK
		colAdd.rgb = lerp((_Mask_AddTex?col.rgb:colAdd.rgb),colAdd.rgb,mask);
		#endif

		#ifdef _E_DISSOLVE 
			#ifdef _E_ADVANCE
				colAdd.rgb *= edge2;
				colAdd.rgb += edge1;
			#endif
			colAdd.a *= dissolve;
		#endif
		
		fixed4 colMultiply = col*colAdd;

		#ifdef _E_ADVANCE // AddTexture BlendMode
			if(_AddTexBlendMode == 0){
				
				col = colMultiply; 
			}else if(_AddTexBlendMode == 1){
				
				col = fixed4(lerp(col.rgb,colAdd.rgb,colAdd.a),col.a);
			}else if(_AddTexBlendMode == 2){
				
				col = col + colAdd;
			}
		#else
		col = colMultiply;
		#endif

	#endif
}

void RimProcess(v2f i,float facing, inout half4 col){
	#ifdef _E_RIM

		half3 V = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
		half3 N = normalize(i.normal*facing);
		half NoV = dot(N, V) - _RimOffset;

		fixed rim = smoothstep(0,_RimSmooth*2,NoV);

		_RimColor.rgb = _ReverseGhost?col.rgb:_RimColor.rgb;
		col.rgb = lerp(_RimColor.rgb,col.rgb,saturate(rim+(1-_RimColor.a) ));

		#ifdef _E_ADVANCE
		fixed ghost = _RimGhost?1-rim:1;
		ghost = _ReverseGhost?1-ghost:ghost;
		col.a *= ghost;
		#endif

	#endif
}




