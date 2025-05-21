Shader "Nature/Terrain/TerrBlend_River" {
Properties {
	_SnowAdd("Add_snow_peak", Range(-0.93, 0.43)) = 0
	_SnowAdd2("Add_snow_field", Range(0, 1)) = 0
	_Color ("Main Color", Color) = (1,1,1,1)
	_ColorGrass0 ("Grass0", Color) = (1,1,1,1)
	_ColorGrass1 ("Grass1", Color) = (1,1,1,1)
	_ColorGrass2 ("Grass2", Color) = (1,1,1,1)
	_ColorGrass3 ("Grass_snow", Color) = (1,1,1,1)
	_OldGrass("OldGrass", Range(3, 200)) = 14
	_OldGrass2("OldGrass2", Range(3, 200)) = 14
	_ColorStones1 ("Stones1", Color) = (1,1,1,1)

	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.01, 0.2)) = 0.01
	
	[HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
	_Mask1 ("Mask1 (RGBA)", 2D) = "red" {}
	[HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "black" {}
	[HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "black" {}
	[HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "black" {}
	[HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
	[HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "bump" {}
	[HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "bump" {}
	[HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "bump" {}
	[HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "bump" {}
	[HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
	[HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
	
	_ColorTex ("ColorMap (RGB)", 2D) = "black" {}
	_Normalmap ("Normalmap (RGB)", 2D) = "white" {}
	_Tiling ("Tiling", Range (0.01, 80)) = 0.05
	
	_HeightSplatAll ("Grass(R) Cliff(G) Stones(B) Snow(a)", 2D) = "black" {}
	_Parallax ("Height", Range (0.005, 0.08)) = 0.02
	
	_Cube ("Reflection Cubemap", Cube) = "" {}
	_ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
}
	
SubShader {
	Tags {
		"Queue" = "Geometry-100"
		"RenderType" = "Opaque"
	}

CGPROGRAM
#pragma surface surf BlinnPhong vertex:vert fullforwardshadows
#pragma target 3.5


void vert (inout appdata_full v)
{
	v.tangent.xyz = cross(v.normal, float3(0,0,1));
	v.tangent.w = -1;
}

sampler2D _Mask1;
sampler2D _Control;
sampler2D _Splat0,_Splat1,_Splat2,_Splat3;
sampler2D _Normal0,_Normal1,_Normal2,_Normal3;

float _Parallax;

struct Input {
	float2 uv_Mask1 : TEXCOORD0;

	float2 uv_Splat0 : TEXCOORD1;
	float2 uv_Splat1 : TEXCOORD2;
	float2 uv_Splat2 : TEXCOORD3;
	float2 uv_Splat3 : TEXCOORD4;
	float3 worldPos;
	float3 worldNormal;
	float _Parallax;
	float3 worldRefl;
	float3 viewDir;

	INTERNAL_DATA
};


		fixed4 _Color;
		fixed4 _ColorGrass1;
		fixed4 _ColorGrass0;
		fixed4 _ColorGrass2;
		fixed4 _ColorGrass3;
		fixed4 _ColorStones1;
		half _OldGrass;
		half _OldGrass2;
		half _StoneAdd;
		half _GrassAdd;
		half _SnowAdd;
		half _SnowAdd2;
		half _Shininess;
		sampler2D _ColorTex;
		half _Tiling;
		sampler2D _HeightSplatAll;
		samplerCUBE _Cube;

void surf (Input IN, inout SurfaceOutput o) {

	o.Normal = float3(0, 0, 1);
	float3 n = WorldNormalVector(IN, o.Normal);
	float3 projNormal = saturate(pow(n * 1.4, 30));
	float2 invertY = float2(1, -1);

			half4 h = tex2D (_HeightSplatAll, IN.uv_Splat3).a;
			float2 offset = ParallaxOffset (h, _Parallax, IN.viewDir);
			IN.uv_Splat3 += offset*5;

			
			half4 hgrass = tex2D(_HeightSplatAll, IN.uv_Splat0).r;
			float2 offset1 = ParallaxOffset(hgrass, _Parallax, IN.viewDir);
			IN.uv_Splat0 += offset1 * 2;

			half4 h2 = tex2D (_HeightSplatAll, IN.uv_Splat1).g;
			float2 offset2 = ParallaxOffset (h2, _Parallax, IN.viewDir);
			IN.uv_Splat1 += offset2*5;
			
			half4 h3 = tex2D (_HeightSplatAll, IN.uv_Splat2).b;
			float2 offset3 = ParallaxOffset (h3, _Parallax, IN.viewDir);
			IN.uv_Splat2 += offset3*2;

			half4 ColorTex = tex2D (_ColorTex, IN.uv_Mask1);
			half4 MaskTex = tex2D (_Mask1, IN.uv_Mask1);
			half4 ControlTex = tex2D (_Control, IN.uv_Mask1);

			//-------------------------------------------------------
			// SIDE X
			float4 x = tex2D(_Splat1, frac(IN.worldPos.zy / _Tiling));
			float4 x_n = tex2D(_Normal1, frac(IN.worldPos.zy / _Tiling));
			// TOP / BOTTOM
			float4 y = tex2D(_Splat1, frac(IN.worldPos.zx / _Tiling));
			float4 y_n = tex2D(_Normal1, frac(IN.worldPos.zx / _Tiling));
			// SIDE Z    
			float4 z = tex2D(_Splat1, frac(IN.worldPos.xy / _Tiling));
			float4 z_n = tex2D(_Normal1, frac(IN.worldPos.xy / _Tiling));
			//-------------------------------------------------------

			half4 Detail0 = tex2D (_Splat0, IN.uv_Splat0);
			half4 Detail0_dots = tex2D (_Splat0, IN.uv_Splat0/2);
			half4 Detail1 = tex2D (_Splat1, IN.worldPos.zy / _Tiling);
			half4 Detail2 = tex2D (_Splat2, IN.uv_Splat2);
			half4 Detail2_2 = tex2D (_Splat2, IN.uv_Splat2);
			
			half4 Random_mask_1 = tex2D (_Splat2, IN.uv_Mask1);
			half4 Detail2_3 = tex2D (_Splat1, IN.uv_Splat2);
			half4 Detail3 = tex2D (_Splat3, IN.uv_Splat3);

			//Grass -----------------------------------------------
			float4 textureGrassWhite = tex2D (_Splat0, IN.uv_Splat0) * _ColorGrass1;
			float4 textureGrass0 = tex2D (_Splat0, IN.uv_Splat0) * _ColorGrass0;
			float4 textureGrass1 = tex2D (_Splat0, IN.uv_Splat0) * _ColorGrass1 * ColorTex;
			float4 textureGrass2 = tex2D (_Splat2, IN.uv_Splat0) * _ColorGrass2 ;
			float4 textureGrass3 = tex2D (_Splat3, IN.uv_Splat0) * _ColorGrass3;
			//Stones ----------------------------------------------
			float4 textureStones1 = tex2D (_Splat2, IN.uv_Splat2) * _ColorStones1;
			float4 textureStones2 = tex2D (_Splat2, IN.uv_Splat2);
			float4 textureStones3 = tex2D (_Splat3, IN.uv_Splat2);
			float4 textureStones4 = tex2D (_Splat2, IN.uv_Splat2);
			float4 textureStones5 = tex2D (_Splat2, IN.uv_Splat2) * ColorTex;

			float a_white = MaskTex.r * ((Detail0_dots.a)-Random_mask_1-Random_mask_1+0.13);
			float a00 = ((MaskTex.r * (Detail0.a) * ColorTex.a +MaskTex.a*0.5+MaskTex.b+MaskTex.b+MaskTex.b)+(((Random_mask_1*ColorTex.a)*1.5)-Random_mask_1.a*25));
			float a0 = MaskTex.r* MaskTex.r;
			float a1 = MaskTex.g + ControlTex.g;
			float a2 = ((MaskTex.b - 1 - MaskTex.g - MaskTex.r*2-MaskTex.r - MaskTex.a)+ColorTex.a);
			//water_1
			float a2_water = ((MaskTex.b*3-MaskTex.a*5-MaskTex.r*5-MaskTex.g)-MaskTex.r*50)*5;
			//SNOW_TOP
			float a3 = (((MaskTex.a/2 -_SnowAdd*1.6 + Random_mask_1.a + ControlTex.a-MaskTex.g-MaskTex.r-_GrassAdd-MaskTex.g*50+Random_mask_1-ControlTex.r)
					+Random_mask_1.a*0.8)*Random_mask_1.a*4+(1-Random_mask_1.a*4))-ColorTex.a*10;
			float a4 = 1-MaskTex.r;
			//SNOW_TOP_DUST
			float a5 = (MaskTex.a/2 -_SnowAdd*1.6 + Random_mask_1.a + ControlTex.a-MaskTex.r-_GrassAdd+Random_mask_1)*Random_mask_1.a*80*Random_mask_1.a*(Detail0);

			half4 HeightSplatTex1 = tex2D (_HeightSplatAll, IN.uv_Splat0).r*1.4;
			half4 HeightSplatTex2 = tex2D (_HeightSplatAll, IN.uv_Splat1).g;
			half4 HeightSplatTex3 = tex2D (_HeightSplatAll, IN.uv_Splat2).b*1.2 +(1.14/20)*5;
			half4 HeightSplatTex4 = tex2D (_HeightSplatAll, IN.uv_Splat3 - offset).a -1.39 +(1.14/20)*1.4 *Random_mask_1.a*Random_mask_1.a*Random_mask_1.a;

			half4 HeightSplatGrass0 = tex2D (_HeightSplatAll, IN.uv_Splat0).r * MaskTex.b;
			half4 HeightSplatGrass1 = tex2D (_HeightSplatAll, IN.uv_Splat0).r*2 * (Detail0.a)+ _ColorGrass1.a;
			half4 HeightSplatGrass2 = tex2D (_HeightSplatAll, IN.uv_Splat1).r + 0.72+ _ColorGrass2.a;
			half4 HeightSplatGrass3 = tex2D (_HeightSplatAll, IN.uv_Splat1).r + 0.72+ _ColorGrass3.a;

			half4 HeightSplatStones1 = tex2D (_HeightSplatAll, IN.uv_Splat1).b+ _ColorStones1.a;
			half4 HeightSplatStones2 = tex2D (_HeightSplatAll, IN.uv_Splat2).b;
			half4 HeightSplatStones3 = tex2D (_HeightSplatAll, IN.uv_Splat3).b;
			float mgrass = (max(max(max(HeightSplatGrass0.rgb + a00, HeightSplatGrass1.rgb + a0), HeightSplatGrass2.rgb + a1*8), HeightSplatGrass3.rgb + a2*5)) - 0.15;

			float mStones = (max(max(HeightSplatStones1.rgb + a1, HeightSplatStones2.rgb + a2), HeightSplatStones3.rgb + a3)) - 0.01;

			float g_white = max(HeightSplatGrass0.rgb + a_white * _OldGrass2 - mgrass, 0);
			float g00 = max(HeightSplatGrass0.rgb + a00 * _OldGrass - mgrass, 0);
			float g0 = max(HeightSplatGrass1.rgb + a0 - mgrass, 0); 
		    float g1 = max(HeightSplatGrass2.rgb + a1 * 8 - mgrass, 0) + ControlTex.g;
		    float g2 = max(HeightSplatGrass2.rgb + a2 - mgrass, 0);
		    float g2_snow = max(HeightSplatGrass1.rgb + a5 *0.73 - mgrass, 0);

		    float s0 = max(HeightSplatStones1.rgb + a1 - mStones, 0);
		    float s1 = max(HeightSplatStones2.rgb + a2 - mStones, 0);
		    float s2 = max(HeightSplatStones3.rgb + a3 - mStones, 0);
		    float s3 = max(HeightSplatStones2.rgb + a4*3-a0-a1+a2-MaskTex.a*5 - mStones, 0);

			float4 GrassWhiteTex = textureGrassWhite;
			float4 Grass0Tex = textureGrass0;
		    float4 Grass1Tex = textureGrass1;
		    float4 Grass2Tex = textureGrass2;
		    float4 Grass3Tex = textureGrass3;

		    float4 Stones1Tex = textureStones1;
		    float4 Stones2Tex = textureStones2;
		    float4 Stones3Tex = textureStones3;
		    float4 Stones4Tex = textureStones4;
			float4 Stones5Tex = textureStones5;

		    fixed4 texGrass = (GrassWhiteTex * g_white + Grass0Tex * g00 +  Grass1Tex * g0 + Grass2Tex * g1 + Grass3Tex * g2_snow) / (g_white + g00 + g0 + g1 + g2_snow);

		    fixed4 texStones = (Stones1Tex * s0 + Stones2Tex * s1 + Stones3Tex * s2 + Stones4Tex * s3) / (s0 + s1 + s2 + s3);
			
		    float ma = (max(max(max(HeightSplatTex1.rgb + a0, HeightSplatTex2.rgb + a1), HeightSplatTex3.rgb + a2), HeightSplatTex4.rgb + a3)) - 0.01;

		    float b0 = max(HeightSplatTex1.rgb + a0 - ma, 0)*5;
		    float b1 = max(HeightSplatTex2.rgb + a1 - ma, 0)*50;
		    float b2 = max(HeightSplatTex3.rgb + a2 - ma, 0)*4;
		    float b3 = max(HeightSplatTex4.rgb + a3 - ma, 0)*50;
		    
			float b4 = max(HeightSplatTex3.rgb + ColorTex.a/9 +g0+ g1 - ma, 0);

			float b4_snow = _SnowAdd2 * max(HeightSplatTex2.rgb +((MaskTex.b*5*Random_mask_1.a*10)+(MaskTex.b*5*ControlTex.a))+(MaskTex.a+ColorTex.a-MaskTex.r)*0.77 - ma, 0)*(MaskTex.r+MaskTex.a);
			float b5 = (max(HeightSplatTex2.rgb + (a2_water)- ma, 0));
			float b6 = max(HeightSplatTex3.rgb - ma, 0);
		    
		    float Grass_snow = _SnowAdd2 * max(HeightSplatStones2.rgb*1.6 + (b4 - b0-MaskTex.b*100)*ControlTex.r - ma, 0);
		    float Ice_snow = _SnowAdd2 * max(HeightSplatStones2.rgb*MaskTex.a*20 + MaskTex.b*5*ControlTex.a-Random_mask_1*50-MaskTex.r*100 - ma, 0);
		    
		    float4 texture0 = texGrass;
		    float4 texture1 = tex2D (_Splat1, IN.worldPos.zx / _Tiling);
		    float4 texture2 = texStones;
		    float4 texture3 = Detail3;
			float4 texture4 = textureStones4;
			float4 texture5 = textureStones5;

		    fixed4 tex = (texture0 * b0 + texture1 * b1 + texture2 * b2 + texture3 * (b3+b4_snow+Grass_snow+Ice_snow) + texture4 * b4 + texture5 * b5 + texture2 * b6) / (b0 + b1 + b2 + (b3+b4_snow) + b4 + b5 + b6+Ice_snow);

		    tex = tex;

			texture0 = tex2D (_Normal0, IN.uv_Splat0);
			texture1 = tex2D (_Normal1, IN.worldPos.zx / _Tiling);
			texture2 = tex2D (_Normal2, IN.uv_Splat2);
			texture3 = tex2D (_Normal3, IN.uv_Splat3);
			float4 mixnormal = (texture0 * b0 + texture1 * b1 + texture2 * b2 + texture3 * (b3+b4_snow+b4*Random_mask_1.a*3+Ice_snow) + texture2 * b4 + texture2 * b5) / (b0 + b1 + b2 + (b3+b4_snow+b4*Random_mask_1.a*3) + b4 + b5+Ice_snow);

			o.Normal = normalize(UnpackNormal(mixnormal)*1.5);

			o.Albedo = z.rgb;
			o.Albedo = lerp(o.Albedo, x.rgb, projNormal.x);
			o.Albedo = lerp(o.Albedo, y.rgb, projNormal.y);
			o.Albedo = lerp(tex.rgb, o.Albedo, a1)*_Color;

			o.Gloss = 1 * MaskTex.a - MaskTex.r +0.06;
			o.Alpha = _Color.a;
			o.Specular = _Shininess;
			
	float3 worldRefl = WorldReflectionVector (IN, UnpackNormal(mixnormal));
	fixed4 reflcol_dirt = texCUBE (_Cube, worldRefl)*10;
	reflcol_dirt *= ColorTex.a;

	reflcol_dirt *= b2;
	reflcol_dirt *= b1;

	reflcol_dirt *= tex2D(_ColorTex, IN.uv_Mask1);
	reflcol_dirt *= tex2D(_Mask1, IN.uv_Mask1).a;
	
	fixed4 reflcol_snow = texCUBE (_Cube, worldRefl)*40;
	reflcol_snow *= (tex2D(_Splat3, IN.uv_Splat3/1.5).a * tex2D(_Splat3, IN.uv_Splat3/2 + _Time/800).a);
	reflcol_snow *= b3;
	reflcol_snow += (tex2D(_Splat3, IN.uv_Splat3/1.5).a * tex2D(_Splat3, IN.uv_Splat3/2 + _Time/800).a)*s3;
	reflcol_snow += (tex2D(_Splat3, IN.uv_Splat3/1.5).a * tex2D(_Splat3, IN.uv_Splat3/2 + _Time/800).a)*s3;
	reflcol_snow += (tex2D(_Splat3, IN.uv_Splat3/1.5).a * tex2D(_Splat3, IN.uv_Splat3/2 + _Time/800).a)*g1; 
	
	reflcol_snow += b2;
	reflcol_snow += s3/15;

	fixed4 reflcol_stone = texCUBE (_Cube, worldRefl)/1;
		reflcol_stone *= b6;
		reflcol_stone *= (tex2D(_Splat3, IN.uv_Splat3/1.5).a * tex2D(_Splat3, IN.uv_Splat3/2 + _Time/800).a);
	fixed4 reflcol_stone_2 = texCUBE (_Cube, worldRefl)/50;
		reflcol_stone_2 *= s3*2;
	o.Emission = reflcol_dirt.rgb + reflcol_snow.rgb + (reflcol_stone.rgb);
}
ENDCG

 
}

//Dependency "AddPassShader" = "Hidden/Nature/Terrain/TerrSuperBlendSpec AddPass"
Dependency "BaseMapShader" = "Specular"

Fallback "Nature/Terrain/Diffuse"
}