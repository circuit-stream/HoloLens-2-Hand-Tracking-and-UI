Shader "Jurassic Pack/JPFX"
{
	Properties
	{
		[NoScaleOffset]_MainTex ("Particle Texture", 2D) = "white" {}
		_Glow("Glowing", Float) = 1
		_Scale("Scale", Float) = 1
		[Toggle] _ZWrite("Zwrite", Float) = 0
	}

	SubShader
	{
		Tags{ "LightMode" = "ForwardBase" "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
		Blend SrcAlpha OneMinusSrcAlpha
		Cull back ZWrite [_ZWrite]

		Pass
		{
			CGPROGRAM
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			sampler2D _MainTex; fixed _Glow, _Scale;

			struct In
			{
				float4 vertex : POSITION;
				fixed3 normal : NORMAL;
				fixed4 color : COLOR;
				fixed2 texcoord : TEXCOORD0;
				//UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Out
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				fixed2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				//UNITY_VERTEX_OUTPUT_STEREO
			};

			Out vert (In v)
			{
				Out o;
				UNITY_SETUP_INSTANCE_ID(v);
				//UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.uv = v.texcoord*_Scale;
				o.color = v.color;
				o.vertex = UnityObjectToClipPos(v.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			fixed4 frag (Out i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.uv)*_Glow*i.color;
				UNITY_APPLY_FOG(i.fogCoord, c);
				return fixed4(c.rgb, c.a);
			}
			ENDCG
		}
	}

}
