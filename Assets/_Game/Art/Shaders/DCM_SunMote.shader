// Dust mote particle that only shows where the sun reaches: it samples the
// main light shadow map at its own position, so motes glow inside the sunbeam
// and vanish in shade. That draws the beam without a fake volume mesh.
Shader "DontCallMe/SunMote"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.93, 0.78, 1)
        _Intensity ("Intensity", Float) = 1.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Mote"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex MoteVertex
            #pragma fragment MoteFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings MoteVertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 MoteFragment(Varyings input) : SV_Target
            {
                half2 d = input.uv * 2.0h - 1.0h;
                half disc = saturate(1.0h - dot(d, d));
                disc *= disc;
                half shadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(input.positionWS));
                half lit = smoothstep(0.35h, 0.65h, shadow);
                half alpha = disc * lit * input.color.a;
                return half4(_Color.rgb * input.color.rgb * _Intensity, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
