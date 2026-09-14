sampler SourceSampler : register(s0);
float Time;
float2 Dimensions;
float Scanlines;
float Noise;
float Vignette;
float Bloom;

float4 Crt(float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float2 centered = uv * 2.0 - 1.0;
    float radiusSquared = dot(centered, centered);
    float4 source = tex2D(SourceSampler, uv) * color;
    float2 texel = 1.0 / Dimensions;
    float3 nearby = (
        tex2D(SourceSampler, saturate(uv + float2(texel.x, 0))).rgb +
        tex2D(SourceSampler, saturate(uv - float2(texel.x, 0))).rgb +
        tex2D(SourceSampler, saturate(uv + float2(0, texel.y))).rgb +
        tex2D(SourceSampler, saturate(uv - float2(0, texel.y))).rgb
    ) * 0.25;
    source.rgb += max(nearby - 0.55, 0.0) * Bloom * source.a;
    float scanline = 0.5 + 0.5 * sin(uv.y * Dimensions.y * 3.14159265);
    float noise = frac(sin(dot(floor(uv * Dimensions), float2(12.9898, 78.233)) + floor(Time * 12.0)) * 43758.5453) - 0.5;
    float vignette = saturate(radiusSquared * Vignette);
    source.rgb *= 1.0 - scanline * Scanlines - vignette;
    source.rgb += noise * Noise * source.a;
    return source;
}
technique Material
{
    pass P0 { PixelShader = compile ps_3_0 Crt(); }
}
