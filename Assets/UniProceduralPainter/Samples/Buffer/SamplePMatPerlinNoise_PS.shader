Shader "UniPMaterial/Custom/SamplePMatPerlinNoise"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ViewDepth ("Z", Float) = 0
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

            float4 _Color;
            float _ViewDepth;
            StructuredBuffer<int> _PerlinNoise3DParams;

            //glsl mod
            float mod(float x, float y)
            {
                return x - y * floor(x / y);
            }

            float2 mod(float2 x, float2 y)
            {
                return x - y * floor(x / y);
            }

            float3 mod(float3 x, float3 y)
            {
                return x - y * floor(x / y);
            }

            //ref: The Real-time Volumetric Cloudscapes of Horizon: Zero Dawn
            float remap(float value, float omin, float omax, float nmin, float nmax)
            {
                return nmin + (((value - omin) / (omax - omin)) * (nmax - nmin));
            }

            //ref: https://www.shadertoy.com/view/4djSRW
            float3 hashF3F3C002(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            //ref: https://mrl.cs.nyu.edu/~perlin/noise/
            float PerlinGrad(int hash, float x, float y, float z)
            {
                int h = hash & 15;
                float u = h < 8 ? x : y;
                float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
                return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
            }

			float PerlinNoiseF3F1Origin(float3 x)
			{
				int3 X = (int3)floor(x) & 255;
				x = frac(x);
				float3 u = x * x * x * (x * (x * 6 - 15) + 10);
				int A = _PerlinNoise3DParams[mod(X.x, 256)] + X.y;
				int AA = _PerlinNoise3DParams[mod(A, 256)] + X.z;
				int AB = _PerlinNoise3DParams[mod(A + 1, 256)] + X.z;
				int B = _PerlinNoise3DParams[mod(X.x + 1, 256)] + X.y;
				int BA = _PerlinNoise3DParams[mod(B, 256)] + X.z;
				int BB = _PerlinNoise3DParams[mod(B + 1, 256)] + X.z;

				return lerp(lerp(lerp(PerlinGrad(_PerlinNoise3DParams[mod(AA, 256)], x.x, x.y, x.z),
					PerlinGrad(_PerlinNoise3DParams[mod(BA, 256)], x.x - 1, x.y, x.z), u.x),
					lerp(PerlinGrad(_PerlinNoise3DParams[mod(AB, 256)], x.x, x.y - 1, x.z),
						PerlinGrad(_PerlinNoise3DParams[mod(BB, 256)], x.x - 1, x.y - 1, x.z), u.x), u.y),
					lerp(lerp(PerlinGrad(_PerlinNoise3DParams[mod(AA + 1, 256)], x.x, x.y, x.z - 1),
						PerlinGrad(_PerlinNoise3DParams[mod(BA + 1, 256)], x.x - 1, x.y, x.z - 1), u.x),
						lerp(PerlinGrad(_PerlinNoise3DParams[mod(AB + 1, 256)], x.x, x.y - 1, x.z - 1),
							PerlinGrad(_PerlinNoise3DParams[mod(BB + 1, 256)], x.x - 1, x.y - 1, x.z - 1),
							u.x), u.y), u.z);
			}

            float PerlinNoiseF3F1Sebh(float3 pIn, float frequency, int octaveCount)
            {
                //fbm
                const float octaveFrenquencyFactor = 2;
                //
                float sum = 0;
                float weightSum = 0;
                float weight = 0.5;
                for (int oct = 0; oct < octaveCount; oct++)
                {
                    float3 p = pIn * frequency;
                    float val = PerlinNoiseF3F1Origin(p);

                    sum += val * weight;
                    weightSum += weight;

                    weight *= weight;
                    frequency *= octaveFrenquencyFactor;
                }

                float noise = (sum / weightSum) * 0.5 + 0.5;
                return saturate(noise);
            }

            float CellsTileableF3F1Sebh(float3 p, float cellCount)
            {
                //Worley Noise
                const float3 pCell = p * cellCount;
                float d = 1.0e10;
                for (int xo = -1; xo <= 1; xo++)
                {
                    for (int yo = -1; yo <= 1; yo++)
                    {
                        for (int zo = -1; zo <= 1; zo++)
                        {
                            float3 tp = floor(pCell) + float3(xo, yo, zo);

                            tp = pCell - tp - hashF3F3C002(mod(tp, cellCount));//change noise here

                            //d = min(d, dot(tp, tp));
                            d = min(d, length(tp));
                        }
                    }
                }
                return saturate(d);
            }

            float FbmWorley(float3 p, float c)
            {
                float H = 1.15;
                float G = exp2(-H);
                float f = 1.0;
                float a = 1.0;
                float b = 0.0;
                float wn = 0.0;
                int numOctaves = 10;
                for (int i = 0; i < numOctaves; i++)
                {
                    wn += a * (1 - CellsTileableF3F1Sebh(p * f, c));
                    b += a;
                    f *= 2.0;
                    a *= G;
                }
                return (wn / b);
            }

            float PerlinWorleyNoise(float3 p, float4 np0, float4 np1, float4 np2)
            {
                return remap(PerlinNoiseF3F1Sebh(p * np0.x + np0.yzw, np2.y, clamp((int)ceil(np2.z), 1, 32)) * np2.w + (1 - np2.w), 1 - FbmWorley(p * np1.x + np1.yzw, np2.x), 1, 0, 1);
            }

            float4 frag (Vert2Frag i) : SV_Target
            {
                float n = PerlinWorleyNoise(float3(i.uv, _ViewDepth), float4(1, 0, 0, 0), float4(1, 0, 0, 0), float4(10, 4, 4, 0.5));
                float4 col = _Color;
                col.xyz *= n;
                return col;
            }
            ENDCG
        }
    }
}
/*PROPBEGIN
* Buffer: Name=_PerlinNoise3DParams; Desc=Perlin Noise Params; Stride=4;
PROPEND*/