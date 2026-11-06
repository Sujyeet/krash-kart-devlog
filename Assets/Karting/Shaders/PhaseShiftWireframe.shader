Shader "KartGame/PhaseShiftWireframe"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0.0, 0.85, 1.0, 1.0)
        _GlowColor ("Glow Color", Color) = (0.1, 0.5, 1.0, 1.0)
        _GlowIntensity ("Glow Intensity", Range(0.5, 5.0)) = 2.2
        _GridScale ("Grid Scale", Range(0.05, 1.0)) = 0.22
        _GridThickness ("Grid Thickness", Range(0.01, 0.15)) = 0.04
        _ScanlineSpeed ("Scanline Speed", Range(0.5, 10.0)) = 3.5
        _ScanlineThickness ("Scanline Thickness", Range(0.01, 0.25)) = 0.08
        _FlickerSpeed ("Flicker Speed", Range(1.0, 20.0)) = 8.0
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
        _RimIntensity ("Rim Intensity", Range(0.5, 4.0)) = 1.5
        _Alpha ("Alpha", Range(0.0, 1.0)) = 0.85
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+150"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha One // Additive hologram appearance

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GridColor;
                float4 _GlowColor;
                float  _GlowIntensity;
                float  _GridScale;
                float  _GridThickness;
                float  _ScanlineSpeed;
                float  _ScanlineThickness;
                float  _FlickerSpeed;
                float  _RimPower;
                float  _RimIntensity;
                float  _Alpha;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(posWS);
                output.worldPos   = posWS;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS  = normalize(GetCameraPositionWS() - posWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 v = normalize(input.viewDirWS);

                // Triplanar procedural wireframe grid in world coordinates
                float3 wPos = input.worldPos * _GridScale;
                float3 absN = abs(n);
                float gx = 1.0 - smoothstep(0.0, _GridThickness, frac(wPos.x));
                float gy = 1.0 - smoothstep(0.0, _GridThickness, frac(wPos.y));
                float gz = 1.0 - smoothstep(0.0, _GridThickness, frac(wPos.z));
                float grid = saturate(gx * absN.x + gy * absN.y + gz * absN.z);

                // Outer silhouette emphasis (Fresnel rim)
                float NdotV = saturate(dot(n, v));
                float rim = pow(1.0 - NdotV, _RimPower) * _RimIntensity;

                // Vertical moving holographic scanlines
                float scan = frac((_Time.y * _ScanlineSpeed) + (input.worldPos.y * 0.4));
                float scanMask = 1.0 - smoothstep(0.5 - _ScanlineThickness, 0.5 + _ScanlineThickness, scan);

                // Hologram power flicker
                float flicker = 0.88 + 0.12 * sin((_Time.y * _FlickerSpeed) + input.worldPos.x);

                // Total hologram composite mask
                float wireframeMask = saturate((grid + rim * 0.6) * scanMask * flicker);

                float3 baseGlow = _GlowColor.rgb * _GridColor.rgb * _GlowIntensity;
                half4 col;
                col.rgb = baseGlow * wireframeMask;
                col.a   = saturate(wireframeMask * _Alpha);
                return col;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+150"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float3 worldPos  : TEXCOORD0;
                float3 normalWS  : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            float4 _GridColor;
            float4 _GlowColor;
            float  _GlowIntensity;
            float  _GridScale;
            float  _GridThickness;
            float  _ScanlineSpeed;
            float  _ScanlineThickness;
            float  _FlickerSpeed;
            float  _RimPower;
            float  _RimIntensity;
            float  _Alpha;

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos       = UnityObjectToClipPos(v.vertex);
                o.worldPos  = worldPos;
                o.normalWS  = UnityObjectToWorldNormal(v.normal);
                o.viewDirWS = normalize(_WorldSpaceCameraPos - worldPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewDirWS);

                float3 wPos = i.worldPos * _GridScale;
                float3 absN = abs(n);
                float gx = 1.0 - smoothstep(0.0, _GridThickness, frac(wPos.x));
                float gy = 1.0 - smoothstep(0.0, _GridThickness, frac(wPos.y));
                float gz = 1.0 - smoothstep(0.0, _GridThickness, frac(wPos.z));
                float grid = saturate(gx * absN.x + gy * absN.y + gz * absN.z);

                float NdotV = saturate(dot(n, v));
                float rim = pow(1.0 - NdotV, _RimPower) * _RimIntensity;

                float scan = frac((_Time.y * _ScanlineSpeed) + (i.worldPos.y * 0.4));
                float scanMask = 1.0 - smoothstep(0.5 - _ScanlineThickness, 0.5 + _ScanlineThickness, scan);

                float flicker = 0.88 + 0.12 * sin((_Time.y * _FlickerSpeed) + i.worldPos.x);

                float wireframeMask = saturate((grid + rim * 0.6) * scanMask * flicker);

                float3 baseGlow = _GlowColor.rgb * _GridColor.rgb * _GlowIntensity;
                fixed4 col;
                col.rgb = baseGlow * wireframeMask;
                col.a   = saturate(wireframeMask * _Alpha);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

