// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "TriForge/Fantasy Forest/TreeWind2"
{
	Properties
	{
		[Enum(Off,0,Front,1,Back,2)]_CullMode("Cull Mode", Int) = 2
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_MainTex("Main Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,0)
		_MainBendingStrength("Main Bending Strength", Range( 0.2 , 1)) = 1
		_MainBendingMultiplier("Main Bending Multiplier", Float) = 1
		_LeafBendingStrength("Leaf Bending Strength", Range( 0 , 1)) = 1
		_LeafBendingMultiplier("Leaf Bending Multiplier", Float) = 1
		_LeafDownwardStrength("Leaf Downward Strength", Float) = 0.15
		_LeafForwardStrength("Leaf Forward Strength", Float) = 0.5
		_LeafNoiseScale("Leaf Noise Scale", Range( 0.1 , 5)) = 1
		_LeafFlutterSpeed("Leaf Flutter Speed", Float) = 1
		_WindDirectionRandomness("Wind Direction Randomness", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "DisableBatching" = "True" }
		Cull [_CullMode]
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf StandardSpecular keepalpha addshadow fullforwardshadows nolightmap  dithercrossfade vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
		};

		uniform int _CullMode;
		uniform sampler2D FFE_Wind_Mask;
		uniform float _LeafFlutterSpeed;
		uniform float _LeafNoiseScale;
		uniform float _LeafDownwardStrength;
		uniform float _LeafBendingStrength;
		uniform float _LeafBendingMultiplier;
		uniform float FFE_Leaf_Flutter;
		uniform float FFE_Wind_Speed;
		uniform float _MainBendingStrength;
		uniform float _MainBendingMultiplier;
		uniform float FFE_Wind_Strength;
		uniform float _LeafForwardStrength;
		uniform float3 FFE_Wind_Direction;
		uniform float _WindDirectionRandomness;
		uniform sampler2D _MainTex;
		uniform float4 _Color;
		uniform float _Cutoff = 0.5;


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
			float mulTime262 = _Time.y * _LeafFlutterSpeed;
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float3 temp_output_258_0 = ( ase_worldPos / ( 6.0 * _LeafNoiseScale ) );
			float2 panner213 = ( mulTime262 * float2( 0,0.4 ) + temp_output_258_0.xy);
			float LeafBendingStrength234 = ( _LeafBendingStrength * _LeafBendingMultiplier * FFE_Leaf_Flutter );
			float mulTime289 = _Time.y * ( 1.0 * FFE_Wind_Speed );
			float3 objToWorld126 = mul( unity_ObjectToWorld, float4( float3( 0,0,0 ), 1 ) ).xyz;
			float2 temp_output_128_0 = (objToWorld126).xz;
			float2 lerpResult160 = lerp( ( temp_output_128_0 / 35.0 ) , ( temp_output_128_0 / 12.0 ) , 0.5);
			float2 panner132 = ( mulTime289 * float2( 0.15,0.05 ) + lerpResult160);
			float MainBendingStrength242 = ( _MainBendingStrength * _MainBendingMultiplier * FFE_Wind_Strength );
			float temp_output_165_0 = (( -0.2 + ( 1.3 * MainBendingStrength242 ) ) + (tex2Dlod( FFE_Wind_Mask, float4( panner132, 0, 0.0) ).r - 0.0) * (( 0.8 + ( 0.6 * MainBendingStrength242 ) ) - ( -0.2 + ( 1.3 * MainBendingStrength242 ) )) / (1.0 - 0.0));
			float lerpResult162 = lerp( 0.0 , ( temp_output_165_0 * MainBendingStrength242 ) , MainBendingStrength242);
			float MainBending210 = ( temp_output_165_0 * lerpResult162 );
			float2 panner199 = ( mulTime262 * float2( -0.2,0 ) + temp_output_258_0.xy);
			float4 appendResult207 = (float4(0.0 , ( tex2Dlod( FFE_Wind_Mask, float4( panner213, 0, 0.0) ).g * _LeafDownwardStrength * LeafBendingStrength234 * MainBending210 ) , ( tex2Dlod( FFE_Wind_Mask, float4( panner199, 0, 0.0) ).a * _LeafForwardStrength * LeafBendingStrength234 * MainBending210 ) , 0.0));
			float saferPower146 = abs( v.texcoord1.xy.y );
			float UV2WindMask227 = ( ( pow( saferPower146 , 8.0 ) * 0.6 ) + v.texcoord1.xy.y );
			float4 lerpResult233 = lerp( float4( 0,0,0,0 ) , ( v.color.g * appendResult207 ) , UV2WindMask227);
			float3 worldToObjDir206 = mul( unity_WorldToObject, float4( lerpResult233.xyz, 0 ) ).xyz;
			float3 LeafBending204 = worldToObjDir206;
			float ifLocalVar269 = 0;
			if( FFE_Wind_Direction.x == 0.0 )
				ifLocalVar269 = (float)1;
			float ifLocalVar270 = 0;
			if( FFE_Wind_Direction.z == 0.0 )
				ifLocalVar270 = (float)1;
			float3 lerpResult273 = lerp( FFE_Wind_Direction , float3(1,0,0) , ( ifLocalVar269 * ifLocalVar270 ));
			float3 worldToObjDir275 = normalize( mul( unity_WorldToObject, float4( lerpResult273, 0 ) ).xyz );
			float3 lerpResult276 = lerp( worldToObjDir275 , lerpResult273 , _WindDirectionRandomness);
			float3 WindDirection277 = lerpResult276;
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 rotatedValue133 = RotateAroundAxis( float3( 0,0,0 ), ase_vertex3Pos, normalize( WindDirection277 ), radians( ( MainBending210 * 22.0 ) ) );
			float3 MainBendingRotation250 = ( rotatedValue133 - ase_vertex3Pos );
			v.vertex.xyz += ( LeafBending204 + ( UV2WindMask227 * MainBendingRotation250 ) );
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float4 tex2DNode2 = tex2D( _MainTex, i.uv_texcoord );
			o.Albedo = ( tex2DNode2 * _Color ).rgb;
			float temp_output_252_0 = 0.0;
			float3 temp_cast_1 = (temp_output_252_0).xxx;
			o.Specular = temp_cast_1;
			o.Smoothness = temp_output_252_0;
			o.Alpha = 1;
			clip( ( tex2DNode2.a * _Color.a ) - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
}
/*ASEBEGIN
Version=18935
7;60;1920;959;10256.68;-952.4171;4.225603;True;True
Node;AmplifyShaderEditor.CommentaryNode;197;-6773.263,560.9977;Inherit;False;2907.715;1342.441;Comment;37;164;210;155;162;189;165;245;186;185;131;180;132;188;181;191;247;246;187;192;160;129;158;161;242;159;128;130;244;126;243;263;285;286;287;289;290;291;Main Bending;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;263;-6621.908,1563.797;Inherit;False;Global;FFE_Wind_Strength;FFE_Wind_Strength;12;0;Create;True;0;0;0;False;0;False;0;0.69;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;164;-6683.235,1376.859;Inherit;False;Property;_MainBendingStrength;Main Bending Strength;4;0;Create;True;0;0;0;False;0;False;1;0.8;0.2;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;243;-6644.087,1469.11;Inherit;False;Property;_MainBendingMultiplier;Main Bending Multiplier;5;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TransformPositionNode;126;-6603.747,611.0817;Inherit;False;Object;World;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;291;-6424.197,1198.543;Inherit;False;Global;FFE_Wind_Speed;FFE_Wind_Speed;13;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;244;-6389.087,1416.11;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;159;-6275.425,854.0501;Inherit;False;Constant;_Float7;Float 7;8;0;Create;True;0;0;0;False;0;False;12;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;130;-6278.216,724.9965;Inherit;False;Constant;_Float1;Float 1;7;0;Create;True;0;0;0;False;0;False;35;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;287;-6445.197,1097.543;Inherit;False;Constant;_MainBendingSpeed;Main Bending Speed;13;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;128;-6333.217,610.9977;Inherit;False;True;False;True;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;129;-6078.215,661.9974;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;158;-6074.425,835.0504;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;161;-6099.425,975.0501;Inherit;False;Constant;_Float8;Float 8;8;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;242;-6148.087,1411.11;Inherit;False;MainBendingStrength;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;290;-6125.197,1136.543;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;282;-7520.153,1095.034;Inherit;True;Global;FFE_Wind_Mask;FFE_Wind_Mask;13;0;Create;True;0;0;0;False;0;False;8132ffd5113818049a1b7ec588afbab3;8132ffd5113818049a1b7ec588afbab3;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RegisterLocalVarNode;284;-7240.812,1094.754;Inherit;False;WindMask;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode;247;-5737.606,1416.196;Inherit;False;242;MainBendingStrength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;187;-5649.821,1336.003;Inherit;False;Constant;_Float6;Float 6;9;0;Create;True;0;0;0;False;0;False;1.3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;160;-5874.426,814.0507;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;289;-5892.197,1136.543;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;246;-5736.532,1588.909;Inherit;False;242;MainBendingStrength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;192;-5650.226,1497.875;Inherit;False;Constant;_Float14;Float 14;9;0;Create;True;0;0;0;False;0;False;0.6;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;181;-5468.94,1260.287;Inherit;False;Constant;_Float12;Float 12;9;0;Create;True;0;0;0;False;0;False;0.8;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;286;-5599.519,666.5592;Inherit;False;284;WindMask;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;191;-5457.226,1527.875;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;180;-5471.94,1136.287;Inherit;False;Constant;_Float11;Float 11;9;0;Create;True;0;0;0;False;0;False;-0.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;188;-5456.226,1386.857;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;132;-5621.98,864.5211;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0.15,0.05;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;266;-7458.618,-216.3799;Inherit;False;1712.018;660.969;Check if WindDirection is being read from the script, if not use a default direction vector;11;276;275;274;273;272;271;270;269;268;267;277;Direction;1,0.08150674,0,1;0;0
Node;AmplifyShaderEditor.SamplerNode;285;-5378.519,662.5592;Inherit;True;Property;_TextureSample0;Texture Sample 0;14;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;185;-5230.941,1141.287;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;186;-5230.941,1252.287;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;268;-7432.715,79.17886;Inherit;False;Global;FFE_Wind_Direction;FFE_Wind_Direction;18;0;Create;True;0;0;0;False;0;False;0,0,0;0.9963899,0,0.08489505;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;245;-4989.963,1486.124;Inherit;False;242;MainBendingStrength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;165;-5026.146,1095.564;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.2;False;4;FLOAT;0.8;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;267;-7369.366,-30.93319;Inherit;False;Constant;_Int0;Int 0;18;0;Create;True;0;0;0;False;0;False;1;0;False;0;1;INT;0
Node;AmplifyShaderEditor.CommentaryNode;240;-6731.083,2028.665;Inherit;False;2865.563;1237.427;Comment;16;202;229;233;203;207;204;206;208;248;249;257;258;259;262;256;280;Leaf Bending;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;256;-6671.649,2766.578;Inherit;False;Property;_LeafNoiseScale;Leaf Noise Scale;10;0;Create;True;0;0;0;False;0;False;1;1;0.1;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;259;-6530.183,2650.841;Inherit;False;Constant;_Float2;Float 2;12;0;Create;True;0;0;0;False;0;False;6;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;270;-7093.812,28.27281;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;INT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;189;-4720.067,1334.08;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ConditionalIfNode;269;-7094.871,-164.9081;Inherit;False;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;INT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;195;-5338.794,-715.9097;Inherit;False;1467.291;561.9999;Comment;6;227;147;193;194;146;142;Main Bending UV2 Mask;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector3Node;272;-6883.674,-169.5978;Inherit;False;Constant;_DefaultDirection;Default Direction;8;0;Create;True;0;0;0;False;0;False;1,0,0;0,0,1;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;162;-4548.86,1310.113;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;271;-6895.973,-7.495234;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;221;-7676.466,3220.605;Inherit;False;Property;_LeafBendingStrength;Leaf Bending Strength;6;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;265;-7592.74,3408.286;Inherit;False;Global;FFE_Leaf_Flutter;FFE_Leaf_Flutter;12;0;Create;True;0;0;0;False;0;False;0;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;236;-7631.543,3310.451;Inherit;False;Property;_LeafBendingMultiplier;Leaf Bending Multiplier;7;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;280;-6379.54,2747.466;Inherit;False;2;2;0;FLOAT;1.3;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;208;-6567.665,2492.667;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;257;-6555.738,2318.302;Inherit;False;Property;_LeafFlutterSpeed;Leaf Flutter Speed;11;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;262;-6291.565,2323.144;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;258;-6238.183,2530.841;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;249;-5895.489,2640.005;Inherit;False;745.5571;571.3542;Comment;7;218;219;237;239;294;295;199;Forward Movement;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;235;-7382.543,3254.451;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;142;-5288.794,-536.2709;Inherit;True;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;155;-4268.312,1094.63;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;248;-6053.772,2094.118;Inherit;False;898.686;518.6501;Comment;7;213;222;238;217;216;292;293;Downward Movement;1,1,1,1;0;0
Node;AmplifyShaderEditor.LerpOp;273;-6666.899,-1.799196;Inherit;True;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;196;-5498.889,-43.8657;Inherit;False;1629.941;503.2019;Comment;9;250;136;133;135;138;139;140;211;278;Main Bending Rotation;1,1,1,1;0;0
Node;AmplifyShaderEditor.PannerNode;199;-5843.505,2798.184;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.2,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;295;-5838.25,2695.92;Inherit;False;284;WindMask;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.PannerNode;213;-5855.092,2340.555;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0.4;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;234;-7139.543,3250.451;Inherit;False;LeafBendingStrength;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;293;-5856.878,2139.868;Inherit;False;284;WindMask;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;210;-4068.938,1090.305;Inherit;False;MainBending;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TransformDirectionNode;275;-6398.479,-113.0774;Inherit;False;World;Object;True;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;274;-6697.908,239.1766;Inherit;False;Property;_WindDirectionRandomness;Wind Direction Randomness;12;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;146;-5026.503,-665.9097;Inherit;True;True;2;0;FLOAT;0;False;1;FLOAT;8;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;194;-4775.73,-542.848;Inherit;False;Constant;_Float13;Float 13;9;0;Create;True;0;0;0;False;0;False;0.6;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;294;-5646.304,2695.92;Inherit;True;Property;_TextureSample2;Texture Sample 2;13;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;217;-5600.172,2333.236;Inherit;False;Property;_LeafDownwardStrength;Leaf Downward Strength;8;0;Create;True;0;0;0;False;0;False;0.15;0.15;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;238;-5606.675,2425.945;Inherit;False;234;LeafBendingStrength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;222;-5563.456,2506.206;Inherit;False;210;MainBending;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;193;-4588.731,-621.8481;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;140;-5226.025,291.2222;Inherit;False;Constant;_Float4;Float 4;4;0;Create;True;0;0;0;False;0;False;22;30;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;219;-5580.371,2910.107;Inherit;False;Property;_LeafForwardStrength;Leaf Forward Strength;9;0;Create;True;0;0;0;False;0;False;0.5;0.2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;239;-5544.855,3084.538;Inherit;False;210;MainBending;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;211;-5469.063,175.833;Inherit;False;210;MainBending;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;276;-6179.655,191.1305;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;292;-5656.604,2139.281;Inherit;True;Property;_TextureSample1;Texture Sample 1;13;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;237;-5587.925,2996.419;Inherit;False;234;LeafBendingStrength;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;218;-5292.376,2863.839;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;216;-5316.813,2313.008;Inherit;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;147;-4402.504,-432.9103;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;139;-5217.264,180.971;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;277;-5980.423,184.9157;Inherit;False;WindDirection;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RadiansOpNode;138;-4955.463,183.07;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;227;-4144.079,-437.8116;Inherit;False;UV2WindMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;278;-5023.34,68.82373;Inherit;False;277;WindDirection;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.PosVertexDataNode;135;-4998.788,279.3362;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;203;-5078.628,2159.555;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;207;-5047.359,2337.074;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RotateAboutAxisNode;133;-4740.13,98.19386;Inherit;False;True;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;202;-4830.566,2313.073;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;229;-4892.928,2453.551;Inherit;False;227;UV2WindMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;136;-4352.949,163.805;Inherit;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;233;-4570.521,2348.216;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TransformDirectionNode;206;-4377.521,2343.216;Inherit;False;World;Object;False;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;250;-4145.917,158.6612;Inherit;False;MainBendingRotation;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;228;-1008.826,477.3302;Inherit;False;227;UV2WindMask;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;143;-1119.694,-291.4216;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;251;-1045.897,575.587;Inherit;False;250;MainBendingRotation;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;204;-4089.52,2343.216;Inherit;False;LeafBending;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;205;-745.3903,464.0189;Inherit;False;204;LeafBending;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;2;-846.5266,-319.9578;Inherit;True;Property;_MainTex;Main Texture;2;0;Create;False;0;0;0;False;0;False;-1;None;2013d4059b4e3ca4b91827a2b6770318;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;141;-767.2503,557.159;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;72;-770.6423,-80.9163;Float;False;Property;_Color;Color;3;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.IntNode;125;-700.97,-450.8615;Float;False;Property;_CullMode;Cull Mode;0;1;[Enum];Create;True;0;3;Off;0;Front;1;Back;2;0;True;0;False;2;2;False;0;1;INT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;71;-468.5409,-215.9111;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;223;-453.9207,507.4107;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;131;-5377.98,858.5211;Inherit;True;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;252;-150.5437,80.66656;Inherit;False;Constant;_Float0;Float 0;7;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;226;-467.9449,-109.9293;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;;0;0;StandardSpecular;TriForge/Fantasy Forest/TreeWind2;False;False;False;False;False;False;True;False;False;False;False;False;True;True;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;1;-1;-1;-1;0;False;0;0;True;125;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;244;0;164;0
WireConnection;244;1;243;0
WireConnection;244;2;263;0
WireConnection;128;0;126;0
WireConnection;129;0;128;0
WireConnection;129;1;130;0
WireConnection;158;0;128;0
WireConnection;158;1;159;0
WireConnection;242;0;244;0
WireConnection;290;0;287;0
WireConnection;290;1;291;0
WireConnection;284;0;282;0
WireConnection;160;0;129;0
WireConnection;160;1;158;0
WireConnection;160;2;161;0
WireConnection;289;0;290;0
WireConnection;191;0;192;0
WireConnection;191;1;246;0
WireConnection;188;0;187;0
WireConnection;188;1;247;0
WireConnection;132;0;160;0
WireConnection;132;1;289;0
WireConnection;285;0;286;0
WireConnection;285;1;132;0
WireConnection;185;0;180;0
WireConnection;185;1;188;0
WireConnection;186;0;181;0
WireConnection;186;1;191;0
WireConnection;165;0;285;1
WireConnection;165;3;185;0
WireConnection;165;4;186;0
WireConnection;270;0;268;3
WireConnection;270;3;267;0
WireConnection;189;0;165;0
WireConnection;189;1;245;0
WireConnection;269;0;268;1
WireConnection;269;3;267;0
WireConnection;162;1;189;0
WireConnection;162;2;245;0
WireConnection;271;0;269;0
WireConnection;271;1;270;0
WireConnection;280;0;259;0
WireConnection;280;1;256;0
WireConnection;262;0;257;0
WireConnection;258;0;208;0
WireConnection;258;1;280;0
WireConnection;235;0;221;0
WireConnection;235;1;236;0
WireConnection;235;2;265;0
WireConnection;155;0;165;0
WireConnection;155;1;162;0
WireConnection;273;0;268;0
WireConnection;273;1;272;0
WireConnection;273;2;271;0
WireConnection;199;0;258;0
WireConnection;199;1;262;0
WireConnection;213;0;258;0
WireConnection;213;1;262;0
WireConnection;234;0;235;0
WireConnection;210;0;155;0
WireConnection;275;0;273;0
WireConnection;146;0;142;2
WireConnection;294;0;295;0
WireConnection;294;1;199;0
WireConnection;193;0;146;0
WireConnection;193;1;194;0
WireConnection;276;0;275;0
WireConnection;276;1;273;0
WireConnection;276;2;274;0
WireConnection;292;0;293;0
WireConnection;292;1;213;0
WireConnection;218;0;294;4
WireConnection;218;1;219;0
WireConnection;218;2;237;0
WireConnection;218;3;239;0
WireConnection;216;0;292;2
WireConnection;216;1;217;0
WireConnection;216;2;238;0
WireConnection;216;3;222;0
WireConnection;147;0;193;0
WireConnection;147;1;142;2
WireConnection;139;0;211;0
WireConnection;139;1;140;0
WireConnection;277;0;276;0
WireConnection;138;0;139;0
WireConnection;227;0;147;0
WireConnection;207;1;216;0
WireConnection;207;2;218;0
WireConnection;133;0;278;0
WireConnection;133;1;138;0
WireConnection;133;3;135;0
WireConnection;202;0;203;2
WireConnection;202;1;207;0
WireConnection;136;0;133;0
WireConnection;136;1;135;0
WireConnection;233;1;202;0
WireConnection;233;2;229;0
WireConnection;206;0;233;0
WireConnection;250;0;136;0
WireConnection;204;0;206;0
WireConnection;2;1;143;0
WireConnection;141;0;228;0
WireConnection;141;1;251;0
WireConnection;71;0;2;0
WireConnection;71;1;72;0
WireConnection;223;0;205;0
WireConnection;223;1;141;0
WireConnection;131;0;132;0
WireConnection;226;0;2;4
WireConnection;226;1;72;4
WireConnection;0;0;71;0
WireConnection;0;3;252;0
WireConnection;0;4;252;0
WireConnection;0;10;226;0
WireConnection;0;11;223;0
ASEEND*/
//CHKSM=71314A770EBAD6671E775D1D9D48DD41A3D1F6B6