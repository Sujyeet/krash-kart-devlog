Shader "KartGame/BlinkWarp"
{
    Properties
    {
        _Color ("Main Color", Color) = (0.0, 0.85, 1.0, 1.0)
        _SecondaryColor ("Accent Glow", Color) = (0.85, 0.25, 1.0, 1.0)
        _Glow ("Glow Intensity", Range(0.5, 4.0)) = 2.2
        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0
        _AnimProgress ("Anim Progress", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+120"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _SecondaryColor;
                float  _Glow;
                float  _Alpha;
                float  _AnimProgress;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = normalize(GetCameraPositionWS() - positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Fresnel glow on edges
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, 2.2);

                // Procedural warp noise bands
                float band = sin((uv.y * 18.0) - (_AnimProgress * 25.0)) * 0.5 + 0.5;
                float energy = saturate(fresnel * 0.7 + band * 0.4);

                half4 finalColor = lerp(_Color, _SecondaryColor, energy);
                finalColor.rgb *= _Glow;
                finalColor.a = saturate((fresnel + band * 0.5) * _Alpha);

                return finalColor;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+120"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float2 uv        : TEXCOORD0;
                float3 normalWS  : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            float4 _Color;
            float4 _SecondaryColor;
            float  _Glow;
            float  _Alpha;
            float  _AnimProgress;

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.viewDirWS = normalize(_WorldSpaceCameraPos - worldPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normalWS = normalize(i.normalWS);
                float3 viewDirWS = normalize(i.viewDirWS);

                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, 2.2);

                float band = sin((i.uv.y * 18.0) - (_AnimProgress * 25.0)) * 0.5 + 0.5;
                float energy = saturate(fresnel * 0.7 + band * 0.4);

                fixed4 finalColor = lerp(_Color, _SecondaryColor, energy);
                finalColor.rgb *= _Glow;
                finalColor.a = saturate((fresnel + band * 0.5) * _Alpha);

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

