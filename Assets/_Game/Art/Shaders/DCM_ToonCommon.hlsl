#ifndef DCM_TOON_COMMON_INCLUDED
#define DCM_TOON_COMMON_INCLUDED

TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);

// Globals set by DCMLook in the scene. All zero means: no fill light, default bands.
half _DCM_HatchGlobal;
float4 _DCM_FillDir;      // xyz: direction toward the fill (window bounce) light
half4 _DCM_FillColor;     // rgb: colour * intensity of the fill
half4 _DCM_ShadeParams;   // x: fill threshold, y: fill softness, z: AO threshold, w: AO softness

half ToonStep(half x, half threshold, half softness)
{
    return smoothstep(threshold - softness, threshold + softness, x);
}

float3 TriplanarWeights(float3 n)
{
    float3 w = pow(abs(n), 4.0);
    return w / max(w.x + w.y + w.z, 1e-4);
}

half SampleTriplanar(float3 p, float3 n)
{
    float3 w = TriplanarWeights(n);
    half x = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, p.zy).r;
    half y = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, p.xz).r;
    half z = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, p.xy).r;
    return x * w.x + y * w.y + z * w.z;
}

// Grayscale ink detail: 1 = paper white (no change), 0 = full ink.
half SampleInkDetail(float2 uv, float3 positionOS, half3 normalOS, float3 positionWS, half3 normalWS)
{
    half detail;
    [branch] if (_DetailMode < 0.5)
    {
        detail = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, uv * _DetailMap_ST.xy + _DetailMap_ST.zw).r;
    }
    else
    {
        bool objectSpace = _DetailMode < 1.5;
        float3 p = objectSpace ? positionOS : positionWS;
        float3 n = objectSpace ? (float3)normalOS : (float3)normalWS;
        detail = SampleTriplanar(p * _DetailScale, n);
    }
    return detail;
}

// Anti-aliased stripe: 1 on the line, 0 between lines. Fades out when the lines
// get denser than a few pixels so distant surfaces do not moire.
half StripeLine(float u, half width)
{
    float d = abs(frac(u) - 0.5) * 2.0;
    float aa = max(fwidth(u) * 1.5, 1e-4);
    half lineValue = smoothstep(1.0 - width - aa, 1.0 - width + aa, d);
    half fade = saturate(1.0 - (fwidth(u) - 0.25) * 4.0);
    return lineValue * fade;
}

float Hash31(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float ValueNoise3(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(lerp(Hash31(i + float3(0, 0, 0)), Hash31(i + float3(1, 0, 0)), f.x),
                     lerp(Hash31(i + float3(0, 1, 0)), Hash31(i + float3(1, 1, 0)), f.x), f.y),
                lerp(lerp(Hash31(i + float3(0, 0, 1)), Hash31(i + float3(1, 0, 1)), f.x),
                     lerp(Hash31(i + float3(0, 1, 1)), Hash31(i + float3(1, 1, 1)), f.x), f.y), f.z);
}

half HatchLines(float3 positionWS, half3 normalWS, float density, half width)
{
    float3 w = TriplanarWeights(normalWS);
    float3 p = positionWS * density;
    half lx = StripeLine(p.y * 0.7 + p.z, width);
    half ly = StripeLine(p.x + p.z * 0.7, width);
    half lz = StripeLine(p.x * 0.7 + p.y, width);
    half hatch = lx * w.x + ly * w.y + lz * w.z;
    // Patchy coverage, like hatching laid down by hand rather than a wallpaper pattern.
    half patches = smoothstep(0.32h, 0.68h, ValueNoise3(positionWS * 1.6));
    half globalScale = _DCM_HatchGlobal > 0.0 ? _DCM_HatchGlobal : 1.0h;
    return hatch * patches * globalScale;
}

#endif
