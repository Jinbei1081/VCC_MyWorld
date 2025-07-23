Shader "Yoshio_will/ColorPickerShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
        [Header(RGB)]
        _RUMagnifier ("RUMagnifier", float) = 0
        _RUOffset ("RUOffset", float) = 0
        _RVMagnifier("RVMagnifier", float) = 0
        _RVOffset("RVOffset", float) = 0
        _GUMagnifier("GUMagnifier", float) = 0
        _GUOffset("GUOffset", float) = 0
        _GVMagnifier("GVMagnifier", float) = 0
        _GVOffset("GVOffset", float) = 0
        _BUMagnifier("BUMagnifier", float) = 0
        _BUOffset("BUOffset", float) = 0
        _BVMagnifier("BVMagnifier", float) = 0
        _BVOffset("BVOffset", float) = 0

        [Header(HSV)]
        _HUMagnifier ("HUMagnifier", float) = 0
        _HUOffset ("HUOffset", float) = 0
        _HVMagnifier("HVMagnifier", float) = 0
        _HVOffset("HVOffset", float) = 0
        _SUMagnifier("SUMagnifier", float) = 0
        _SUOffset("SUOffset", float) = 0
        _SVMagnifier("SVMagnifier", float) = 0
        _SVOffset("SVOffset", float) = 0
        _VUMagnifier("VUMagnifier", float) = 0
        _VUOffset("VUOffset", float) = 0
        _VVMagnifier("VVMagnifier", float) = 0
        _VVOffset("VVOffset", float) = 0

        [Header(uGUI)]
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _ColorMask("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

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
                float4 color: COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color: COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _RUMagnifier, _GUMagnifier, _BUMagnifier;
            float _RVMagnifier, _GVMagnifier, _BVMagnifier;
            float _RUOffset, _GUOffset, _BUOffset;
            float _RVOffset, _GVOffset, _BVOffset;
            float _HUMagnifier, _SUMagnifier, _VUMagnifier;
            float _HVMagnifier, _SVMagnifier, _VVMagnifier;
            float _HUOffset, _SUOffset, _VUOffset;
            float _HVOffset, _SVOffset, _VVOffset;

            float3 hsv2rgb(float3 hsv)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);

                return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                fixed4 rgb = fixed4(
                    _RUMagnifier * i.uv.x + _RUOffset + _RVMagnifier * i.uv.y + _RVOffset,
                    _GUMagnifier * i.uv.x + _GUOffset + _GVMagnifier * i.uv.y + _GVOffset,
                    _BUMagnifier * i.uv.x + _BUOffset + _BVMagnifier * i.uv.y + _BVOffset, 1);

                fixed4 hsv = fixed4(
                    _HUMagnifier * i.uv.x + _HUOffset + _HVMagnifier * i.uv.y + _HVOffset,
                    _SUMagnifier * i.uv.x + _SUOffset + _SVMagnifier * i.uv.y + _SVOffset,
                    _VUMagnifier * i.uv.x + _VUOffset + _VVMagnifier * i.uv.y + _VVOffset, 1);

                fixed3 hsvConv = hsv2rgb(hsv);
                col = col * (rgb + fixed4(hsvConv.r, hsvConv.g, hsvConv.b, 1));

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
