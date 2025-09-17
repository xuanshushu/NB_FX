#ifndef XUANXUAN_UTILITY
#define XUANXUAN_UTILITY



//引入自UnityCG.cginc
#define UNITY_PI            3.14159265359f
#define UNITY_TWO_PI        6.28318530718f
#define UNITY_FOUR_PI       12.56637061436f
#define UNITY_INV_PI        0.31830988618f
#define UNITY_INV_TWO_PI    0.15915494309f
#define UNITY_INV_FOUR_PI   0.07957747155f
#define UNITY_HALF_PI       1.57079632679f
#define UNITY_INV_HALF_PI   0.636619772367f

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

inline half NB_Remap(half x, half inMin, half inMax, half outMin, half outMax)
{
    // x=  clamp(x,inMin,inMax);
    return saturate((x - inMin) / (inMax - inMin)) * (outMax - outMin) + outMin;
}

inline half NB_Remap01(half x, half inMin, half inMax)
{
    // x=  clamp(x,inMin,inMax);
    return saturate((x - inMin) / (inMax - inMin));
}

inline half NB_RemapNoClamp(half x, half inMin, half inMax, half outMin, half outMax)
{
    // x=  clamp(x,inMin,inMax);
    return ((x - inMin) / (inMax - inMin)) * (outMax - outMin) + outMin;
}

//原生SmoothStep开销很大：https://zhuanlan.zhihu.com/p/34629262
//在很多时候可以用简单线性映射代替。
half SimpleSmoothstep(half min,half max,half interp)
{
    return saturate((interp - min)/(max-min));
}

half4 TryLinearize(half4 color)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
    return pow(color, 2.2f);
    #else
    return color;
    #endif
}

half4 TryLinearizeWithoutAlpha(half4 color)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
    return half4(pow(color.rgb, 2.2f), color.a);
    #else
    return color;
    #endif
}

half3 TryLinearize(half3 color)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
    return pow(color, 2.2f);
    #else
    return color;
    #endif
}

half2 TryLinearize(half2 color)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
    return pow(color, 2.2f);
    #else
    return color;
    #endif
}

half TryLinearize(half color)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
    return pow(color, 2.2f);
    #else
    return color;
    #endif
}

half4 tex2D_TryLinearizeWithoutAlpha(sampler2D tex, float2 uv)
{
    #if defined(UNITY_COLORSPACE_GAMMA)
    return TryLinearizeWithoutAlpha(tex2D(tex, uv));    
    #else
    return tex2D(tex, uv);
    #endif
}

inline float LinearToGammaSpaceExact (float value)
{
    if (value <= 0.0F)
        return 0.0F;
    else if (value <= 0.0031308F)
        return 12.92F * value;
    else if (value < 1.0F)
        return 1.055F * pow(value, 0.4166667F) - 0.055F;
    else
        return pow(value, 0.45454545F);
}

// real FastSRGBToLinear(real c)
// {
//     return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
// }

float2 Rotate_Radians_float(float2 UV, float2 Center, float Rotation)
{
    if(Rotation == 0)
    {
        return UV;
    }
        
    // Rotation = Rotation / 180 * 3.14;    //从角度转为弧度。
    Rotation *= 0.01745329222;    //从角度转为弧度。
    UV -= Center;
    float s = sin(Rotation);
    float c = cos(Rotation);
    float2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix * 2 - 1;
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;
    return UV;
}

inline half luminance(half3 color)
{
    return dot(color, float3(0.2126f, 0.7152f, 0.0722f));
}

inline half DepthFactor(float Z, float near, float far)
{
    Z = saturate((Z-near)/(far - near));
    return Z;
}

//抽自Unity.cginc
inline half3 LinearToGammaSpace (half3 linRGB)
{
    linRGB = max(linRGB, half3(0.h, 0.h, 0.h));
    // An almost-perfect approximation from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
    return max(1.055h * pow(linRGB, 0.416666667h) - 0.055h, 0.h);

    // Exact version, useful for debugging.
    //return half3(LinearToGammaSpaceExact(linRGB.r), LinearToGammaSpaceExact(linRGB.g), LinearToGammaSpaceExact(linRGB.b));
}

//ASE
float2 voronoihash1( float2 p )
{
			
    p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
    return frac( sin( p ) *43758.5453);
}

//ASE
float voronoi1( float2 v, float time, inout float2 id, inout float2 mr, float smoothness )
{
    float2 n = floor( v );
    float2 f = frac( v );
    float F1 = 8.0;
    float F2 = 8.0; float2 mg = 0;
    for ( int j = -1; j <= 1; j++ )
    {
        for ( int i = -1; i <= 1; i++ )
        {
            float2 g = float2( i, j );
            float2 o = voronoihash1( n + g );
            o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
            float d = 0.5 * dot( r, r );
            if( d<F1 ) {
                F2 = F1;
                F1 = d; mg = g; mr = r; id = o;
            } else if( d<F2 ) {
                F2 = d;
            }
        }
    }
    return F2 - F1;
}

void voroniForgraphfunc_half(half2 uv,half angle,half scale,out float outVoroni1 )
{
    uv = uv*scale;
    float2 id1 = 0;
    float2 uv1 = 0;
    outVoroni1 = voronoi1( uv, angle, id1, uv1, 0 );
    
}

