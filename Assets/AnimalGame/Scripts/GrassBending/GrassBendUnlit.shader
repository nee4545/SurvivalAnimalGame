Shader "URP/GrassBendLit"
{
    Properties
    {
        _BaseMap   ("Base Map (RGBA)", 2D) = "white" {}
        _BaseColor ("Base Color", Color)    = (1,1,1,1)

        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clipping", Float) = 1
        _Cutoff    ("Clip Threshold", Range(0,1)) = 0.5

        [Toggle(_NORMALMAP)] _UseNormalMap ("Use Normal Map", Float) = 0
        _BumpMap   ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0,2)) = 1

        _Smoothness("Smoothness", Range(0,1)) = 0.35
        _SpecColor ("Specular Color", Color)  = (0.2,0.2,0.2,1)
    }

    SubShader
    {
        Tags {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="TransparentCutout"
            "Queue"="AlphaTest"
        }

        Cull Off          // two-sided grass cards
        ZWrite On
        Blend One Zero

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT // receives main light softness if project uses it
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // --- Material textures
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            // --- Material constants
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _Cutoff;
                float  _UseNormalMap;
                float  _BumpScale;
                float  _Smoothness;
                float4 _SpecColor;
            CBUFFER_END

            // --- Bend globals (set by your driver script)
            float3 _BendOrigin;
            float  _BendRadius;
            float  _BendStrength;
            float  _BendTipWeight;
            float  _BendMaxTipHeight;

            struct Attributes {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float4 tangentWS  : TEXCOORD3; // xyz=tangent, w=sign
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float smooth01(float x){ x=saturate(x); return x*x*(3.0-2.0*x); }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // UV
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                // Object->World
                float3 posWS = TransformObjectToWorld(IN.positionOS);

                // --- Bend in world XZ
                float2 dXZ   = posWS.xz - _BendOrigin.xz;
                float  dist  = length(dXZ);
                float2 dirXZ = (dist > 1e-5) ? dXZ / dist : float2(0,0);

                float fall = 1.0 - saturate(dist / max(_BendRadius, 1e-4));
                fall = smooth01(fall);

                float tipLocal01 = saturate(IN.positionOS.y / max(_BendMaxTipHeight, 1e-4));
                float tip = lerp(1.0, tipLocal01, saturate(_BendTipWeight));

                float3 dispWS = float3(dirXZ.x, 0, dirXZ.y) * (_BendStrength * fall * tip);
                posWS += dispWS;

                // Outputs
                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                float3 tanWS   = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.tangentWS  = float4(normalize(tanWS), IN.tangentOS.w);

                return OUT;
            }

            // Tangent-space normal to world
            float3 GetWSNormal(float3 nWS, float4 tWS, float3 nTS)
            {
                float3 N = normalize(nWS);
                float3 T = normalize(tWS.xyz);
                float3 B = normalize(cross(N, T) * tWS.w);
                float3x3 TBN = float3x3(T, B, N);
                return normalize(mul(nTS, TBN));
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample textures
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                // Normal
                float3 nWS = normalize(IN.normalWS);
                #if defined(_NORMALMAP)
                    float3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                    nWS = GetWSNormal(nWS, IN.tangentWS, nTS);
                #endif

                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float NdotL = saturate(dot(nWS, mainLight.direction));
                float3 diffuse = albedo.rgb * (mainLight.color * NdotL);

                // Additional lights
                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < pixelLightCount; i++)
                {
                    Light l = GetAdditionalLight(i, IN.positionWS);
                    diffuse += albedo.rgb * l.color * saturate(dot(nWS, l.direction));
                }
                #endif

                // Ambient (SH)
                float3 ambient = SampleSH(nWS) * albedo.rgb;

                // Simple specular (Blinn-Phong-ish)
                float3 H = normalize(mainLight.direction + V);
                float  NdotH = saturate(dot(nWS, H));
                float  shininess = lerp(8.0, 128.0, _Smoothness);
                float3 spec = _SpecColor.rgb * pow(NdotH, shininess) * mainLight.color * step(0.0, NdotL);

                // Fog (URP)
                float3 color = diffuse + ambient + spec;
                color = MixFog(color, ComputeFogFactor(IN.positionCS.z));

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
