Shader "UniPMaterial/Custom/SamplePMatTwist"
{
    Properties
    {
        _TwistParam("Twist", Float) = 0
        _ChessboardTex("棋盘纹理", 2D) = "white" {}//Chessboard
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

            float _TwistParam;
            sampler2D _ChessboardTex;

            float4 frag (Vert2Frag i) : SV_Target
            {
                i.uv = i.uv * 2 - 1;
                float2 polar = float2(length(i.uv), atan2(i.uv.y, i.uv.x));
                polar.y += polar.x * _TwistParam;
                float sinTheta, cosTheta;
                sincos(polar.y, sinTheta, cosTheta);
                float2 uv = polar.x * float2(cosTheta, sinTheta) * 0.5 + 0.5;
                return tex2D(_ChessboardTex, uv);
            }
            ENDCG
        }
    }
}
/*PROPBEGIN
PROPEND*/