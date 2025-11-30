//Simple parallax shader by TheLazyCowboy1

Shader "TheLazyCowboy1/WarpNoise" //Unlit Transparent Vertex Colored Additive 
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
		Blend Off
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
		
		GrabPass { }

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

#if defined(SHADER_API_PSSL)
sampler2D _GrabTexture;
#else
sampler2D _GrabTexture : register(s0);
#endif

sampler2D _MainTex;
uniform float2 _MainTex_TexelSize;

//sampler2D _LevelTex;
//sampler2D _PreLevelColorGrab;
//sampler2D _SlopedTerrainMask;
sampler2D TheLazyCowboy1_ColoredNoiseTex;

//uniform float4 _spriteRect;
//uniform float2 _screenSize;

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
    float2 scrPos : TEXCOORD1;
    float4 clr : COLOR;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.scrPos = ComputeScreenPos(o.pos);
    o.clr = v.color;
    return o;
}

inline half4 warpNoise(half2 pos, half severity) {
	half2 pos1 = pos * (1 - severity * 2);
	half2 pos2 = half2(1, 1) - pos1;
	half4 noise1 = tex2D(TheLazyCowboy1_ColoredNoiseTex, pos1);
	half4 noise2 = tex2D(TheLazyCowboy1_ColoredNoiseTex, pos2);
	pos = pos1;
	pos.x = pos.x + severity;
	pos.y = pos.y + severity;
	pos.x = pos.x + noise1 * severity;
	pos.y = pos.y + noise2 * severity;
	return tex2D(TheLazyCowboy1_ColoredNoiseTex, pos);
}
inline half4 sqr(half4 a) {
	return a*a;
}

half4 frag (v2f i) : SV_Target
{
	//i.clr = float4(1, 0.8, 0.5, 0.5); //TEMPORARILY OVERRIDE THE COLOR FOR TESTING PURPOSES
	half effectStrength = 20 * i.clr.w;
	half3 targetColor = i.clr.xyz;
		//map screen pos to level tex coord
	//return warpNoise(i.scrPos, 0.25);

	//get avg diff
	half4 thisCol = tex2D(_GrabTexture, i.scrPos);
	half4 sum = half4(0, 0, 0, 0);
	half4 realSum = half4(0, 0, 0, 0);
	for (int a = -1; a <= 1; a++) {
		for (int b = -1; b <= 1; b++) {
			if (a != 0 || b != 0) {
				for (int c = 1; c <= 2; c++) {
					half2 offset = half2(a, b) * c * ((a == 0 || b == 0) ? 1 : 0.7071068) * 0.005;
					half4 newCol = tex2D(_GrabTexture, i.scrPos + offset);
					sum = sum + abs(newCol - thisCol);
					realSum = realSum + newCol - thisCol;
				}
			}
		}
	}
	sum = sum * 0.125 * 0.5;
	realSum = realSum * 0.125 * 0.5;
	//sum = half4(1, 1, 1, 1) - saturate(3 * sum * warpNoise(i.scrPos, 0.4 * saturate(3 * (sum.x + sum.y + sum.z + sum.w))));
	//return thisCol * sum;
	//sum = saturate(3 * sum * warpNoise(i.scrPos, 0.3 + 0.1 * saturate(3 * (sum.x + sum.y + sum.z + sum.w))));
	//sum = saturate(4 * (sum * warpNoise(i.scrPos, 0.3 + 0.1 * saturate(3 * (sum.x + sum.y + sum.z + sum.w))) * (1 - 2 * realSum)));

	//return customLerp(thisCol, i.clr, sum);
	
	//desired lerp factors
	//1. warpNoise, duh
	//2. changing from light to dark
	//3. the lightness of the pixel
	half3 thisCol3 = thisCol.xyz;
	half3 sum3 = sum.xyz;
	half3 realSum3 = realSum.xyz;

	half3 noise = warpNoise(i.scrPos, 0.2 + 0.25 * saturate(1.5 * 0.5 * (sum.x - sum.y + sum.z - sum.w))).xyz;
	half3 lerps = saturate(effectStrength
		* (noise - half3(0.2,0.2,0.2))
		* (0.5 - 4 * realSum3)
		* thisCol3 * thisCol3// * (half3(0.5,0.5,0.5) + 0.5*thisCol3)
		);
	
	//desired color factors
	//1. the desired color (i.clr)
	//2. the opposite of the current color (same brightness)
	//3. the opposite of the changing color (same brightness)

	half3 mixedCol = 0.25 * (thisCol3 + saturate(4 * sum3 * effectStrength));
	//half4 mixedCol = 0.5 * saturate(3*sum);
	mixedCol = half3(1 - mixedCol.y-mixedCol.z, 1 - mixedCol.x-mixedCol.z, 1 - mixedCol.x-mixedCol.y) * (half3(1,1,1) - noise * targetColor * 0.1);
	//mixedCol = 0.5 * (i.clr + mixedCol);
	mixedCol = lerp(targetColor, mixedCol, noise * 0.5);

	half3 ret = lerp(thisCol3, mixedCol, lerps);
	//ret.w = 1;
	return half4(ret.x, ret.y, ret.z, 1);

	//half3 lerps = half3(0.5 * (sum.y + sum.z), 0.5 * (sum.x + sum.z), 0.5 * (sum.x + sum.y));
	//half4 lerps = sum;
	//half3 lerps = half3(sum.x * (0.5 + 0.5 * thisCol.x), sum.y * (0.5 + 0.5 * thisCol.y), sum.z * (0.5 + 0.5 * thisCol.z));

	//return half4(lerp(thisCol.x, 1, lerps.x), lerp(thisCol.y, 1, lerps.y), lerp(thisCol.z, 1, lerps.z), 1);

}
ENDCG
				
			}
		} 
	}
}
