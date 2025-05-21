// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TriForge/Fantasy Forest/SimpleWater"
{
	Properties
	{
		[Normal]_NormalMap("Normal Map", 2D) = "bump" {}
		_NormalMapScale("Normal Map Scale", Range( 0 , 10)) = 1
		_NormalIntensity("Normal Intensity", Range( 0 , 5)) = 0
		_WaterSmoothness("Water Smoothness", Range( 0 , 1)) = 0.95
		_WaterSpecularity("Water Specularity", Range( 0 , 1)) = 1
		_FoamAmount("Foam Amount", Range( 0 , 2)) = 0
		_FoamContrast("Foam Contrast", Range( 0 , 5)) = 1.5
		_FoamTexture("Foam Texture", 2D) = "white" {}
		_FoamScale("Foam Scale", Float) = 3
		_RefractionIntensity("Refraction Intensity", Range( 0 , 5)) = 5
		_Float3("Float 3", Float) = 0
		_Float6("Float 6", Range( 0 , 2)) = 0
		_DeepColor("Deep Color", Color) = (0.08940016,0.2804352,0.3867925,0)
		_ShallowColor("Shallow Color", Color) = (0.2282841,0.4686714,0.509434,0)
		_FresnelOpacityStrength("Fresnel Opacity Strength", Range( 0 , 1)) = 0.1
		_FoamOpacity("Foam Opacity", Range( 0 , 1)) = 1
		_FresnelScale("Fresnel Scale", Range( 0 , 10)) = 0
		_FresnelPower("Fresnel Power", Range( 0 , 25)) = 0
		_WaterNormalSpeed("Water Normal Speed", Float) = 1
		_NormalDetailIntensity("Normal Detail Intensity", Range( 0 , 5)) = 1
		_NormalDetailScale("Normal Detail Scale", Range( 0 , 10)) = 1
		[HideInInspector] __dirty( "", Int ) = 1
		[Header(Forward Rendering Options)]
		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections("Reflections", Float) = 1.0
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		GrabPass{ }
		CGPROGRAM
		#include "UnityPBSLighting.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma shader_feature _SPECULARHIGHLIGHTS_OFF
		#pragma shader_feature _GLOSSYREFLECTIONS_OFF
		#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
		#else
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
		#endif
		#pragma surface surf StandardCustomLighting alpha:fade keepalpha exclude_path:deferred nolightmap  nodynlightmap nodirlightmap vertex:vertexDataFunc 
		struct Input
		{
			float4 screenPos;
			float3 worldPos;
			float eyeDepth;
			float3 worldNormal;
			INTERNAL_DATA
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
		uniform sampler2D _NormalMap;
		uniform float _WaterNormalSpeed;
		uniform float _NormalMapScale;
		uniform float _NormalIntensity;
		uniform float _NormalDetailScale;
		uniform float _NormalDetailIntensity;
		uniform float _RefractionIntensity;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _WaterSpecularity;
		uniform float _WaterSmoothness;
		uniform float4 _ShallowColor;
		uniform float4 _DeepColor;
		uniform float _Float3;
		uniform float _Float6;
		uniform float _FresnelScale;
		uniform float _FresnelPower;
		uniform float _FresnelOpacityStrength;
		uniform float _FoamAmount;
		uniform float _FoamContrast;
		uniform sampler2D _FoamTexture;
		uniform float _FoamScale;
		uniform float _FoamOpacity;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			o.eyeDepth = -UnityObjectToViewPos( v.vertex.xyz ).z;
		}

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float4 screenColor419 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,ase_screenPosNorm.xy/ase_screenPosNorm.w);
			float temp_output_443_0 = ( 1.0 * 0.6 * _WaterNormalSpeed );
			float temp_output_11_0_g10 = ( temp_output_443_0 * _Time.y );
			float3 ase_worldPos = i.worldPos;
			float2 panner44 = ( 1.0 * _Time.y * float2( 0.02,0.1 ) + (( ase_worldPos / ( 3.0 * _NormalMapScale ) )).xz);
			float2 temp_output_32_0_g10 = panner44;
			float2 panner5_g10 = ( temp_output_11_0_g10 * float2( 0.1,0.1 ) + temp_output_32_0_g10);
			float temp_output_33_0_g10 = _NormalIntensity;
			float2 panner16_g10 = ( temp_output_11_0_g10 * float2( -0.1,-0.1 ) + ( temp_output_32_0_g10 + float2( 0.418,0.355 ) ));
			float2 panner19_g10 = ( temp_output_11_0_g10 * float2( -0.1,0.1 ) + ( temp_output_32_0_g10 + float2( 0.865,0.148 ) ));
			float2 panner23_g10 = ( temp_output_11_0_g10 * float2( 0.1,-0.1 ) + ( temp_output_32_0_g10 + float2( 0.651,0.752 ) ));
			float temp_output_11_0_g9 = ( temp_output_443_0 * _Time.y );
			float2 panner50 = ( 1.0 * _Time.y * float2( 0.1,0.06 ) + (( ase_worldPos / ( 9.0 * _NormalMapScale ) )).xz);
			float2 temp_output_32_0_g9 = panner50;
			float2 panner5_g9 = ( temp_output_11_0_g9 * float2( 0.1,0.1 ) + temp_output_32_0_g9);
			float temp_output_33_0_g9 = _NormalIntensity;
			float2 panner16_g9 = ( temp_output_11_0_g9 * float2( -0.1,-0.1 ) + ( temp_output_32_0_g9 + float2( 0.418,0.355 ) ));
			float2 panner19_g9 = ( temp_output_11_0_g9 * float2( -0.1,0.1 ) + ( temp_output_32_0_g9 + float2( 0.865,0.148 ) ));
			float2 panner23_g9 = ( temp_output_11_0_g9 * float2( 0.1,-0.1 ) + ( temp_output_32_0_g9 + float2( 0.651,0.752 ) ));
			float temp_output_11_0_g11 = ( 2.1 * _Time.y );
			float2 panner453 = ( 1.0 * _Time.y * float2( 0.02,0.1 ) + (( ase_worldPos / ( ( 0.5 * _NormalDetailScale ) * _NormalMapScale ) )).xz);
			float2 temp_output_32_0_g11 = panner453;
			float2 panner5_g11 = ( temp_output_11_0_g11 * float2( 0.1,0.1 ) + temp_output_32_0_g11);
			float temp_output_33_0_g11 = _NormalDetailIntensity;
			float2 panner16_g11 = ( temp_output_11_0_g11 * float2( -0.1,-0.1 ) + ( temp_output_32_0_g11 + float2( 0.418,0.355 ) ));
			float2 panner19_g11 = ( temp_output_11_0_g11 * float2( -0.1,0.1 ) + ( temp_output_32_0_g11 + float2( 0.865,0.148 ) ));
			float2 panner23_g11 = ( temp_output_11_0_g11 * float2( 0.1,-0.1 ) + ( temp_output_32_0_g11 + float2( 0.651,0.752 ) ));
			float3 WaterNormal274 = BlendNormals( BlendNormals( ( ( ( UnpackScaleNormal( tex2D( _NormalMap, panner5_g10 ), temp_output_33_0_g10 ) + UnpackScaleNormal( tex2D( _NormalMap, panner16_g10 ), temp_output_33_0_g10 ) ) + ( UnpackScaleNormal( tex2D( _NormalMap, panner19_g10 ), temp_output_33_0_g10 ) + UnpackScaleNormal( tex2D( _NormalMap, panner23_g10 ), temp_output_33_0_g10 ) ) ) * 1.0 ) , ( ( ( UnpackScaleNormal( tex2D( _NormalMap, panner5_g9 ), temp_output_33_0_g9 ) + UnpackScaleNormal( tex2D( _NormalMap, panner16_g9 ), temp_output_33_0_g9 ) ) + ( UnpackScaleNormal( tex2D( _NormalMap, panner19_g9 ), temp_output_33_0_g9 ) + UnpackScaleNormal( tex2D( _NormalMap, panner23_g9 ), temp_output_33_0_g9 ) ) ) * 1.0 ) ) , ( ( ( UnpackScaleNormal( tex2D( _NormalMap, panner5_g11 ), temp_output_33_0_g11 ) + UnpackScaleNormal( tex2D( _NormalMap, panner16_g11 ), temp_output_33_0_g11 ) ) + ( UnpackScaleNormal( tex2D( _NormalMap, panner19_g11 ), temp_output_33_0_g11 ) + UnpackScaleNormal( tex2D( _NormalMap, panner23_g11 ), temp_output_33_0_g11 ) ) ) * 0.2 ) );
			float4 temp_output_270_0 = ( ase_screenPosNorm + float4( ( WaterNormal274 * _RefractionIntensity ) , 0.0 ) );
			float4 screenColor271 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,temp_output_270_0.xy/temp_output_270_0.w);
			float eyeDepth413 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, temp_output_270_0.xy ));
			float ifLocalVar414 = 0;
			if( eyeDepth413 > i.eyeDepth )
				ifLocalVar414 = 1.0;
			else if( eyeDepth413 < i.eyeDepth )
				ifLocalVar414 = 0.0;
			float4 lerpResult418 = lerp( screenColor419 , screenColor271 , ifLocalVar414);
			float4 Refraction281 = lerpResult418;
			SurfaceOutputStandardSpecular s394 = (SurfaceOutputStandardSpecular ) 0;
			s394.Albedo = float3( 0,0,0 );
			s394.Normal = WorldNormalVector( i , WaterNormal274 );
			s394.Emission = float3( 0,0,0 );
			float3 temp_cast_2 = (_WaterSpecularity).xxx;
			s394.Specular = temp_cast_2;
			s394.Smoothness = _WaterSmoothness;
			s394.Occlusion = 1.0;

			data.light = gi.light;

			UnityGI gi394 = gi;
			#ifdef UNITY_PASS_FORWARDBASE
			Unity_GlossyEnvironmentData g394 = UnityGlossyEnvironmentSetup( s394.Smoothness, data.worldViewDir, s394.Normal, float3(0,0,0));
			gi394 = UnityGlobalIllumination( data, s394.Occlusion, s394.Normal, g394 );
			#endif

			float3 surfResult394 = LightingStandardSpecular ( s394, viewDir, gi394 ).rgb;
			surfResult394 += s394.Emission;

			#ifdef UNITY_PASS_FORWARDADD//394
			surfResult394 -= s394.Emission;
			#endif//394
			float screenDepth316 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth316 = ( screenDepth316 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _Float3 );
			float clampResult325 = clamp( ( distanceDepth316 * _Float6 ) , 0.0 , 1.0 );
			float4 lerpResult329 = lerp( _ShallowColor , _DeepColor , saturate( clampResult325 ));
			float4 Color362 = lerpResult329;
			float4 ColorBlend432 = ( float4( surfResult394 , 0.0 ) + Color362 );
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_worldTangent = WorldNormalVector( i, float3( 1, 0, 0 ) );
			float3 ase_worldBitangent = WorldNormalVector( i, float3( 0, 1, 0 ) );
			float3x3 ase_tangentToWorldFast = float3x3(ase_worldTangent.x,ase_worldBitangent.x,ase_worldNormal.x,ase_worldTangent.y,ase_worldBitangent.y,ase_worldNormal.y,ase_worldTangent.z,ase_worldBitangent.z,ase_worldNormal.z);
			float fresnelNdotV384 = dot( mul(ase_tangentToWorldFast,WaterNormal274), ase_worldViewDir );
			float fresnelNode384 = ( 0.0 + _FresnelScale * pow( max( 1.0 - fresnelNdotV384 , 0.0001 ), _FresnelPower ) );
			float lerpResult441 = lerp( 1.0 , fresnelNode384 , _FresnelOpacityStrength);
			float ColorOpacity365 = lerpResult329.a;
			float FresnelOpacity402 = ( saturate( lerpResult441 ) * ColorOpacity365 );
			float4 lerpResult382 = lerp( Refraction281 , ColorBlend432 , FresnelOpacity402);
			float4 RefractionBlend433 = lerpResult382;
			float4 color422 = IsGammaSpace() ? float4(0.8679245,0.8679245,0.8679245,0) : float4(0.7254258,0.7254258,0.7254258,0);
			float screenDepth182 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth182 = saturate( abs( ( screenDepth182 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _FoamAmount ) ) );
			float saferPower188 = abs( ( 1.0 - distanceDepth182 ) );
			float2 panner197 = ( 1.0 * _Time.y * float2( 0.1,0 ) + (( ase_worldPos / _FoamScale )).xz);
			float2 panner194 = ( 1.0 * _Time.y * float2( 0.2,0 ) + (( ase_worldPos / _FoamScale )).xz);
			float2 panner466 = ( 1.0 * _Time.y * float2( 0.15,0.08 ) + (( ase_worldPos / 1.0 )).xz);
			float2 panner470 = ( 1.0 * _Time.y * float2( 0.012,0.04 ) + (( ase_worldPos / 5.0 )).xz);
			float4 tex2DNode475 = tex2D( _FoamTexture, panner470 );
			float saferPower476 = abs( tex2DNode475.g );
			float FoamFinal420 = saturate( ( ( ( ( ( saturate( pow( saferPower188 , _FoamContrast ) ) - tex2D( _FoamTexture, panner197 ).r ) - saturate( pow( ( tex2D( _FoamTexture, panner194 ).g * 0.88 ) , 0.65 ) ) ) - ( tex2D( _FoamTexture, panner466 ).r * 0.35 ) ) - pow( saferPower476 , 0.5 ) ) * 2.0 ) );
			float4 lerpResult424 = lerp( RefractionBlend433 , ( color422 + float4( surfResult394 , 0.0 ) ) , ( FoamFinal420 * _FoamOpacity ));
			float screenDepth256 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth256 = saturate( abs( ( screenDepth256 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( 0.5 ) ) );
			float saferPower258 = abs( distanceDepth256 );
			float WaterEdge428 = saturate( pow( saferPower258 , 1.5 ) );
			float4 lerpResult437 = lerp( RefractionBlend433 , lerpResult424 , WaterEdge428);
			c.rgb = lerpResult437.rgb;
			c.a = 1;
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
			o.Normal = float3(0,0,1);
		}

		ENDCG
	}
	Fallback "Diffuse"
}
/*ASEBEGIN
Version=18935
7;54;1920;965;6129.106;3479.936;8.945799;True;True
Node;AmplifyShaderEditor.CommentaryNode;168;-1883.531,539.155;Inherit;False;2826.004;1855.721;Comment;40;274;175;266;267;11;100;103;44;50;443;6;49;43;444;53;12;41;48;47;40;73;71;46;42;72;445;446;447;448;449;450;451;452;453;454;455;456;457;459;461;Normal;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;448;-1535.934,1983.554;Inherit;False;Constant;_Float7;Float 2;7;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;42;-1408.456,800.0391;Inherit;False;Constant;_Float2;Float 2;7;0;Create;True;0;0;0;False;0;False;3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;459;-1656.688,2138.036;Inherit;False;Property;_NormalDetailScale;Normal Detail Scale;21;0;Create;True;0;0;0;False;0;False;1;1.17;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;46;-1373.425,1466.793;Inherit;False;Constant;_Float1;Float 1;7;0;Create;True;0;0;0;False;0;False;9;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;72;-1711.766,1083.572;Inherit;False;Property;_NormalMapScale;Normal Map Scale;2;0;Create;True;0;0;0;False;0;False;1;1.15;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;71;-1259.791,796.759;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;47;-1157.046,1260.723;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.WorldPosInputsNode;40;-1444.45,644.411;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;458;-1327.688,2118.036;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;73;-1121.016,1440.472;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;6;-1839.496,1270.224;Inherit;True;Property;_NormalMap;Normal Map;1;1;[Normal];Create;True;0;0;0;False;0;False;None;437fcbf1eee883f4b8b194c794fa1a2b;True;bump;LockedToTexture2D;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;449;-1181.269,1971.274;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;41;-1256.911,646.326;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldPosInputsNode;450;-1365.928,1818.926;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleDivideOpNode;48;-873.3176,1390.048;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;451;-1178.389,1820.841;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;49;-679.366,1412.718;Inherit;False;True;False;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;444;-748.1498,1315.652;Inherit;False;Property;_WaterNormalSpeed;Water Normal Speed;19;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;445;-1561.509,1267.128;Inherit;False;NormalMap;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;53;-758.6761,1233.323;Inherit;False;Constant;_Float4;Float 4;4;0;Create;True;0;0;0;False;0;False;0.6;0.6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;43;-1090.258,641.6962;Inherit;False;True;False;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;263;-2388.434,-2508.96;Inherit;False;2468.436;1407.48;Comment;30;469;250;462;466;252;193;465;197;253;196;464;202;199;463;194;195;215;198;214;200;213;467;470;471;472;473;474;475;476;478;Foam Texture;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-776.9367,1146.421;Inherit;False;Constant;_WaterSpeed;Water Speed;4;0;Create;True;0;0;0;False;0;False;1;0.6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;44;-870.4365,643.6251;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.02,0.1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;50;-416.6439,1356.147;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.1,0.06;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-471.3009,828.6318;Inherit;False;Constant;_Float;Float;6;0;Create;True;0;0;0;False;0;False;1;0.8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;447;-338.5093,810.128;Inherit;False;445;NormalMap;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.ComponentMaskNode;452;-1011.736,1816.211;Inherit;False;True;False;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;446;-324.5093,1078.128;Inherit;False;445;NormalMap;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;103;-603.303,1000.823;Inherit;False;Property;_NormalIntensity;Normal Intensity;3;0;Create;True;0;0;0;False;0;False;0;3;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;443;-509.6422,1209.95;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;200;-2300.402,-2085.663;Inherit;False;Property;_FoamScale;Foam Scale;9;0;Create;True;0;0;0;False;0;False;3;1.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;213;-2336.148,-1953.533;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;401;1908.288,-1143.412;Inherit;False;1143.056;347.9071;Comment;6;319;316;324;323;325;336;Depth ;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;100;-468.6771,1125.966;Inherit;False;Constant;_Float0;Float 0;12;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;453;-791.9145,1818.14;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.02,0.1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;267;-122.347,791.584;Inherit;True;4WayChaos;-1;;10;c91857f78a432b948baf6cdc0c5f4513;0;5;32;FLOAT2;0,0;False;31;SAMPLER2D;0,0,0,0;False;29;FLOAT;0.2;False;33;FLOAT;1;False;10;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;456;-493.6121,1747.363;Inherit;False;Property;_NormalDetailIntensity;Normal Detail Intensity;20;0;Create;True;0;0;0;False;0;False;1;0.39;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;266;-123.6438,1059.149;Inherit;True;4WayChaos;-1;;9;c91857f78a432b948baf6cdc0c5f4513;0;5;32;FLOAT2;0,0;False;31;SAMPLER2D;0,0,0,0;False;29;FLOAT;0.2;False;33;FLOAT;1;False;10;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;214;-2059.116,-1903.405;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;262;-978.1631,-592.0611;Inherit;False;1190.497;301.6274;Comment;6;182;210;188;190;187;189;Foam Mask;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;319;1958.288,-1025.524;Inherit;False;Property;_Float3;Float 3;11;0;Create;True;0;0;0;False;0;False;0;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;455;-427.6121,1606.363;Inherit;False;445;NormalMap;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.BlendNormalsNode;175;280.6129,1019.386;Inherit;True;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.FunctionNode;454;-127.4274,1630.374;Inherit;True;4WayChaos;-1;;11;c91857f78a432b948baf6cdc0c5f4513;0;5;32;FLOAT2;0,0;False;31;SAMPLER2D;0,0,0,0;False;29;FLOAT;0.2;False;33;FLOAT;1;False;10;FLOAT;2.1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;187;-930.1631,-500.9594;Inherit;False;Property;_FoamAmount;Foam Amount;6;0;Create;True;0;0;0;False;0;False;0;1.313;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;198;-2338.434,-2262.793;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;215;-1897.975,-1908.115;Inherit;False;True;False;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;324;2138.058,-911.5043;Inherit;False;Property;_Float6;Float 6;12;0;Create;True;0;0;0;False;0;False;0;1.5;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;316;2158.057,-1093.412;Inherit;False;True;False;False;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;195;-1544.13,-2457.606;Inherit;True;Property;_FoamTexture;Foam Texture;8;0;Create;True;0;0;0;False;0;False;None;3ebf940de200d0946bcd96c41b54100a;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;467;-2294.141,-1502.674;Inherit;False;Constant;_Float10;Float 10;23;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;182;-644.3587,-542.0087;Inherit;False;True;True;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;457;539.4868,1358.966;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;199;-2061.403,-2212.664;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;323;2468.258,-989.5051;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;463;-2334.882,-1673.267;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PannerNode;194;-1503.13,-2048.606;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.2,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;474;-2290.804,-1184.712;Inherit;False;Constant;_Float11;Float 10;23;0;Create;True;0;0;0;False;0;False;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;292;1184,560;Inherit;False;1839.671;755.5186;Comment;14;417;416;415;414;413;271;419;418;281;270;269;293;294;275;Refraction;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;274;626.0599,1013.149;Inherit;False;WaterNormal;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ClampOpNode;325;2678.111,-989.2761;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;189;-929.3262,-384.4337;Inherit;False;Property;_FoamContrast;Foam Contrast;7;0;Create;True;0;0;0;False;0;False;1.5;3.51;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;202;-1900.26,-2217.373;Inherit;False;True;False;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WorldPosInputsNode;471;-2331.545,-1355.305;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SamplerNode;196;-1139.132,-2231.606;Inherit;True;Property;_TextureSample1;Texture Sample 1;24;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;464;-2057.851,-1623.14;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;210;-363.6125,-542.061;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;472;-2054.514,-1305.178;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;294;1218.561,949.1688;Inherit;False;Property;_RefractionIntensity;Refraction Intensity;10;0;Create;True;0;0;0;False;0;False;5;0.25;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;336;2886.345,-990.439;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;405;3312.469,-350.9227;Inherit;False;1524.082;470.5134;Comment;11;390;402;400;398;386;384;404;439;440;441;442;Fresnel;1,1,1,1;0;0
Node;AmplifyShaderEditor.ColorNode;328;3665.578,-1827.492;Inherit;False;Property;_ShallowColor;Shallow Color;14;0;Create;True;0;0;0;False;0;False;0.2282841,0.4686714,0.509434,0;0.09803905,0.2274508,0.1921567,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;275;1232,800;Inherit;False;274;WaterNormal;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PowerNode;188;-144.8135,-485.7395;Inherit;False;True;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;253;-707.7326,-2173.406;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0.88;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;327;3678.529,-1646.537;Inherit;False;Property;_DeepColor;Deep Color;13;0;Create;True;0;0;0;False;0;False;0.08940016,0.2804352,0.3867925,0;0.09718724,0.2264147,0.1941078,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;197;-1504.031,-2209.705;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.1,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;465;-1896.71,-1627.849;Inherit;False;True;False;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;440;3404.951,-107.1413;Inherit;False;Property;_FresnelPower;Fresnel Power;18;0;Create;True;0;0;0;False;0;False;0;0.6;0;25;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;329;4003.705,-1733.278;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;269;1248,608;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;293;1440.493,822.2026;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;473;-1893.373,-1309.887;Inherit;False;True;False;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;439;3405.951,-195.1414;Inherit;False;Property;_FresnelScale;Fresnel Scale;17;0;Create;True;0;0;0;False;0;False;0;1.53;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;404;3488.318,-278.3343;Inherit;False;274;WaterNormal;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PannerNode;466;-1494.275,-1782.54;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.15,0.08;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;193;-1138.645,-2458.96;Inherit;True;Property;_texture1;texture 1;24;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;252;-433.963,-2173.65;Inherit;True;False;2;0;FLOAT;0;False;1;FLOAT;0.65;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;190;47.33499,-510.2267;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;390;3750.176,-68.87425;Inherit;False;Property;_FresnelOpacityStrength;Fresnel Opacity Strength;15;0;Create;True;0;0;0;False;0;False;0.1;0.918;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;384;3734.011,-272.1524;Inherit;False;Standard;TangentNormal;ViewDir;True;True;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;3;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;442;3989.032,-271.3302;Inherit;False;Constant;_Float9;Float 9;18;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;270;1536.541,689.0806;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;430;4595.426,-1333.109;Inherit;False;1104.79;455.2224;Comment;7;397;363;165;166;330;394;432;Color Blend;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;235;428.4011,-1014.186;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;470;-1490.938,-1464.578;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.012,0.04;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;250;-156.9062,-2171.572;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;331;4046.014,-1542.058;Inherit;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SamplerNode;462;-1138.314,-1944.404;Inherit;True;Property;_TextureSample4;Texture Sample 1;24;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;166;4645.426,-1182.001;Inherit;False;Property;_WaterSpecularity;Water Specularity;5;0;Create;True;0;0;0;False;0;False;1;0.27;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SurfaceDepthNode;415;1639.915,1013.014;Inherit;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenDepthNode;413;1699.96,923.85;Inherit;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;416;1763.664,1094.767;Inherit;False;Constant;_Float5;Float 5;15;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;475;-1142.577,-1536.27;Inherit;True;Property;_TextureSample5;Texture Sample 1;24;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;417;1763.664,1180.767;Inherit;False;Constant;_Float8;Float 8;15;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;365;4215.317,-1475.305;Inherit;False;ColorOpacity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;441;4136.33,-223.9302;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;165;4647.674,-1093.469;Inherit;False;Property;_WaterSmoothness;Water Smoothness;4;0;Create;True;0;0;0;False;0;False;0.95;0.94;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;254;591.1565,-1014.321;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;469;-666.1371,-1765.74;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.35;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;362;4203.768,-1737.528;Inherit;False;Color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;330;4730.228,-1283.109;Inherit;False;274;WaterNormal;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ScreenColorNode;419;2331.708,606.5023;Inherit;False;Global;_GrabScreen1;Grab Screen 1;17;0;Create;True;0;0;0;False;0;False;Object;-1;False;True;False;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenColorNode;271;2292.922,790.2305;Inherit;False;Global;_GrabScreen0;Grab Screen 0;17;0;Create;True;0;0;0;False;0;False;Instance;419;False;True;False;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;398;4261.837,-87.29827;Inherit;False;365;ColorOpacity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;414;2062.176,862.6844;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;363;5116.424,-1000.812;Inherit;False;362;Color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;468;763.562,-1014.738;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CustomStandardSurface;394;5055.349,-1224.075;Inherit;False;Specular;Tangent;6;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,1;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PowerNode;476;-523.627,-1489.347;Inherit;False;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;386;4305.318,-204.1449;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;480;926.3591,-802.8953;Inherit;False;Constant;_Float12;Float 12;22;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;477;930.2861,-1014.676;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;397;5345.643,-1119.43;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;418;2514.727,606.8517;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;400;4482.597,-151.6528;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;431;4930.028,-802.7137;Inherit;False;806.1973;292.0583;Comment;5;372;403;382;433;436;Refraction Blend;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;261;1908.342,305.3318;Inherit;False;1109.405;186.9951;Water Edge;4;256;259;258;428;Water Edge;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;432;5490.596,-1122.034;Inherit;False;ColorBlend;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;479;1103.359,-854.8953;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;281;2835.578,610.3875;Inherit;False;Refraction;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;402;4640.118,-155.6522;Inherit;False;FresnelOpacity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;436;4986.053,-682.6436;Inherit;False;432;ColorBlend;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;372;4986.755,-760.8354;Inherit;False;281;Refraction;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;209;1266.704,-833.5115;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;403;4985.442,-595.5046;Inherit;False;402;FresnelOpacity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;256;1958.342,357.3268;Inherit;False;True;True;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;382;5278.268,-720.1938;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;420;1437.008,-837.4538;Inherit;False;FoamFinal;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;429;5005.132,-449.0711;Inherit;False;731.6436;560.7823;Comment;7;423;426;425;424;422;427;435;Foam Blend;1,1,1,1;0;0
Node;AmplifyShaderEditor.PowerNode;258;2271.242,356.8022;Inherit;False;True;2;0;FLOAT;0;False;1;FLOAT;1.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;259;2494.747,355.3318;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;423;5133.224,-100.3766;Inherit;False;420;FoamFinal;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;422;5035.762,-380.3438;Inherit;False;Constant;_Color0;Color 0;15;0;Create;True;0;0;0;False;0;False;0.8679245,0.8679245,0.8679245,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;426;5033.001,4.224387;Inherit;False;Property;_FoamOpacity;Foam Opacity;16;0;Create;True;0;0;0;False;0;False;1;0.619;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;433;5523.371,-727.2062;Inherit;False;RefractionBlend;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;435;5319.356,-354.9672;Inherit;False;433;RefractionBlend;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;427;5340.76,-230.7996;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;428;2681.863,364.3602;Inherit;False;WaterEdge;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;425;5340.263,-80.48497;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;424;5582.016,-201.4345;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;438;5986.527,-202.4315;Inherit;False;428;WaterEdge;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;461;-1841.544,1597.909;Inherit;True;Property;_NormalDetailMap;Normal Detail Map;0;1;[Normal];Create;True;0;0;0;False;0;False;None;39b7cc4fa2e7d6f42b58643bdf8aca95;True;bump;LockedToTexture2D;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.LerpOp;437;6112.569,-448.645;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;478;-768.1394,-1323.921;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.61;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;7026.458,-1151.06;Float;False;True;-1;2;;0;0;CustomLighting;TriForge/Fantasy Forest/SimpleWater;False;False;False;False;False;False;True;True;True;False;False;False;False;False;True;False;False;False;True;True;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;ForwardOnly;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;71;0;42;0
WireConnection;71;1;72;0
WireConnection;458;0;448;0
WireConnection;458;1;459;0
WireConnection;73;0;46;0
WireConnection;73;1;72;0
WireConnection;449;0;458;0
WireConnection;449;1;72;0
WireConnection;41;0;40;0
WireConnection;41;1;71;0
WireConnection;48;0;47;0
WireConnection;48;1;73;0
WireConnection;451;0;450;0
WireConnection;451;1;449;0
WireConnection;49;0;48;0
WireConnection;445;0;6;0
WireConnection;43;0;41;0
WireConnection;44;0;43;0
WireConnection;50;0;49;0
WireConnection;452;0;451;0
WireConnection;443;0;12;0
WireConnection;443;1;53;0
WireConnection;443;2;444;0
WireConnection;453;0;452;0
WireConnection;267;32;44;0
WireConnection;267;31;447;0
WireConnection;267;29;11;0
WireConnection;267;33;103;0
WireConnection;267;10;443;0
WireConnection;266;32;50;0
WireConnection;266;31;446;0
WireConnection;266;29;100;0
WireConnection;266;33;103;0
WireConnection;266;10;443;0
WireConnection;214;0;213;0
WireConnection;214;1;200;0
WireConnection;175;0;267;0
WireConnection;175;1;266;0
WireConnection;454;32;453;0
WireConnection;454;31;455;0
WireConnection;454;33;456;0
WireConnection;215;0;214;0
WireConnection;316;0;319;0
WireConnection;182;0;187;0
WireConnection;457;0;175;0
WireConnection;457;1;454;0
WireConnection;199;0;198;0
WireConnection;199;1;200;0
WireConnection;323;0;316;0
WireConnection;323;1;324;0
WireConnection;194;0;215;0
WireConnection;274;0;457;0
WireConnection;325;0;323;0
WireConnection;202;0;199;0
WireConnection;196;0;195;0
WireConnection;196;1;194;0
WireConnection;464;0;463;0
WireConnection;464;1;467;0
WireConnection;210;0;182;0
WireConnection;472;0;471;0
WireConnection;472;1;474;0
WireConnection;336;0;325;0
WireConnection;188;0;210;0
WireConnection;188;1;189;0
WireConnection;253;0;196;2
WireConnection;197;0;202;0
WireConnection;465;0;464;0
WireConnection;329;0;328;0
WireConnection;329;1;327;0
WireConnection;329;2;336;0
WireConnection;293;0;275;0
WireConnection;293;1;294;0
WireConnection;473;0;472;0
WireConnection;466;0;465;0
WireConnection;193;0;195;0
WireConnection;193;1;197;0
WireConnection;252;0;253;0
WireConnection;190;0;188;0
WireConnection;384;0;404;0
WireConnection;384;2;439;0
WireConnection;384;3;440;0
WireConnection;270;0;269;0
WireConnection;270;1;293;0
WireConnection;235;0;190;0
WireConnection;235;1;193;1
WireConnection;470;0;473;0
WireConnection;250;0;252;0
WireConnection;331;0;329;0
WireConnection;462;0;195;0
WireConnection;462;1;466;0
WireConnection;413;0;270;0
WireConnection;475;0;195;0
WireConnection;475;1;470;0
WireConnection;365;0;331;3
WireConnection;441;0;442;0
WireConnection;441;1;384;0
WireConnection;441;2;390;0
WireConnection;254;0;235;0
WireConnection;254;1;250;0
WireConnection;469;0;462;1
WireConnection;362;0;329;0
WireConnection;419;0;269;0
WireConnection;271;0;270;0
WireConnection;414;0;413;0
WireConnection;414;1;415;0
WireConnection;414;2;416;0
WireConnection;414;4;417;0
WireConnection;468;0;254;0
WireConnection;468;1;469;0
WireConnection;394;1;330;0
WireConnection;394;3;166;0
WireConnection;394;4;165;0
WireConnection;476;0;475;2
WireConnection;386;0;441;0
WireConnection;477;0;468;0
WireConnection;477;1;476;0
WireConnection;397;0;394;0
WireConnection;397;1;363;0
WireConnection;418;0;419;0
WireConnection;418;1;271;0
WireConnection;418;2;414;0
WireConnection;400;0;386;0
WireConnection;400;1;398;0
WireConnection;432;0;397;0
WireConnection;479;0;477;0
WireConnection;479;1;480;0
WireConnection;281;0;418;0
WireConnection;402;0;400;0
WireConnection;209;0;479;0
WireConnection;382;0;372;0
WireConnection;382;1;436;0
WireConnection;382;2;403;0
WireConnection;420;0;209;0
WireConnection;258;0;256;0
WireConnection;259;0;258;0
WireConnection;433;0;382;0
WireConnection;427;0;422;0
WireConnection;427;1;394;0
WireConnection;428;0;259;0
WireConnection;425;0;423;0
WireConnection;425;1;426;0
WireConnection;424;0;435;0
WireConnection;424;1;427;0
WireConnection;424;2;425;0
WireConnection;437;0;433;0
WireConnection;437;1;424;0
WireConnection;437;2;438;0
WireConnection;478;0;475;2
WireConnection;0;13;437;0
ASEEND*/
//CHKSM=CF2E41A96C1A5DCD22B1059272589CE04CB325DA