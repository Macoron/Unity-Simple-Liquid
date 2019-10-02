Shader "Liquid/SimpleLiquidShader"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_SurfaceLevel("Liquid Surface Level", Vector) = (0,1,0,0)
		_GravityDirection("Gravity direction",Vector) = (0,-1,0,0)
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent+1" "RenderType" = "Transparent" }

		Zwrite Off
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag


			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 worldPos : POSITION1;
			};

			float4 _GravityDirection;
			float4 _SurfaceLevel;
			fixed4 _Color;

			v2f vert(appdata v)
			{
				v2f o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
			{
				float dotProd1 = dot(_SurfaceLevel - i.worldPos, _GravityDirection);
				if (dotProd1 > 0) discard;

				return _Color;
			}
		ENDCG
		}
	}
}