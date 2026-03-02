// SpriteBatch가 물려주는 기본 텍스처
texture uImage0;

// 우리가 외부에서 넣을 그라디언트 텍스처 파라미터
texture GradTex;

// 샘플러
sampler2D uImage0Sampler = sampler_state
{
    Texture = <uImage0>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D GradSampler = sampler_state
{
    Texture = <GradTex>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU = Wrap;  // gradOffset 회전할 거면 Wrap이 편함
    AddressV = Clamp;
};

float rotation;
float gradOffset = 0.0;     // 0~1
float intensity = 1.0;
float fadeStrength = 1.0;

static const float TWO_PI = 6.28318530718;

float4 RotateSigilPixel(float2 uv : TEXCOORD0) : COLOR0
{
    // base(시길 마스크) 회전
    float2x2 rotM = float2x2(cos(rotation), -sin(rotation),
                             sin(rotation),  cos(rotation));
    float2 uvR = mul(uv + float2(-0.5, -0.5), rotM) + float2(0.5, 0.5);

    float4 base = tex2D(uImage0Sampler, uvR);

    // X방향 페이드
    float fadeA = 1.0 - saturate(uv.x * fadeStrength);

    // 각도 기반 t(0~1)
    float2 p = float2(0.5, 0.5) - uv;
    float t = (atan2(p.y, p.x) / TWO_PI) + 0.5;
    t = frac(t + gradOffset);

    // 그라디언트 샘플 (가로 그라디언트 가정)
    float4 grad = tex2D(GradSampler, float2(t, 0.5));

    // 디버그용(선택): grad가 안 들어오면 대개 (1,1,1,1) 또는 (0,0,0,0)로 보임
    // return float4(grad.rgb, 1);

    float outA = base.a * fadeA;
    float3 outRGB = grad.rgb * intensity;

    return float4(outRGB, outA);
}

technique Technique1
{
    pass Aura
    {
        PixelShader = compile ps_3_0 RotateSigilPixel();
    }
}