Shader "Custom/MobileStylizedWaterBasic"
{
    Properties
    {
        _BaseColor ("Base Water Color", Color) = (0.15, 0.65, 0.85, 0.55)
        _DeepColor ("Deep Water Color", Color) = (0.02, 0.18, 0.35, 0.75)
        _FresnelColor ("Fresnel Color", Color) = (0.8, 1, 1, 1)

        _Alpha ("Alpha", Range(0, 1)) = 0.65
        _WaveSpeed ("Wave Speed", Float) = 0.7
        _WaveScale ("Wave Scale", Float) = 10.0
        _WaveStrength ("Wave Strength", Range(0, 1)) = 0.04

        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0, 2)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            fixed4 _DeepColor;
            fixed4 _FresnelColor;

            float _Alpha;
            float _WaveSpeed;
            float _WaveScale;
            float _WaveStrength;
            float _FresnelPower;
            float _FresnelStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float wave =
                    sin((v.vertex.x + _Time.y * _WaveSpeed) * _WaveScale) *
                    cos((v.vertex.z + _Time.y * _WaveSpeed) * _WaveScale);

                v.vertex.y += wave * _WaveStrength;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                o.viewDir = normalize(_WorldSpaceCameraPos.xyz - o.worldPos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float wavePattern =
                    sin((i.uv.x + _Time.y * 0.08) * 25.0) *
                    cos((i.uv.y + _Time.y * 0.06) * 25.0);

                float waterBlend = saturate(wavePattern * 0.5 + 0.5);

                fixed4 col = lerp(_DeepColor, _BaseColor, waterBlend);

                float fresnel = pow(1.0 - saturate(dot(normalize(i.worldNormal), normalize(i.viewDir))), _FresnelPower);
                fresnel *= _FresnelStrength;

                col.rgb += _FresnelColor.rgb * fresnel;
                col.a = _Alpha;

                return col;
            }
            ENDCG
        }
    }
}
