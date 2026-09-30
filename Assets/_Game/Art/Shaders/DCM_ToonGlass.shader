// Comic glass: a light tint, a bright fresnel edge and two diagonal white
// streaks, the way glass is drawn in comics. Streaks run in world space so
// every pane gets diagonal ones whatever its orientation.
Shader "DontCallMe/ToonGlass"
{
    Properties
    {
        _TintColor ("Tint (A = opacity)", Color) = (0.78, 0.9, 0.95, 0.12)
        _EdgeColor ("Edge Color (A = opacity)", Color) = (1, 1, 1, 0.35)
        _EdgePower ("Edge Power", Range(0.5, 8)) = 3
        _StreakColor ("Streak Color (A = opacity)", Color) = (1, 1, 1, 0.45)
        _StreakDirection ("Streak Direction (world space)", Vector) = (1, 1.3, 0.8, 0)
        _StreakFrequency ("Streak Repeat Per Meter", Float) = 1.4
        _StreakWidth ("Streak Width", Range(0.01, 0.4)) = 0.09
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
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
            Name "ForwardGlass"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GlassVertex
            #pragma fragment GlassFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
                half4 _EdgeColor;
                half _EdgePower;
                half4 _StreakColor;
                float4 _StreakDirection;
                float _StreakFrequency;
                half _StreakWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings GlassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half Band(float u, half width)
            {
                float d = abs(frac(u) - 0.5) * 2.0;
                float aa = max(fwidth(u) * 1.5, 1e-4);
                return smoothstep(1.0 - width - aa, 1.0 - width + aa, d);
            }

            half4 GlassFragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 n = normalize(input.normalWS) * (isFrontFace ? 1.0h : -1.0h);
                half3 v = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = pow(1.0h - saturate(dot(n, v)), _EdgePower);

                float u = dot(input.positionWS, normalize(_StreakDirection.xyz)) * _StreakFrequency;
                half streak = max(Band(u, _StreakWidth), Band(u + 0.17, _StreakWidth * 0.4));

                Light mainLight = GetMainLight();
                half3 color = _TintColor.rgb * (SampleSH(n) + mainLight.color * 0.25h);
                half alpha = _TintColor.a;

                color = lerp(color, _EdgeColor.rgb, fresnel * _EdgeColor.a);
                alpha = max(alpha, fresnel * _EdgeColor.a);
                color = lerp(color, _StreakColor.rgb, streak * _StreakColor.a);
                alpha = max(alpha, streak * _StreakColor.a);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
