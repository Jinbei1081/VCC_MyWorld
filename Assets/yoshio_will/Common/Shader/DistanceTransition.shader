Shader "Yoshio_will/DistanceTransition"
{
    Properties
    {
        [Header(Main)]
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)

        _SpecGlossMap("Roughness Map", 2D) = "white" {}
        _Glossiness("Roughness", Range(0.0, 1.0)) = 1.0
        [MaterialToggle] _InvertGlossMap("Invert Roughness Map", float) = 0.0

        _MetallicGlossMap("Metallic Map", 2D) = "white" {}
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

        [Normal]_BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Scale", float) = 1.0

        _EmissionMap("Emission", 2D) = "black" {}
        [HDR]_EmissionColor("Emission Color", Color) = (0, 0, 0, 1)

        [Header(Sub)]
        _MainTex2("Albedo 2", 2D) = "white" {}
        _Color2("Color 2", Color) = (1, 1, 1, 1)

        _SpecGlossMap2("Roughness Map 2", 2D) = "white" {}
        _Glossiness2("Roughness 2", Range(0.0, 1.0)) = 1.0
        [MaterialToggle] _InvertGlossMap2("Invert Roughness Map 2", float) = 0.0

        _MetallicGlossMap2("Metallic Map 2", 2D) = "white" {}
        _Metallic2("Metallic 2", Range(0.0, 1.0)) = 0.0

        [Normal]_BumpMap2("Normal Map 2", 2D) = "bump" {}
        _BumpScale2("Scale 2", float) = 1.0

        _EmissionMap2("Emission 2", 2D) = "black" {}
        [HDR]_EmissionColor2("Emission Color 2", Color) = (0, 0, 0, 1)

        [Header(Fade Edge)]
        _FadeDistanceNear("Fade Distance Near", float) = 10
        _FadeDistanceFar("Fade Distance Far", float) = 20
        [HDR]_EdgeEmissionColor("Edge Emission Color", Color) = (3, 3, 3, 1)
        _EdgeNoiseMap("Edge Noise Map", 2D) = "black" {}
        _EdgeNoiseScale("Edge Noise Scale", Vector) = (3, 3, 3, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex, _SpecGlossMap, _MetallicGlossMap, _BumpMap, _EmissionMap;
        half _Glossiness, _Metallic, _BumpScale, _InvertGlossMap;
        fixed4 _Color, _EmissionColor;

        sampler2D _MainTex2, _SpecGlossMap2, _MetallicGlossMap2, _BumpMap2, _EmissionMap2;
        half _Glossiness2, _Metallic2, _BumpScale2, _InvertGlossMap2;
        fixed4 _Color2, _EmissionColor2;

        sampler2D _EdgeNoiseMap;
        float _FadeDistanceNear, _FadeDistanceFar;
        fixed4 _EdgeEmissionColor;
        fixed4 _EdgeNoiseScale;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // カメラとの距離を測る
            float3 cameraDistance = distance(IN.worldPos, _WorldSpaceCameraPos);
            float n = tex2D(_EdgeNoiseMap, fixed2(IN.worldPos.x / _EdgeNoiseScale.x, IN.worldPos.y / _EdgeNoiseScale.y + IN.worldPos.z / _EdgeNoiseScale.z)) / 2 + 0.5;
            float edgefade = saturate(((cameraDistance - _FadeDistanceNear) / (_FadeDistanceFar - _FadeDistanceNear)) + n);

            // メインテクスチャ
            fixed4 col = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed4 emission = tex2D(_EmissionMap, IN.uv_MainTex) * _EmissionColor;
            fixed3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            normal = normalize(normal * _BumpScale + float3(0, 0, 1) * (1 - _BumpScale));
            half rough = tex2D(_SpecGlossMap, IN.uv_MainTex);
            if (_InvertGlossMap > 0.5) rough = 1 - rough;
            rough *= _Glossiness;
            half metal = tex2D(_MetallicGlossMap, IN.uv_MainTex) * _Metallic;

            // サブテクスチャ
            fixed4 col2 = tex2D(_MainTex2, IN.uv_MainTex) * _Color2;
            fixed4 emission2 = tex2D(_EmissionMap2, IN.uv_MainTex) * _EmissionColor2;
            fixed3 normal2 = UnpackNormal(tex2D(_BumpMap2, IN.uv_MainTex));
            normal2 = normalize(normal2 * _BumpScale2 + float3(0, 0, 1) * (1 - _BumpScale2));
            half rough2 = tex2D(_SpecGlossMap2, IN.uv_MainTex);
            if (_InvertGlossMap2 > 0.5) rough2 = 1 - rough2;
            rough2 *= _Glossiness2;
            half metal2 = tex2D(_MetallicGlossMap2, IN.uv_MainTex) * _Metallic2;

            if (edgefade <= 0)
            {
                o.Albedo = col.rgb;
                o.Alpha = col.a;
                o.Smoothness = 1 - rough;
                o.Metallic = metal;
                o.Normal = normal;
                o.Emission = emission;
            }
            else if (edgefade < 1)
            {
                o.Albedo = col.rgb;
                o.Alpha = col.a;
                o.Smoothness = 1 - rough;
                o.Metallic = metal;
                o.Normal = normal;
                o.Emission = lerp(emission, _EdgeEmissionColor, edgefade);
            }
            else
            {
                o.Albedo = col2.rgb;
                o.Alpha = col2.a;
                o.Smoothness = 1 - rough2;
                o.Metallic = metal2;
                o.Normal = normal2;
                o.Emission = emission2;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
