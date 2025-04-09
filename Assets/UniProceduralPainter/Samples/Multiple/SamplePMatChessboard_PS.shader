Shader "UniPMaterial/Custom/SamplePMatChessboard"
{
    Properties
    {
        _Color1 ("Color1", Color) = (1, 1, 1, 1)
        _Color2 ("Color1", Color) = (0, 0, 0, 1)
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

            float4 _Color1;
            float4 _Color2;

            float mod(float x, float y)
            {
                return x - y * floor(x / y);
            }

            float4 frag (Vert2Frag i) : SV_Target
            {
                i.uv = floor(i.uv * 64);
                float3 baseColor = lerp(_Color1.xyz, _Color2.xyz, mod((i.uv.x + i.uv.y), 2.0));
                return float4(baseColor, 1);
            }
            ENDCG
        }
    }
}
/*PROPBEGIN
PROPEND*/