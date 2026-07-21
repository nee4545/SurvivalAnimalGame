Shader "Custom/MobileStylizedWaterBasic_FoamFlow"
{
    Properties
    {
        _BaseColor ("Base Water Color", Color) = (0.15, 0.65, 0.85, 0.55)
        _DeepColor ("Deep Water Color", Color) = (0.02, 0.18, 0.35, 0.75)
        _FresnelColor ("Fresnel Color", Color) = (0.8, 1, 1, 1)

        _Alpha ("Alpha", Range(0, 1)) = 0.65
        _WaveSpeed ("Wave Speed", Float) = 0.7
        _WaveScale ("Wave Scale", Float) = 10.0
        _WaveStrength ("Wave Strength", Range(0, 1)) = 0.015

        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0, 2)) = 0.45

        _FoamColor ("Foam Color", Color) = (0.85, 1.0, 0.95, 1.0)
        _FoamDistance ("Foam Distance", Float) = 1.2
        _FoamSoftness ("Foam Softness", Range(0.01, 2)) = 0.35
        _FoamStrength ("Foam Strength", Range(0, 1)) = 0.75
        _FoamNoiseScale ("Foam Noise Scale", Float) = 18.0
        _FoamNoiseSpeed ("Foam Noise Speed", Float) = 0.35
        _FoamNoiseAmount ("Foam Noise Amount", Range(0, 1)) = 0.45

        _FlowStreakColor ("Flow Streak Color", Color) = (0.75, 1.0, 0.95, 1.0)
        _FlowStreakStrength ("Flow Streak Strength", Range(0, 1)) = 0.28
        _FlowStreakSpeed ("Flow Streak Speed", Float) = 1.0
        _FlowStreakScale ("Flow Streak Scale", Float) = 10.0
        _FlowStreakStretch ("Flow Streak Stretch", Float) = 8.0
        _FlowStreakSharpness ("Flow Streak Sharpness", Range(0, 1)) = 0.72
        _FlowUseVAsForward ("Flow Use V As Forward", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;

            fixed4 _BaseColor;
            fixed4 _DeepColor;
            fixed4 _FresnelColor;

            float _Alpha;
            float _WaveSpeed;
            float _WaveScale;
            float _WaveStrength;
            float _FresnelPower;
            float _FresnelStrength;

            fixed4 _FoamColor;
            float _FoamDistance;
            float _FoamSoftness;
            float _FoamStrength;
            float _FoamNoiseScale;
            float _FoamNoiseSpeed;
            float _FoamNoiseAmount;

            fixed4 _FlowStreakColor;
            float _FlowStreakStrength;
            float _FlowStreakSpeed;
            float _FlowStreakScale;
            float _FlowStreakStretch;
            float _FlowStreakSharpness;
            float _FlowUseVAsForward;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
                float eyeDepth : TEXCOORD5;
            };

            float SimpleFoamNoise(float2 uv)
            {
                float n1 =
                    sin((uv.x + _Time.y * _FoamNoiseSpeed) * _FoamNoiseScale) *
                    cos((uv.y - _Time.y * _FoamNoiseSpeed * 0.7) * _FoamNoiseScale);

                float n2 =
                    sin((uv.x * 1.7 - _Time.y * _FoamNoiseSpeed * 0.5) * _FoamNoiseScale * 0.55) *
                    cos((uv.y * 1.4 + _Time.y * _FoamNoiseSpeed) * _FoamNoiseScale * 0.55);

                float noise = (n1 + n2) * 0.5;

                return saturate(noise * 0.5 + 0.5);
            }

            float FlowStreaks(float2 uv)
            {
                float2 flowUV = lerp(uv, uv.yx, _FlowUseVAsForward);

                float along = flowUV.x + _Time.y * _FlowStreakSpeed;
                float across = flowUV.y;

                float stripe = sin(
                    across * _FlowStreakScale * _FlowStreakStretch +
                    sin(along * _FlowStreakScale * 0.55) * 1.65
                );

                stripe = saturate(stripe * 0.5 + 0.5);
                stripe = smoothstep(_FlowStreakSharpness, 1.0, stripe);

                float broken =
                    sin(along * _FlowStreakScale * 2.2 + sin(across * _FlowStreakScale) * 2.0) *
                    cos(along * _FlowStreakScale * 0.7 + _Time.y * _FlowStreakSpeed * 0.6);

                broken = saturate(broken * 0.5 + 0.5);
                broken = smoothstep(0.35, 1.0, broken);

                float softBand = sin(along * _FlowStreakScale * 0.25 + across * 2.0);
                softBand = saturate(softBand * 0.5 + 0.5);

                return saturate(stripe * broken * lerp(0.45, 1.0, softBand));
            }

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
                o.screenPos = ComputeScreenPos(o.pos);
                o.eyeDepth = -UnityObjectToViewPos(v.vertex).z;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float wavePattern =
                    sin((i.uv.x + _Time.y * 0.08) * 25.0) *
                    cos((i.uv.y + _Time.y * 0.06) * 25.0);

                float waterBlend = saturate(wavePattern * 0.5 + 0.5);

                fixed4 col = lerp(_DeepColor, _BaseColor, waterBlend);

                float flowStreak = FlowStreaks(i.uv) * _FlowStreakStrength;

                col.rgb = lerp(
                    col.rgb,
                    _FlowStreakColor.rgb,
                    saturate(flowStreak)
                );

                float fresnel =
                    pow(
                        1.0 - saturate(dot(normalize(i.worldNormal), normalize(i.viewDir))),
                        _FresnelPower
                    );

                fresnel *= _FresnelStrength;

                col.rgb += _FresnelColor.rgb * fresnel;

                float rawSceneDepth =
                    SAMPLE_DEPTH_TEXTURE_PROJ(
                        _CameraDepthTexture,
                        UNITY_PROJ_COORD(i.screenPos)
                    );

                float sceneEyeDepth = LinearEyeDepth(rawSceneDepth);

                float depthDifference = sceneEyeDepth - i.eyeDepth;

                float foamBase =
                    1.0 - smoothstep(
                        0.0,
                        max(0.001, _FoamDistance),
                        depthDifference
                    );

                foamBase = saturate(foamBase);

                float foamNoise = SimpleFoamNoise(i.uv);

                float brokenFoam =
                    foamBase *
                    lerp(1.0, foamNoise, _FoamNoiseAmount);

                brokenFoam =
                    smoothstep(
                        _FoamSoftness * 0.15,
                        _FoamSoftness,
                        brokenFoam
                    );

                brokenFoam *= _FoamStrength;

                col.rgb =
                    lerp(
                        col.rgb,
                        _FoamColor.rgb,
                        saturate(brokenFoam)
                    );

                col.a = saturate(_Alpha + brokenFoam * 0.25 + flowStreak * 0.08);

                return col;
            }

            ENDCG
        }
    }

    FallBack Off
}
