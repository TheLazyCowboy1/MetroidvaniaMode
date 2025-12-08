//Simple parallax shader by TheLazyCowboy1

Shader "TheLazyCowboy1/Wing" //Unlit Transparent Vertex Colored Additive 
{
	Properties 
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	Category 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite Off
		//Alphatest Greater 0
		Blend SrcAlpha OneMinusSrcAlpha
		Fog { Color(0,0,0,0) }
		Lighting Off
		Cull Off //we can turn backface culling off because we know nothing will be facing backwards

		BindChannels 
		{
			Bind "Vertex", vertex
			Bind "texcoord", texcoord 
			Bind "Color", color 
		}

		SubShader   
		{	
		
			Pass 
			{
				
				
CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
//#pragma debug

//#pragma multi_compile _ THELAZYCOWBOY1_TERRAIN

// #pragma enable_d3d11_debug_symbols
#include "UnityCG.cginc"
//#include "_Functions.cginc"
//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

sampler2D _MainTex;
uniform float2 _MainTex_TexelSize;

sampler2D _NoiseTex2;

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
    //float2 scrPos : TEXCOORD1;
    float4 clr : COLOR;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    //o.scrPos = ComputeScreenPos(o.pos);
    o.clr = v.color;
    return o;
}

half4 frag (v2f i) : SV_Target
{
	half4 retCol = i.clr * tex2D(_MainTex, i.uv);
	if (retCol.w <= 0) {
		discard;
	}

	//vertical lines
	half sinTime = sin(-53*_Time.y + 100*i.uv.x);
	half opacity = -0.2 + 0.3 * (sinTime * 0.5 + 0.5);
	
	//noise flicker
	half noiseStrength = 0.23;
	half2 noiseSamplePos = i.uv*0.5 + 0.25*half2(1+_SinTime.w, 1-_SinTime.z);
	half4 noise = tex2D(_NoiseTex2, noiseSamplePos);
	half2 noiseDiff = noise.xy - i.uv;
	noiseDiff.x = abs(noiseDiff.x) % 0.25;
	noiseDiff.y = abs(noiseDiff.y) % 0.25;
	opacity = opacity + 0.5 * saturate((noiseStrength - noiseDiff.x - noiseDiff.y - abs((1+sinTime)-noise.z*2)) * 1000);

	//fade out horizontally
	half wMod = (1 - i.clr.w) * (1 - i.clr.w);
	opacity = opacity - 2 * i.uv.x * wMod;

	retCol.w = saturate(retCol.w + opacity * (1 - wMod));
	return retCol;

}
ENDCG
				
			}
		} 
	}
}
