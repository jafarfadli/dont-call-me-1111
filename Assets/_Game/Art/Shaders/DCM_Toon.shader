// Comic-book toon shader for the room.
// Flat albedo, hard light bands, hard sun shadows, optional ink detail texture,
// world-space hatching in shade and a hard glint. Outlines come from the
// fullscreen ink pass (DCM_InkComposite), which needs the DepthNormals pass below.
Shader "DontCallMe/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _ShadeTint ("Shade Tint", Color) = (1, 1, 1, 1)

        [Header(Light Bands)]
        _LightThreshold ("Light Threshold", Range(-1, 1)) = 0.0
        _LightSoftness ("Light Softness", Range(0.001, 0.5)) = 0.03

        [Header(Ink Detail)]
        _DetailMap ("Ink Detail (grayscale, multiplied)", 2D) = "white" {}
        [Enum(UV, 0, Object Triplanar, 1, World Triplanar, 2)] _DetailMode ("Detail Projection", Float) = 1
        _DetailScale ("Detail Tiles Per Meter", Float) = 1
        _DetailStrength ("Detail Strength", Range(0, 1)) = 0.8

        [Header(Hatching)]
        _HatchStrength ("Hatch Strength", Range(0, 1)) = 0.3
        _HatchDensity ("Hatch Lines Per Meter", Float) = 38
        _HatchWidth ("Hatch Line Width", Range(0.02, 0.6)) = 0.2

        [Header(Highlights)]
        _GlintColor ("Glint Color (A = strength)", Color) = (1, 1, 1, 0)
        _GlintSize ("Glint Size", Range(0, 1)) = 0.06
        _RimColor ("Rim Color (A = strength)", Color) = (1, 1, 1, 0)
        _RimSize ("Rim Size", Range(0, 1)) = 0.25

        [Header(Emission)]
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionMap ("Emission Map", 2D) = "white" {}

        [Header(Surface)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        [HideInInspector] _InkId ("Ink Id (outline between different ids)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadeTint;
            half _LightThreshold;
            half _LightSoftness;
            float4 _DetailMap_ST;
            half _DetailMode;
            float _DetailScale;
            half _DetailStrength;
            half _HatchStrength;
            float _HatchDensity;
            half _HatchWidth;
            half4 _GlintColor;
            half _GlintSize;
            half4 _RimColor;
            half _RimSize;
            half4 _EmissionColor;
            half _Cutoff;
            half _InkId;
        CBUFFER_END

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ToonVertex
            #pragma fragment ToonFragment

            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "DCM_ToonCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                half3 normalOS : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ToonVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            // Band a punctual light: hard edge on N.L, two-step falloff on distance.
            half PunctualBand(Light light, half3 normalWS)
            {
                half nl = ToonStep(dot(normalWS, light.direction), _LightThreshold, _LightSoftness);
                half falloff = 0.55h * ToonStep(light.distanceAttenuation, 0.06h, 0.015h)
                             + 0.45h * ToonStep(light.distanceAttenuation, 0.22h, 0.03h);
                half shadow = ToonStep(light.shadowAttenuation, 0.5h, 0.12h);
                return nl * falloff * shadow;
            }

            half4 ToonFragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedoAlpha = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)) * _BaseColor;
                #if defined(_ALPHATEST_ON)
                    clip(albedoAlpha.a - _Cutoff);
                #endif

                half3 normalWS = normalize(input.normalWS) * (isFrontFace ? 1.0h : -1.0h);
                half3 normalOS = normalize(input.normalOS) * (isFrontFace ? 1.0h : -1.0h);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                half detail = SampleInkDetail(input.uv, input.positionOS, normalOS, input.positionWS, normalWS);
                half3 albedo = albedoAlpha.rgb * lerp(1.0h, detail, _DetailStrength);

                // LIGHT_LOOP_BEGIN expects a variable named inputData.
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                // Contact shadows: banded SSAO so they read as flat inked shapes.
                half ao = 1.0h;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    ao = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV).indirectAmbientOcclusion;
                #endif
                half aoThreshold = _DCM_ShadeParams.z > 0.0h ? _DCM_ShadeParams.z : 0.55h;
                half aoSoftness = _DCM_ShadeParams.w > 0.0h ? _DCM_ShadeParams.w : 0.05h;
                half aoBand = ToonStep(ao, aoThreshold, aoSoftness);

                half3 ambient = SampleSH(normalWS) * _ShadeTint.rgb * lerp(0.8h, 1.0h, aoBand);

                Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
                half mainBand = ToonStep(dot(normalWS, mainLight.direction), _LightThreshold, _LightSoftness);
                half mainShadow = ToonStep(mainLight.shadowAttenuation, 0.5h, 0.035h);
                half mainTerm = mainBand * mainShadow * mainLight.distanceAttenuation;

                half3 direct = mainLight.color * mainTerm;
                half directAmount = mainTerm * saturate(Luminance(mainLight.color));

                // Window bounce fill: a shadowless directional band that gives every object a lit and a shade side.
                half fillThreshold = _DCM_ShadeParams.x + _LightThreshold;
                half fillSoftness = _DCM_ShadeParams.y > 0.0h ? _DCM_ShadeParams.y : _LightSoftness;
                half fillBand = ToonStep(dot(normalWS, normalize(_DCM_FillDir.xyz + 1e-5)), fillThreshold, fillSoftness) * aoBand;
                direct += _DCM_FillColor.rgb * fillBand;
                directAmount = max(directAmount, fillBand * saturate(Luminance(_DCM_FillColor.rgb)));

                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();

                    #if USE_CLUSTER_LIGHT_LOOP
                    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        Light dirLight = GetAdditionalLight(dirIndex, inputData.positionWS, inputData.shadowMask);
                        half band = ToonStep(dot(normalWS, dirLight.direction), _LightThreshold, _LightSoftness);
                        direct += dirLight.color * band;
                        directAmount = max(directAmount, band * saturate(Luminance(dirLight.color)));
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
                        half band = PunctualBand(light, normalWS);
                        direct += light.color * band;
                        directAmount = max(directAmount, band * saturate(Luminance(light.color)));
                    LIGHT_LOOP_END
                #endif

                half3 color = albedo * (ambient + direct);

                // Hatching only where no direct light lands.
                half hatchMask = 1.0h - saturate(directAmount * 2.5h);
                half hatch = HatchLines(input.positionWS, normalWS, _HatchDensity, _HatchWidth);
                color *= 1.0h - hatch * hatchMask * _HatchStrength;

                // Hard glint from the sun.
                half3 halfDir = normalize(mainLight.direction + viewDirWS);
                half glint = ToonStep(dot(normalWS, halfDir), 1.0h - _GlintSize * 0.25h, 0.004h) * mainTerm;
                color += _GlintColor.rgb * (_GlintColor.a * glint);

                // Thin rim on lit side.
                half rim = ToonStep(1.0h - saturate(dot(normalWS, viewDirWS)), 1.0h - _RimSize, 0.02h);
                color += _RimColor.rgb * (_RimColor.a * rim * saturate(directAmount + 0.25h));

                color += SampleEmission(input.uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex NormalsVertex
            #pragma fragment NormalsFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            struct NormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct NormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            NormalsVaryings NormalsVertex(NormalsAttributes input)
            {
                NormalsVaryings output = (NormalsVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                return output;
            }

            half4 NormalsFragment(NormalsVaryings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                #if defined(_ALPHATEST_ON)
                    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
                #endif
                half3 normalWS = normalize(input.normalWS) * (isFrontFace ? 1.0h : -1.0h);
                // Alpha carries the material's ink id; the ink pass outlines id changes.
                return half4(normalWS, _InkId);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
