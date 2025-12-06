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
	half effectStrength = i.clr.w;
	half3 targetColor = i.clr.xyz;
		//map screen pos to level tex coord
	//return warpNoise(i.scrPos, 0.25);

	//get avg diff
	half4 thisCol = tex2D(_GrabTexture, i.scrPos);
	half4 sum = half4(0, 0, 0, 0);
	half4 realSum = half4(0, 0, 0, 0);
	half weight = 1;
	half totalWeight = 0;
	for (int c = 1; c <= 5; c++) {
		for (int a = 0; a < 2; a++) {
			for (int b = 0; b < 2; b++) {
				half2 offset = ((c&1)
					? half2((a|b) + (a&b) - 1, a - b) //00, 10, 01, 11 => -1, -1, 1, 1; -1, 1, 1, -1
					: half2(b + b - 1, (a^b) + (a^b) - 1)) //00, 10, 01, 11 => -1, 0, 0, 1; 0, 1, -1, 0
					* c * 0.003;
				half4 newCol = tex2D(_GrabTexture, i.scrPos + offset);
				sum = sum + abs(newCol - thisCol) * weight;
				realSum = realSum + (newCol - thisCol) * weight;
			}
		}
		totalWeight = totalWeight + weight * 4;
		weight = weight * 0.8;
	}

	totalWeight = 1 / totalWeight;
	sum = sum * totalWeight;
	realSum = realSum * totalWeight;

	//return lerp(thisCol, i.clr, sum);
	//return sum;
	
	//desired lerp factors
	//1. warpNoise, duh
	//2. changing from light to dark
	//3. the lightness of the pixel
	half3 thisCol3 = thisCol.xyz;
	half3 sum3 = sum.xyz;
	half3 realSum3 = realSum.xyz;

	half3 noise = warpNoise(i.scrPos, 0.3 + 0.1 * saturate(0.5 * (sum.x + sum.y + sum.z))).xyz;
	half3 lerps = saturate(effectStrength * 20
		* noise
		* (half3(0.5,0.5,0.5) - 2 * realSum3)
		* thisCol3 * thisCol3// * (half3(0.5,0.5,0.5) + 0.5*thisCol3)
		* (half3(1,1,1) + 2 * sum3)
		);
	
	//desired color factors
	//1. the desired color (i.clr)
	//2. the opposite of the current color (same brightness)
	//3. the opposite of the changing color (same brightness)

	half3 mixedCol = 0.5 * (thisCol3 + sum3 * effectStrength * 50);

	//adjust mixedCol brightness
	float origMixedColSize = sqrt(mixedCol.x*mixedCol.x + mixedCol.y*mixedCol.y + mixedCol.z*mixedCol.z);
	mixedCol = 0.5 * mixedCol / origMixedColSize;

	//invert mixedCol, basically (red becomes cyan, green becomes magenta, etc.)
	mixedCol = half3(1.02 - mixedCol.y-mixedCol.z, 1.02 - mixedCol.x-mixedCol.z, 1.02 - mixedCol.x-mixedCol.y) * (half3(1.02,1.02,1.02) - noise * targetColor * 0.05);

	mixedCol = lerp(targetColor, mixedCol, saturate(noise * origMixedColSize));

	//return half4(mixedCol.x, mixedCol.y, mixedCol.z, 1);
	//return half4(lerps.x, lerps.y, lerps.z, 1);

	//make mixedCol just as bright as targetColor
	half targetBright = (targetColor.x*targetColor.x + targetColor.y*targetColor.y + targetColor.z*targetColor.z) * effectStrength;
	half mixedBright = mixedCol.x*mixedCol.x + mixedCol.y*mixedCol.y + mixedCol.z*mixedCol.z;
	mixedCol = saturate(mixedCol * sqrt(targetBright / mixedBright));

	//return half4(mixedCol.x, mixedCol.y, mixedCol.z, 1);

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
