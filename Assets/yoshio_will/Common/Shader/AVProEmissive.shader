Shader "Yoshio_will/AVProEmissive"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Main Color", Color) = (1, 1, 1, 1)
        _EmissionMap ("Emission Map", 2D) = "white" {}
        [HDR]_EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        [MaterialToggle] _FlipEmissionMap ("Flip Emission Map", Float) = 0
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0
        _Gamma("Gamma", Float) = 2.2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        ZWrite On

        CGPROGRAM
        #pragma surface surf Standard addshadow
        #pragma target 3.0

        sampler2D _MainTex, _EmissionMap;
        fixed4 _Color, _EmissionColor;
        float _Smoothness, _Metallic, _FlipEmissionMap;
        float _Gamma;

        struct Input
        {
            float2 uv_MainTex;
        };

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 col = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            float2 uv = IN.uv_MainTex;
            uv.y = (_FlipEmissionMap > 0.5) ? (1 - uv.y) : uv.y;

            fixed4 em = tex2D(_EmissionMap, uv) * _EmissionColor;
            em.rgb = pow(em.rgb, _Gamma);

            o.Albedo = col.rgb;
            o.Emission = em.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = col.a;
        }
        ENDCG
    }
    FallBack "Standard"
}
