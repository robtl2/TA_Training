Shader "OSG/Debug/VertexColor"
{
	Properties
	{
		_Channel("E/Channel:R,G,B,A,RGB",Int) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "LightMode" = "UniversalForward" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				half4 color : COLOR;
			};

			int _Channel;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{

				fixed3 color;

				if(_Channel == 0)
					color = i.color.rrr;
				else if(_Channel == 1)
					color = i.color.ggg;
				else if(_Channel == 2)
					color = i.color.bbb;
				else if(_Channel == 3)
					color = i.color.aaa;
				else
					color = i.color.rgb;


				return fixed4(color,1);
			}
			ENDCG
		}
	}

	CustomEditor "SShaderGUI"
}
