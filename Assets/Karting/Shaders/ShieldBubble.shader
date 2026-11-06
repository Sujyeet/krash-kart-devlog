Shader "KartGame/ShieldBubble"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.15, 0.55, 1.0, 0.22)
        _RimColor  ("Rim Color",  Color) = (0.45, 0.85, 1.0, 0.75)
        _RimPower  ("Rim Power", Range(0.5, 8.0)) = 2.8
        _Glow      ("Glow Intensity", Range(0.5, 3.0)) = 1.4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float  _RimPower;
                float  _Glow;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 normalWS  = normalize(input.normalWS);

                float NdotV = saturate(dot(normalWS, viewDirWS));
                float rim   = pow(1.0 - NdotV, _RimPower);

                half4 color;
                color.rgb = _BaseColor.rgb + (_RimColor.rgb * rim * _Glow);
                color.a   = saturate(_BaseColor.a + (rim * _RimColor.a));
                return color;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
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
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
            };

            fixed4 _BaseColor;
            fixed4 _RimColor;
            float  _RimPower;
            float  _Glow;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 normal  = normalize(i.worldNormal);

                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, _RimPower);

                fixed4 color;
                color.rgb = _BaseColor.rgb + (_RimColor.rgb * rim * _Glow);
                color.a   = saturate(_BaseColor.a + (rim * _RimColor.a));
                return color;
            }
            ENDCG
        }
    }
}

