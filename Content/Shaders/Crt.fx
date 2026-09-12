sampler SourceSampler : register(s0);
float Time;
float2 Dimensions;
float Strength;

float4 Crt(float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 source = tex2D(SourceSampler, uv) * color;
    float scanline = 0.5 + 0.5 * sin(uv.y * Dimensions.y * 3.14159265);
    float noise = frac(sin(dot(floor(uv * Dimensions), float2(12.9898, 78.233)) + floor(Time * 12.0)) * 43758.5453) - 0.5;
    float2 edge = uv * 2.0 - 1.0;
    float vignette = saturate(dot(edge, edge) * 0.12);
    source.rgb *= 1.0 - Strength * (scanline * 0.045 + vignette);
    source.rgb += noise * 0.008 * Strength * source.a;
    return source;
}
technique Material
{
    pass P0 { PixelShader = compile ps_3_0 Crt(); }
}
