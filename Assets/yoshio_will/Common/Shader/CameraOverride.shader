Shader "Yoshio_will/CameraOverride"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Background Color", color) = (0,0,0,1)
        _Distance("Override Distance", float) = 0.3
        [MaterialToggle] _ForceOverride("Force Override", float) = 0.0
    }
    SubShader
    {
        Tags {
            "DisableBatching"="True"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }
        LOD 100
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                uint isOverride : FLAG;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Distance;
            float _ForceOverride;

            float _VRChatCameraMode;
            uint _VRChatCameraMask;
            float _VRChatMirrorMode;
            float3 _VRChatMirrorCameraPos;

            v2f vert (appdata v)
            {
                float3 objectPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                //float3 objectPos = mul(unity_ObjectToWorld, float4(0.0, 0.0, 0.0, 1.0)).xyz;  // Canvas‚ÌŒ´“_‚©‚ç‚Ì‹——£‚É‚È‚Á‚Ä‚µ‚Ü‚¤
                float dist = distance(objectPos, _WorldSpaceCameraPos);
                uint isOverride = 
                    (
                        (dist < _Distance) &&
                        (_VRChatCameraMode == 1 || _VRChatCameraMode == 2 || _ForceOverride == 1) &&
                        (_VRChatMirrorMode == 0)
                    ) ? 1 : 0;
                //isOverride = (uint)saturate((float)isOverride + _ForceOverride);
                //isOverride = (dist < _Distance) ? 1 : 0;    // for debug
                //isOverride = 1;

                v2f o;
                o.vertex = (isOverride == 1) ? float4(v.uv.x * 2 - 1, 1 - v.uv.y * 2, 0, 1) : UnityObjectToClipPos(v.vertex);
                o.uv = (isOverride == 1) ? ComputeScreenPos(o.vertex) : v.uv;
                o.isOverride = isOverride;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                clip((float)i.isOverride - 0.5);
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
