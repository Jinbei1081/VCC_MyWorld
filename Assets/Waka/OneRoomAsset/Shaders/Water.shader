// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Waka/Water"
{
	Properties
	{
		[Normal]_TextureSample0("Texture Sample 0", 2D) = "bump" {}
		[Normal]_TextureSample1("Texture Sample 1", 2D) = "bump" {}
		_BaseColor("BaseColor", Color) = (0.4705882,0.7651153,0.7843137,0)
		_Wave("Wave", Range( 0 , 50)) = 1
		_WaterSpeed("WaterSpeed", Range( 0 , 2)) = 0.2
		_Smoothness("Smoothness", Range( 0 , 1)) = 1
		_Metallic("Metallic", Range( 0 , 1)) = 1
		_Opacity("Opacity", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Custom"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" }
		Cull Back
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha , One OneMinusSrcAlpha
		
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _TextureSample0;
		uniform float _Wave;
		uniform float _WaterSpeed;
		uniform sampler2D _TextureSample1;
		uniform float4 _BaseColor;
		uniform float _Metallic;
		uniform float _Smoothness;
		uniform float _Opacity;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 temp_cast_0 = (_Wave).xx;
			float temp_output_9_0 = ( _Time.x * _WaterSpeed );
			float2 temp_cast_1 = (temp_output_9_0).xx;
			float2 uv_TexCoord15 = i.uv_texcoord * temp_cast_0 + temp_cast_1;
			float2 temp_cast_2 = (_Wave).xx;
			float2 temp_cast_3 = (-temp_output_9_0).xx;
			float2 uv_TexCoord14 = i.uv_texcoord * temp_cast_2 + temp_cast_3;
			o.Normal = BlendNormals( UnpackNormal( tex2D( _TextureSample0, uv_TexCoord15 ) ) , UnpackNormal( tex2D( _TextureSample1, uv_TexCoord14 ) ) );
			o.Albedo = _BaseColor.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = _Opacity;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}
/*ASEBEGIN
Version=18935
0;636;1498;363;5636.805;920.5544;5.944691;True;False
Node;AmplifyShaderEditor.CommentaryNode;48;-1503.565,64.00854;Inherit;False;1619.913;489.09;Comment;10;10;9;12;14;15;5;3;6;8;18;波;1,1,1,1;0;0
Node;AmplifyShaderEditor.TimeNode;8;-1360.349,120.5497;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;10;-1453.565,274.6838;Inherit;False;Property;_WaterSpeed;WaterSpeed;6;0;Create;True;0;0;0;False;0;False;0.2;0.5;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-1114.596,187.6695;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;12;-976.6287,274.6838;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-1152.066,379.448;Inherit;False;Property;_Wave;Wave;5;0;Create;True;0;0;0;False;0;False;1;4;0;50;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;14;-761.3899,362.3026;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;15;-768.604,170.7433;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;5;-441.9258,323.0992;Inherit;True;Property;_TextureSample1;Texture Sample 1;2;1;[Normal];Create;True;0;0;0;False;0;False;-1;378a83ef5f938ae48a72cf25b24ac08b;378a83ef5f938ae48a72cf25b24ac08b;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-444.8318,114.0087;Inherit;True;Property;_TextureSample0;Texture Sample 0;0;1;[Normal];Create;True;0;0;0;False;0;False;-1;378a83ef5f938ae48a72cf25b24ac08b;378a83ef5f938ae48a72cf25b24ac08b;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;16;-247.1983,-187.0521;Inherit;False;Property;_BaseColor;BaseColor;4;0;Create;True;0;0;0;False;0;False;0.4705882,0.7651153,0.7843137,0;0.4652457,0.8018868,0.7861559,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;17;170.6368,371.0555;Inherit;False;Property;_Opacity;Opacity;9;0;Create;True;0;0;0;False;0;False;1;0.584;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;97;170.1533,190.6971;Inherit;False;Property;_Metallic;Metallic;8;0;Create;True;0;0;0;False;0;False;1;0.85;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;2;167.2252,278.8366;Inherit;False;Property;_Smoothness;Smoothness;7;0;Create;True;0;0;0;False;0;False;1;0.744;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;6;-111.6605,156.7032;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;92;-101.8602,1430.61;Inherit;True;Property;_TextureSample2;Texture Sample 2;1;1;[Normal];Create;True;0;0;0;False;0;False;-1;378a83ef5f938ae48a72cf25b24ac08b;378a83ef5f938ae48a72cf25b24ac08b;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;547.1142,-70.36062;Float;False;True;-1;2;;0;0;Standard;Water;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;2;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;Custom;;Transparent;All;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;3;1;False;-1;10;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;3;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;9;0;8;1
WireConnection;9;1;10;0
WireConnection;12;0;9;0
WireConnection;14;0;18;0
WireConnection;14;1;12;0
WireConnection;15;0;18;0
WireConnection;15;1;9;0
WireConnection;5;1;14;0
WireConnection;3;1;15;0
WireConnection;6;0;3;0
WireConnection;6;1;5;0
WireConnection;0;0;16;0
WireConnection;0;1;6;0
WireConnection;0;3;97;0
WireConnection;0;4;2;0
WireConnection;0;9;17;0
ASEEND*/
//CHKSM=D7175A9FAE951152903B68865FDF8F8C16CCAD00