inline float2 unity_voronoi_noise_randomVector (float2 UV, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    UV = frac(sin(mul(UV, m)) * 46839.32);
    return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
}

void Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{
    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);

    for(int y=-1; y<=1; y++)
    {
        for(int x=-1; x<=1; x++)
        {
            float2 lattice = float2(x,y);
            float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset);
            float d = distance(lattice + offset, f);
            if(d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                Out = res.x;
                Cells = res.y;
            }
        }
    }
}

void Unity_Blend_Overlay_float4(float4 Base, float4 Blend, float Opacity, out float4 Out)
{
    float4 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float4 result2 = 2.0 * Base * Blend;
    float4 zeroOrOne = step(Base, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Unity_Blend_HardLight_half(half Base, half Blend, half Opacity, out half Out)
{
    half result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    half result2 = 2.0 * Base * Blend;
    half zeroOrOne = step(Blend, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    Out = lerp(Base, Out, Opacity);
}

void Blend_HardLight_half(half Base, half Blend, out half Out)
{
    half result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    half result2 = 2.0 * Base * Blend;
    half zeroOrOne = step(Blend, 0.5);
    Out = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
}

float2 randomGradient(float2 p) {
    p = p + 0.02;
    float x = dot(p, float2(123.4, 234.5));
    float y = dot(p, float2(234.5, 345.6));
    float2 gradient = float2(x, y);
    gradient = sin(gradient);
    gradient = gradient * 43758.5453;

    // part 4.5 - update noise function with time
    // gradient = sin(gradient + u_time);
    return gradient;

    // gradient = sin(gradient);
    // return gradient;
}

#include "./jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"
half SimplexNoise(float2 uv,float time)
{
    return SimplexNoise(float3(uv,time))*0.5+0.5;
}

#include "./jp.keijiro.noiseshader/Shader/ClassicNoise3D.hlsl"
half PerlinNoise(float2 uv,float time)
{
    return ClassicNoise(float3(uv,time))*0.5+0.5;
}

float2 PolarCoordinates(float2 UV, float2 _PCCenter)
{
    // float2 uvsource = float2(0, 0);

    float2 delta = UV - _PCCenter.xy;//校准UV到中心
    float radius = length(delta) * 2;
    float angle = atan2(delta.x, delta.y) * UNITY_INV_TWO_PI ;
    return  float2(angle, radius);//翻转可以调整横向和纵向。
}

float2 PolarCoordinatesStrengthAndST(float2 UVBeforPollarCoord,float2 UVAfterPolarCoord,float polarStrenth ,float4 tex_ST)
{
    UVAfterPolarCoord = UVAfterPolarCoord*tex_ST.xy + tex_ST.zw;//利用相应功能贴图的坐标对ST进行修改。
    UVAfterPolarCoord = lerp(UVBeforPollarCoord, UVAfterPolarCoord, polarStrenth);
    return UVAfterPolarCoord;
}


//极坐标//transformation为极坐标强度
float2 PolarCoordinates(float2 UV, float3 _PCCenter, float4 tex_ST)
{
    // float2 uvsource = float2(0, 0);
    //
    // float2 delta = UV - _PCCenter.xy;//校准UV到中心
    // float radius = length(delta) * 2;
    // float angle = atan2(delta.x, delta.y) * UNITY_INV_TWO_PI ;
    // uvsource = float2(angle, radius);//翻转可以调整横向和纵向。
    // uvsource = uvsource*tex_ST.xy + tex_ST.zw;//利用相应功能贴图的坐标对ST进行修改。
    // uvsource = lerp(UV, uvsource, _PCCenter.z);
    // return uvsource;

    //拆分成两步，第一步求极坐标太耗（atan），一般源UV都是一样的，只是ST不同。
    float2 uvsource = PolarCoordinates(UV,_PCCenter.xy);
    uvsource = PolarCoordinatesStrengthAndST(UV,uvsource,_PCCenter.z,tex_ST);
    return uvsource;
    
}

float2 CylinderCoordinate(float3 positionOS)
{
    float angle = atan2(positionOS.x,positionOS.z)* UNITY_INV_TWO_PI ;
    return float2(angle,positionOS.y);
}


float2 UVOffsetAnimaiton(float2 UV,half2 OffsetSpeed,float time)
{
    float2 newUV =  float2(OffsetSpeed.x*time+UV.x,OffsetSpeed.y*time+UV.y);
    return newUV;
}

half3 rgb2hsv(half3 c)
{
    half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
    half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

half3 hsv2rgb(half3 c)
{
    half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    half3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

half3 ColorSaturate(half3 color, half colorSaturation)
{
    half3 lum = luminance(color);
    return max(0, lerp(lum, color, colorSaturation));
}

half2 Rotate(half2 v, half cos0, half sin0)
{
    return half2(v.x * cos0 - v.y * sin0,
                  v.x * sin0 + v.y * cos0);
}

half SmoothStep01(half interval)//让01线性过渡变成SmoothStep过渡，但是控制计算量。
{
    interval = saturate(interval);
    return interval * interval * ( 3 - 2 * interval );
}
#endif
