// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TriForge/Fantasy Forest/Tree Billboard"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_MainTex("Albedo", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_MainBendingStrength("Main Bending Strength", Range( 0.2 , 1)) = 1
		_MainBendingMultiplier("Main Bending Multiplier", Float) = 1
		_WindDirectionRandomness("Wind Direction Randomness", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "Geometry+0" "DisableBatching" = "True" }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf StandardSpecular keepalpha addshadow fullforwardshadows nolightmap  nodirlightmap dithercrossfade vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float3 FFE_Wind_Direction;
		uniform float _WindDirectionRandomness;
		uniform float _MainBendingStrength;
		uniform float _MainBendingMultiplier;
		uniform float FFE_Wind_Strength;
		uniform sampler2D _Normal;
		uniform float4 _Normal_ST;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float _Cutoff = 0.5;


		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
		{
			original -= center;
			float C = cos( angle );
			float S = sin( angle );
			float t = 1 - C;
			float m00 = t * u.x * u.x + C;
			float m01 = t * u.x * u.y - S * u.z;
			float m02 = t * u.x * u.z + S * u.y;
			float m10 = t * u.x * u.y + S * u.z;
			float m11 = t * u.y * u.y + C;
			float m12 = t * u.y * u.z - S * u.x;
			float m20 = t * u.x * u.z - S * u.y;
			float m21 = t * u.y * u.z + S * u.x;
			float m22 = t * u.z * u.z + C;
			float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
			return mul( finalMatrix, original ) + center;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float ifLocalVar39 = 0;
			if( FFE_Wind_Direction.x == 0.0 )
				ifLocalVar39 = (float)1;
			float ifLocalVar38 = 0;
			if( FFE_Wind_Direction.z == 0.0 )
				ifLocalVar38 = (float)1;
			float3 lerpResult44 = lerp( FFE_Wind_Direction , float3(1,0,0) , ( ifLocalVar39 * ifLocalVar38 ));
			float3 worldToObjDir46 = normalize( mul( unity_WorldToObject, float4( lerpResult44, 0 ) ).xyz );
			float3 lerpResult50 = lerp( worldToObjDir46 , lerpResult44 , _WindDirectionRandomness);
			float3 WindDirection52 = lerpResult50;
			float3 objToWorld4 = mul( unity_ObjectToWorld, float4( float3( 0,0,0 ), 1 ) ).xyz;
			float2 temp_output_8_0 = (objToWorld4).xz;
			float2 lerpResult17 = lerp( ( temp_output_8_0 / 20.0 ) , ( temp_output_8_0 / 5.0 ) , 0.5);
			float2 panner25 = ( 1.0 * _Time.y * float2( 0.3,0.2 ) + lerpResult17);
			float simplePerlin2D27 = snoise( panner25 );
			simplePerlin2D27 = simplePerlin2D27*0.5 + 0.5;
			float MainBendingStrength14 = ( _MainBendingStrength * _MainBendingMultiplier * FFE_Wind_Strength );
			float temp_output_29_0 = (( -0.2 + ( 1.3 * MainBendingStrength14 ) ) + (simplePerlin2D27 - 0.0) * (( 0.8 + ( 0.6 * MainBendingStrength14 ) ) - ( -0.2 + ( 1.3 * MainBendingStrength14 ) )) / (1.0 - 0.0));
			float lerpResult32 = lerp( 0.0 , ( temp_output_29_0 * MainBendingStrength14 ) , MainBendingStrength14);
			float MainBending34 = ( temp_output_29_0 * lerpResult32 );
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 rotatedValue61 = RotateAroundAxis( float3( 0,0,0 ), ase_vertex3Pos, normalize( WindDirection52 ), radians( ( MainBending34 * 22.0 ) ) );
			float3 MainBendingRotation63 = ( rotatedValue61 - ase_vertex3Pos );
			v.vertex.xyz += MainBendingRotation63;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 uv_Normal = i.uv_texcoord * _Normal_ST.xy + _Normal_ST.zw;
			o.Normal = UnpackNormal( tex2D( _Normal, uv_Normal ) );
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode1 = tex2D( _MainTex, uv_MainTex );
			o.Albedo = tex2DNode1.rgb;
			o.Alpha = 1;
			clip( tex2DNode1.a - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
}
/*ASEBEGIN
Version=18935
7;66;1920;953;3596.845;1276.031;3.005517;True;True
Node;AmplifyShaderEditor.CommentaryNode;3;-4052.337,384.0836;Inherit;False;2943.556;1179.526;Comment;31;34;33;32;31;30;29;28;27;26;25;24;23;22;21;20;19;18;17;16;15;14;13;12;11;10;9;8;7;6;5;4;Main Bending;1,1,1,1;0;0
Node;AmplifyShaderEditor.TransformPositionNode;4;-3882.823,434.1677;Inherit;False;Object;World;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;5;-3900.984,1386.883;Inherit;False;Global;FFE_Wind_Strength;FFE_Wind_Strength;12;0;Create;True;0;0;0;False;0;False;0;0.69;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-3962.31,1199.945;Inherit;False;Property;_MainBendingStrength;Main Bending Strength;3;0;Create;True;0;0;0;False;0;False;1;0.8;0.2;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-3923.162,1292.196;Inherit;False;Property;_MainBendingMultiplier;Main Bending Multiplier;4;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;8;-3612.292,434.0836;Inherit;False;True;False;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;9;-3557.291,548.0829;Inherit;False;Constant;_Float1;Float 1;7;0;Create;True;0;0;0;False;0;False;20;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-3668.161,1239.196;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-3554.5,677.1363;Inherit;False;Constant;_Float7;Float 7;8;0;Create;True;0;0;0;False;0;False;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;12;-3353.501,658.1365;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;13;-3378.501,798.1365;Inherit;False;Constant;_Float8;Float 8;8;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;14;-3427.163,1234.196;Inherit;False;MainBendingStrength;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;15;-3357.291,485.0835;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;16;-3016.682,1239.282;Inherit;False;14;MainBendingStrength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;17;-3153.502,637.1369;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-2928.897,1159.089;Inherit;False;Constant;_Float6;Float 6;9;0;Create;True;0;0;0;False;0;False;1.3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;19;-3015.609,1411.995;Inherit;False;14;MainBendingStrength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;20;-2929.302,1320.961;Inherit;False;Constant;_Float14;Float 14;9;0;Create;True;0;0;0;False;0;False;0.6;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;25;-2901.056,687.6072;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.3,0.2;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-2736.302,1350.961;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;21;-2735.302,1209.943;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-2748.016,1083.373;Inherit;False;Constant;_Float12;Float 12;9;0;Create;True;0;0;0;False;0;False;0.8;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;-2751.016,959.3731;Inherit;False;Constant;_Float11;Float 11;9;0;Create;True;0;0;0;False;0;False;-0.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;26;-2510.018,1075.373;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;28;-2510.018,964.3732;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;27;-2657.056,681.6072;Inherit;True;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;35;-4119.504,1707.578;Inherit;False;1712.018;660.969;Check if WindDirection is being read from the script, if not use a default direction vector;11;52;50;47;46;44;41;40;39;38;37;36;Direction;1,0.08150674,0,1;0;0
Node;AmplifyShaderEditor.TFHCRemapNode;29;-2305.222,918.6503;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.2;False;4;FLOAT;0.8;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;30;-2269.039,1309.21;Inherit;False;14;MainBendingStrength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;37;-4093.6,2003.139;Inherit;False;Global;FFE_Wind_Direction;FFE_Wind_Direction;18;0;Create;True;0;0;0;False;0;False;0,0,0;0.9963899,0,0.08489505;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.IntNode;36;-4030.251,1893.026;Inherit;False;Constant;_Int0;Int 0;18;0;Create;True;0;0;0;False;0;False;1;0;False;0;1;INT;0
Node;AmplifyShaderEditor.ConditionalIfNode;38;-3754.698,1952.233;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;INT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;31;-1999.145,1157.166;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;39;-3755.757,1759.051;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;INT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;40;-3544.558,1754.361;Inherit;False;Constant;_DefaultDirection;Default Direction;8;0;Create;True;0;0;0;False;0;False;1,0,0;0,0,1;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;-3556.859,1916.464;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;32;-1827.94,1133.199;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;-1547.392,917.7163;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;44;-3327.783,1922.16;Inherit;True;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;48;-2455.457,2480.432;Inherit;False;1629.941;503.2019;Comment;9;63;62;61;60;59;58;57;56;55;Main Bending Rotation;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;47;-3358.794,2163.136;Inherit;False;Property;_WindDirectionRandomness;Wind Direction Randomness;5;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TransformDirectionNode;46;-3059.365,1810.881;Inherit;False;World;Object;True;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;34;-1348.018,913.3913;Inherit;False;MainBending;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;50;-2840.539,2115.09;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;55;-2425.631,2700.131;Inherit;False;34;MainBending;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;56;-2182.593,2815.52;Inherit;False;Constant;_Float4;Float 4;4;0;Create;True;0;0;0;False;0;False;22;30;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;57;-2173.832,2705.269;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;52;-2641.307,2108.875;Inherit;False;WindDirection;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PosVertexDataNode;59;-1955.356,2803.634;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;58;-1979.908,2593.121;Inherit;False;52;WindDirection;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RadiansOpNode;60;-1912.031,2707.368;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RotateAboutAxisNode;61;-1696.698,2622.491;Inherit;False;True;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;62;-1309.517,2688.103;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;42;-2273.971,1712.117;Inherit;False;1467.291;561.9999;Comment;6;54;53;51;49;45;43;Main Bending UV2 Mask;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;63;-1102.486,2682.959;Inherit;False;MainBendingRotation;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;54;-1079.256,1990.215;Inherit;False;UV2WindMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;64;-380.4983,362.5919;Inherit;False;63;MainBendingRotation;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;53;-1337.68,1995.116;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;-1523.907,1806.178;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;49;-1961.68,1762.117;Inherit;True;True;2;0;FLOAT;0;False;1;FLOAT;8;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;45;-1710.906,1885.178;Inherit;False;Constant;_Float13;Float 13;9;0;Create;True;0;0;0;False;0;False;0.6;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;43;-2223.971,1891.755;Inherit;True;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-790.8968,-54.05421;Inherit;True;Property;_Normal;Normal;2;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;1;-793.8968,-275.0543;Inherit;True;Property;_MainTex;Albedo;1;0;Create;False;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;;0;0;StandardSpecular;TriForge/Fantasy Forest/Tree Billboard;False;False;False;False;False;False;True;False;True;False;False;False;True;True;False;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;TransparentCutout;;Geometry;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;8;0;4;0
WireConnection;10;0;6;0
WireConnection;10;1;7;0
WireConnection;10;2;5;0
WireConnection;12;0;8;0
WireConnection;12;1;11;0
WireConnection;14;0;10;0
WireConnection;15;0;8;0
WireConnection;15;1;9;0
WireConnection;17;0;15;0
WireConnection;17;1;12;0
WireConnection;17;2;13;0
WireConnection;25;0;17;0
WireConnection;23;0;20;0
WireConnection;23;1;19;0
WireConnection;21;0;18;0
WireConnection;21;1;16;0
WireConnection;26;0;22;0
WireConnection;26;1;23;0
WireConnection;28;0;24;0
WireConnection;28;1;21;0
WireConnection;27;0;25;0
WireConnection;29;0;27;0
WireConnection;29;3;28;0
WireConnection;29;4;26;0
WireConnection;38;0;37;3
WireConnection;38;3;36;0
WireConnection;31;0;29;0
WireConnection;31;1;30;0
WireConnection;39;0;37;1
WireConnection;39;3;36;0
WireConnection;41;0;39;0
WireConnection;41;1;38;0
WireConnection;32;1;31;0
WireConnection;32;2;30;0
WireConnection;33;0;29;0
WireConnection;33;1;32;0
WireConnection;44;0;37;0
WireConnection;44;1;40;0
WireConnection;44;2;41;0
WireConnection;46;0;44;0
WireConnection;34;0;33;0
WireConnection;50;0;46;0
WireConnection;50;1;44;0
WireConnection;50;2;47;0
WireConnection;57;0;55;0
WireConnection;57;1;56;0
WireConnection;52;0;50;0
WireConnection;60;0;57;0
WireConnection;61;0;58;0
WireConnection;61;1;60;0
WireConnection;61;3;59;0
WireConnection;62;0;61;0
WireConnection;62;1;59;0
WireConnection;63;0;62;0
WireConnection;54;0;53;0
WireConnection;53;0;51;0
WireConnection;53;1;43;2
WireConnection;51;0;49;0
WireConnection;51;1;45;0
WireConnection;49;0;43;2
WireConnection;0;0;1;0
WireConnection;0;1;2;0
WireConnection;0;10;1;4
WireConnection;0;11;64;0
ASEEND*/
//CHKSM=E8E2AE83EA031D154E99FC26D2098D86A8FBC2B0