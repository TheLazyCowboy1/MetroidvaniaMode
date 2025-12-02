//Simple parallax shader by TheLazyCowboy1

Shader "TheLazyCowboy1/ShieldEffect" //Unlit Transparent Vertex Colored Additive 
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
	//a semicircle starting on the left
	half2 circleCenter = half2(0, 0.5);

	//get distance
	half2 relPos = i.uv - circleCenter;
	relPos.y = relPos.y * 2;
	half sqrDist = relPos.x*relPos.x + relPos.y*relPos.y;
	//half dist = sqrt(sqrDist);

	//don't draw outside of the semi-circle
	if (sqrDist > 1) {
		discard;
	}

	//vertical lines
	half sinTime = sin(53*_Time.y + 300*i.uv.y);
	half opacity = 0.7 + 0.3 * (sinTime * 0.5 + 0.5);

	//outside edge opaque
	half outsideThickness = 0.15;
	opacity = opacity + saturate((sqrDist - 1 + outsideThickness) / outsideThickness * 0.4);

	//outside edge fade out
	half outsideFadeOut = 0.07;
	opacity = opacity * saturate(1 - (sqrDist - 1 + outsideFadeOut) / outsideFadeOut);
	
	//noise flicker
	half noiseStrength = 0.23;
	half2 noiseSamplePos = i.uv*0.5 + 0.25*half2(1+_SinTime.w, 1-_SinTime.z);
	half4 noise = tex2D(_NoiseTex2, noiseSamplePos);
	half2 noiseDiff = noise.xy - i.uv;
	noiseDiff.x = abs(noiseDiff.x) % 0.25;
	noiseDiff.y = abs(noiseDiff.y) % 0.25;
	opacity = opacity + saturate((noiseStrength - noiseDiff.x - noiseDiff.y - abs((1+sinTime)-noise.z*2)) * 1000);

	//inside fade out
	half invWSqrd = (1-i.clr.w)*(1-i.clr.w);
	half yDist = saturate(abs(2*(0.5 - i.uv.y)) + invWSqrd);
	half sqrYDist = yDist*yDist;
	half insideFadeOutFrom = 0.05 + 0.95*sqrYDist; //0.05 to 1
	half insideFadeOutTo = 0.5 + 0.6*sqrYDist; //0.5 to 1.1
	opacity = opacity * saturate((sqrDist - insideFadeOutFrom) / (insideFadeOutTo - insideFadeOutFrom));

	half a = opacity * (1-invWSqrd);
	i.clr.w = saturate(a);

	half colMod = 1 + a - i.clr.w;
	i.clr.x = i.clr.x * colMod;
	i.clr.y = i.clr.y * colMod;
	i.clr.z = i.clr.z * colMod;

	return saturate(i.clr);

}
ENDCG
				
			}
		} 
	}
}
