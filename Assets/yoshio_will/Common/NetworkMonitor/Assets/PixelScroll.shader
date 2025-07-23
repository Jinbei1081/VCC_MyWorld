Shader "Yoshio_will/PixelScroll"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _InitColor("Initial Color", Color) = (1, 1, 1, 1)
        _Vector("Vector", Vector) = (-1, 0, 0, 0)
    }
    SubShader
    {
        Blend One Zero

        Pass
        {
            Name "Update"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float4 _Vector;
            fixed4 _Color, _InitColor;

            float4 frag(v2f_customrendertexture IN) : SV_Target
            {
                float tw = 1 / _CustomRenderTextureWidth;
                float2 uv = IN.localTexcoord.xy + _Vector.xy * tw;
                fixed4 texColor = tex2D(_SelfTexture2D, uv);
                fixed4 color = (uv.x < 0 || uv.x >= 1 || uv.y < 0 || uv.y >= 1) ? _Color : texColor;

                return color;
            }
            ENDCG
        }

        Pass
        {
            Name "Initialize"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float4 _Vector;
            fixed4 _Color, _InitColor;

            float4 frag(v2f_customrendertexture IN) : SV_Target
            {
                return _InitColor;
            }
            ENDCG
        }
    }
}
