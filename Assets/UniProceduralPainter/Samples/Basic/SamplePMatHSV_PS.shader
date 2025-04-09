Shader "UniPMaterial/Custom/SamplePMatHSV"
{
    Properties
    {
        _TestMainTex ("示例纹理", 2D) = "white" {}
        _HueBias ("Hue", Float) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex FullScreenTri
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/UniProceduralPainter/Shader/Lib/UtilLib.cginc"

            sampler2D _TestMainTex;
            float _HueBias;

            float3 RGBToHSV(float3 col)
            {
                const float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(col.bg, K.wz), float4(col.gb, K.xy), step(col.b, col.g));
                float4 q = lerp(float4(p.xyw, col.r), float4(col.r, p.yzx), step(p.x, col.r));
                float d = q.x - min(q.w, q.y);
                const float e = 1.0e-4;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HSVToRGB(float3 col)
            {
                //ref https://www.shadertoy.com/view/MsS3Wc
                //official hsv
                const float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 _oRGB = abs(frac(col.xxx + K.xyz) * 6.0 - K.www);
                float3 _sRGB = saturate(_oRGB - K.xxx);
                _oRGB = col.z * lerp(K.xxx, _sRGB, col.y);
                return _oRGB;
            }

            float4 frag(Vert2Frag i) : SV_Target
            {
                float h = frac(_HueBias);
                float4 col = tex2D(_TestMainTex, i.uv);
                float3 hsv = RGBToHSV(col.xyz);
                col.xyz = HSVToRGB(hsv + float3(h, 0, 0));
                return col;
            }
            ENDCG
        }
    }
}
/*PROPBEGIN
PROPEND*/