//https://forum.unity.com/threads/the-scriptable-render-pipeline-how-to-support-grabpass.521473/

Shader "Unlit/MaskShader"
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

	half4 frag(v2f i) : COLOR
	{
		half4 color = tex2D(_MainTex, i.uv);
		if (color.a == 0)
			discard;
		half4 bgcol;
		bgcol = i.color * color;
		bgcol.a = color.a * i.color.a;
		return bgcol;
	}

		ENDCG
	}

	}

		Fallback off
}
