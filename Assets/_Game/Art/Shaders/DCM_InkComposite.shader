// Fullscreen ink pass: draws comic outlines from depth and normals and prints
// the frame on paper grain. Depth is read per pixel; normals are read between
// pixels (bilinear, 2x2 average) so faces thinner than a pixel still give a
// continuous crease line instead of dashes. The normals alpha holds each
// material's ink id: every change of id is outlined, which inks parts that sit
// almost flush (skirting on a wall, rug on a floor, picture in a frame). A negative
// id marks an object the player can use: its silhouette gets a glowing outline that
// pulses with the global _DCM_HighlightPulse.
// Used by a Full Screen Pass Renderer Feature (requirements: Depth + Normal).
Shader "Hidden/DontCallMe/InkComposite"
{
    Properties
    {
        _InkColor ("Ink Color", Color) = (0.13, 0.11, 0.17, 1)
        _Thickness ("Line Radius (px at 1080p)", Range(0.5, 4)) = 1
        _DepthThreshold ("Depth Threshold (relative)", Range(0.001, 0.3)) = 0.008
        _NormalThreshold ("Normal Threshold", Range(0.01, 1)) = 0.35
        _CreaseStrength ("Crease Line Strength", Range(0, 1)) = 0.85
        _IdStrength ("Material Outline Strength", Range(0, 1)) = 0.9
        _InkVariation ("Ink Density Variation", Range(0, 1)) = 0.3
        _GrainTex ("Grain (R fine, GB smooth)", 2D) = "gray" {}
        _GrainStrength ("Grain Strength", Range(0, 1)) = 0.1
        _FadeStart ("Line Fade Start (m)", Float) = 14
        _FadeEnd ("Line Fade End (m)", Float) = 40
        _PaperTint ("Paper Tint", Color) = (1, 0.985, 0.955, 1)
        _HighlightColor ("Highlight Outline (A = strength)", Color) = (1, 0.82, 0.25, 0.95)
        _HighlightWidth ("Highlight Radius (px at 1080p)", Range(0.5, 8)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "InkComposite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment InkFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4 _InkColor;
            float _Thickness;
            float _DepthThreshold;
            float _NormalThreshold;
            half _CreaseStrength;
            half _IdStrength;
            half _InkVariation;
            TEXTURE2D(_GrainTex);
            SAMPLER(sampler_GrainTex);
            float4 _GrainTex_TexelSize;
            half _GrainStrength;
            float _FadeStart;
            float _FadeEnd;
            half4 _PaperTint;
            half4 _HighlightColor;
            float _HighlightWidth;
            float _DCM_HighlightPulse;

            static const int2 Ring[8] = { int2(-1, -1), int2(0, -1), int2(1, -1), int2(-1, 0), int2(1, 0), int2(-1, 1), int2(0, 1), int2(1, 1) };

            bool IsFar(float rawDepth)
            {
                #if UNITY_REVERSED_Z
                    return rawDepth <= 1e-6;
                #else
                    return rawDepth >= 1.0 - 1e-6;
                #endif
            }

            // Inverse linear depth is affine across a plane in screen space: its second
            // difference is ~0 on flat surfaces and spikes at silhouettes and creases.
            float InvDepthAt(int2 p, int2 size)
            {
                p = clamp(p, int2(0, 0), size - 1);
                float raw = LoadSceneDepth(uint2(p));
                return IsFar(raw) ? 0.0 : 1.0 / LinearEyeDepth(raw, _ZBufferParams);
            }

            // Negative for a highlighted object; its magnitude is the material's ink id.
            float InkIdAt(int2 p, int2 size)
            {
                p = clamp(p, int2(0, 0), size - 1);
                return LOAD_TEXTURE2D_X(_CameraNormalsTexture, uint2(p)).a;
            }

            bool Marked(float id) { return id < -0.004; }

            // Bilinear read centred on a pixel corner: averages the 2x2 block around it.
            float3 NormalBetween(float2 pixelCorner)
            {
                float2 uv = pixelCorner / _ScreenParams.xy;
                return SampleSceneNormals(uv, sampler_LinearClamp);
            }

            half4 InkFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

                int2 size = int2(_ScreenParams.xy);
                int2 pix = int2(uv * _ScreenParams.xy);
                int r = max(1, (int)round(_Thickness * _ScreenParams.y / 1080.0));

                float wC = InvDepthAt(pix, size);
                float2 corner = float2(pix) + 1.0;
                float3 nC = NormalBetween(corner);
                float w[8];
                float normalDiff = 0.0;
                float rawC = InkIdAt(pix, size);
                float idC = abs(rawC);
                float idDiff = 0.0;
                float wMax = wC;
                [unroll] for (int i = 0; i < 8; i++)
                {
                    int2 q = pix + Ring[i] * r;
                    w[i] = InvDepthAt(q, size);
                    wMax = max(wMax, w[i]);
                    if (wC > 0.0 && w[i] > 0.0)
                    {
                        normalDiff = max(normalDiff, 1.0 - dot(nC, NormalBetween(corner + float2(Ring[i] * r))));
                        idDiff = max(idDiff, abs(abs(InkIdAt(q, size)) - idC));
                    }
                }
                // Opposite pairs: (0,7) (1,6) (2,5) (3,4)
                float lap = max(max(abs(w[0] + w[7] - 2.0 * wC), abs(w[1] + w[6] - 2.0 * wC)),
                                max(abs(w[2] + w[5] - 2.0 * wC), abs(w[3] + w[4] - 2.0 * wC)));
                float depthRel = lap / max(wMax, 1e-5);
                half depthEdge = smoothstep(_DepthThreshold, _DepthThreshold * 1.6, depthRel);
                half creaseEdge = smoothstep(_NormalThreshold, _NormalThreshold + 0.1, normalDiff) * _CreaseStrength;
                half idEdge = step(0.004, idDiff) * _IdStrength;

                float eyeDepth = wC > 0.0 ? 1.0 / wC : 1e4;
                half fade = 1.0 - saturate((eyeDepth - _FadeStart) / max(_FadeEnd - _FadeStart, 1e-3));

                // Brush feel: ink density drifts slowly across the frame.
                float aspect = _ScreenParams.x / _ScreenParams.y;
                half density = SAMPLE_TEXTURE2D_LOD(_GrainTex, sampler_GrainTex, uv * float2(aspect, 1.0) * 0.35, 0).g;
                half ink = saturate(max(max(depthEdge, creaseEdge), idEdge)) * fade * lerp(1.0h - _InkVariation, 1.0h, density);

                color.rgb = lerp(color.rgb, _InkColor.rgb, ink * _InkColor.a);

                // Things the player can use: a thin bright line along the silhouette with a faint edge.
                bool markedC = Marked(rawC);
                int r2 = max(1, (int)round(_HighlightWidth * _ScreenParams.y / 1080.0));
                half glow = 0.0h;
                [unroll] for (int j = 0; j < 8; j++)
                {
                    if (Marked(InkIdAt(pix + Ring[j] * r2, size)) != markedC)
                        glow = 1.0h;
                    else if (Marked(InkIdAt(pix + Ring[j] * (r2 + 1), size)) != markedC)
                        glow = max(glow, 0.3h);
                }
                color.rgb = lerp(color.rgb, _HighlightColor.rgb, glow * _HighlightColor.a * lerp(0.45h, 1.0h, saturate(_DCM_HighlightPulse)));

                float2 grainUV = uv * _ScreenParams.xy * _GrainTex_TexelSize.xy;
                half grain = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, grainUV).r;
                color.rgb *= 1.0 + (grain - 0.5) * 2.0 * _GrainStrength;
                color.rgb *= _PaperTint.rgb;
                return color;
            }
            ENDHLSL
        }
    }
}
