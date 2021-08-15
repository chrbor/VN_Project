//https://mispy.me/unity-alpha-blending-overlap/
//https://www.youtube.com/watch?v=csyrUnGA7ZU&list=PLX2vGYjWbI0RS_lkb68ApE2YPcZMC4Ohz&index=7
//https://forum.unity.com/threads/how-to-get-position-of-current-pixel-in-screen-space-in-framgment-shader-function.219843/

//http://developer.download.nvidia.com/CgTutorial/cg_tutorial_appendix_e.html
Shader "Unlit/noAlphaOverlap"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader{
		Tags{ "Queue" = "Transparent" "Rendertype" = "Transparent" }
		Cull Back

		Pass{
		Blend SrcAlpha OneMinusSrcAlpha // Traditional transparency

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

	uniform sampler2D _MainTex;
	sampler2D _CameraOpaqueTexture;

	struct v2f {
		half4 pos : POSITION;
		half4 screenPos : TexCOORD1;
		half4 color : COLOR0;
		half2 uv : TEXCOORD0;
	};

	v2f vert(appdata_full v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.color = v.color;
		o.screenPos = ComputeScreenPos(o.pos);
		half2 uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord);
		o.uv = uv;
		return o;
	}

	half4 frag(v2f i) : COLOR{
		half4 color = tex2D(_MainTex, i.uv);
		if (color.a == 0)
			discard;
		half4 bgcol = tex2D(_CameraOpaqueTexture, half2(i.screenPos.x, i.screenPos.y));

		//bgcol *= i.color * color * (1 - color.a);
		bgcol.a = color.a;// *i.color.a;
		return bgcol;
	}

		ENDCG
	}

	}

		Fallback off
}