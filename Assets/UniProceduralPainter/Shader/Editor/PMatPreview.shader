Shader "UniPMaterial/Editor/PMatPreview"
{
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

            float _PreviewScale;
            float2 _PreviewTranslation;
            float2 _TargetOutputScale;
            float4 _PreviewBGColor;

            Texture2D _PMatOutputHandle;

            SamplerState sampler_PointClamp;

            half4 frag(Vert2Frag i) : SV_Target
            {
                float2 offset = float2(_PreviewTranslation.x, -_PreviewTranslation.y);
                float2 uv = ((i.uv - 0.5) / _PreviewScale - offset) * float2(1, 0.5625) * _TargetOutputScale + 0.5;
                if (any(uv > 1) || any(uv < 0))
                {
                    return _PreviewBGColor;
                }
                half4 col = _PMatOutputHandle.Sample(sampler_PointClamp, uv);
                return lerp(_PreviewBGColor, col, col.w);//TODO:更多的混合模式
            }
            ENDCG
        }
    }
}
