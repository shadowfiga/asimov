sampler SourceSampler : register(s0);
float2 Dimensions;
float Progress;
float3 PetalColor;
float3 AlternateColor;
float3 CenterColor;
float Padding;

float4 Flowers(float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 source = tex2D(SourceSampler, uv) * color;
    float fade = 1.0 - smoothstep(0.65, 1.0, Progress);
    float grow = smoothstep(0.0, 0.35, Progress);
    float2 pixel = uv * Dimensions;
    for (int i = 0; i < 6; i++)
    {
        float side = fmod((float)i, 2.0);
        float row = floor((float)i / 2.0);
        float2 center = float2(lerp(Padding * 0.5, Dimensions.x - Padding * 0.5, side),
            Padding + (Dimensions.y - 2.0 * Padding) * (0.12 + row * 0.38));
        center.x += (side * 2.0 - 1.0) * Progress * 14.0;
        center.y -= Progress * (8.0 + row * 3.0);
        float2 delta = pixel - center;
        float angle = atan2(delta.y, delta.x) + i * 0.73 + Progress * 0.5;
        float radius = (12.0 + row * 3.0) * grow;
        float petalRadius = radius * (0.73 + 0.27 * cos(angle * 5.0));
        float mask = (1.0 - smoothstep(petalRadius - 0.8, petalRadius + 0.8, length(delta))) * fade * grow;
        float core = 1.0 - smoothstep(radius * 0.20, radius * 0.27 + 0.5, length(delta));
        float3 petals = lerp(PetalColor, AlternateColor, side);
        float3 flower = lerp(petals, CenterColor, core);
        source = float4(flower * mask + source.rgb * (1.0 - mask), mask + source.a * (1.0 - mask));
    }
    return source;
}
technique Material
{
    pass P0 { PixelShader = compile ps_3_0 Flowers(); }
}
