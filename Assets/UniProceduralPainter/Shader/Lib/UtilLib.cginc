#ifndef UTILLIB_INCLUDE
#define UTILLIB_INCLUDE

//ref: SRP
float2 GetFullScreenTriangleTexCoord(uint vertexID)
{
#if UNITY_UV_STARTS_AT_TOP
    return float2((vertexID << 1) & 2, 1.0 - (vertexID & 2));
#else
    return float2((vertexID << 1) & 2, vertexID & 2);
#endif
}

float4 GetFullScreenTriangleVertexPosition(uint vertexID, float z = 1)//given api near clip value
{
    // note: the triangle vertex position coordinates are x2 so the returned UV coordinates are in range -1, 1 on the screen.
    float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
    float4 pos = float4(uv * 2.0 - 1.0, z, 1.0);
    return pos;
}

struct Vert2Frag
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
};

Vert2Frag FullScreenTri(uint vertexID : SV_VertexID)
{
    Vert2Frag o;
    o.vertex = GetFullScreenTriangleVertexPosition(vertexID);
    o.uv = GetFullScreenTriangleTexCoord(vertexID);
    return o;
}


#endif