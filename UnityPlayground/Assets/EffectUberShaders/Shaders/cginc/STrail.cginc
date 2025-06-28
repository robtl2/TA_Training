
int _CustomSection;
half _TrailWidth;
int _TrailDir;

float3 HermitePosition(float3 start, float3 end, float3 tanPoint1, float3 tanPoint2, float t)
{
	// Hermite curve formula:
	// (2t^3 - 3t^2 + 1) * p0 + (t^3 - 2t^2 + t) * m0 + (-2t^3 + 3t^2) * p1 + (t^3 - t^2) * m1

	float t_pow2 = t * t;
	float t_pow3 = t_pow2 * t;
	float t_pow3_mul2 = 2.0 * t_pow3;
	float t_pow2_mul3 = 3.0 * t_pow2;

	float3 position = (t_pow3_mul2 - t_pow2_mul3 + 1.0) * start
	+ (t_pow3 - 2.0 * t_pow2 + t) * tanPoint1
	+ (-t_pow3_mul2 + t_pow2_mul3) * end
	+ (t_pow3 - t_pow2) * tanPoint2;

	return position;
}

float3 CatmullRomTangent(float3 start, float3 end, float3 tanPoint1, float3 tanPoint2, float t)
{
	// Calculate tangents
	// p'(t) = (6t² - 6t)p0 + (3t² - 4t + 1)m0 + (-6t² + 6t)p1 + (3t² - 2t)m1

	float t_pow2 = t * t;
	float t_pow2_mul6 = 6 * t_pow2;
	float t_pow2_mul3 = 3 * t_pow2;
	float t_mul6 = 6 * t;

	float3 tangent = (t_pow2_mul6 - t_mul6) * start
	+ (t_pow2_mul3 - 4 * t + 1) * tanPoint1
	+ (-t_pow2_mul6 + t_mul6) * end
	+ (t_pow2_mul3 - 2 * t) * tanPoint2;

	return normalize(tangent);
}


float4 CalculateSTrailPos(float3 p0, float3 p1, float3 m0, float3 m1, half t,half uv_y){

 	float4 posWorld = float4(HermitePosition(p0,p1,m0,m1,t),1);

 	if(!_CustomSection){
 		float3 V = _TrailDir?float3(0,1,0):normalize(_WorldSpaceCameraPos.xyz - posWorld);
	 	float3 T = CatmullRomTangent(p0,p1,m0,m1,t);
	 	float3 D = normalize(cross(V,T));
	 	float s = sign(uv_y-0.5);
	 	D*=s;
	 	posWorld.xyz += D*_TrailWidth;
 	}
 	
 	return posWorld;
}


