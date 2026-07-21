Shader"Custom/RiverTargetOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.85, 0.15, 1)
        _OutlineWidth ("Outline Width", Float) = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
Cull Front

ZWrite Off

ZTest LEqual

Blend SrcAlpha
OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

#include "UnityCG.cginc"

float4 _OutlineColor;
float _OutlineWidth;

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
};

struct v2f
{
    float4 pos : SV_POSITION;
};

v2f vert(appdata v)
{
    v2f o;

    float3 normal = normalize(v.normal);
    float4 expandedVertex = v.vertex;

    expandedVertex.xyz += normal * _OutlineWidth;

    o.pos = UnityObjectToClipPos(expandedVertex);

    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    return _OutlineColor;
}
            ENDCG
        }
    }
}