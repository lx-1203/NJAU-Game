Shader "UI/WaterRipple"
{
    Properties
    {
        // 背景纹理（传入视频 RenderTexture）
        _MainTex      ("Background Texture", 2D) = "black" {}

        _RippleCenter ("Ripple Center",      Vector)      = (0.5, 0.5, 0, 0)
        _Progress     ("Progress",           Range(0,1))  = 0
        _MaxRadius    ("Max Radius",         Float)       = 0.6
        _Strength     ("Distortion",         Float)       = 0.035
        _RingWidth    ("Ring Width",         Float)       = 0.12
        _Alpha        ("Alpha",              Range(0,1))  = 1
        // 高亮圆环颜色
        _RingColor    ("Ring Color",         Color)       = (0.8, 0.93, 1, 0.3)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            float4    _RippleCenter;
            float     _Progress;
            float     _MaxRadius;
            float     _Strength;
            float     _RingWidth;
            float     _Alpha;
            float4    _RingColor;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f    { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 uv     = i.uv;
                float  aspect = _MainTex_TexelSize.z / _MainTex_TexelSize.w;

                float2 center = _RippleCenter.xy;
                float2 delta  = uv - center;
                delta.x      *= aspect;
                float  dist   = length(delta);

                float waveRadius = _Progress * _MaxRadius;
                float ringDist   = abs(dist - waveRadius);

                // 波环 mask — 越宽越明显
                float ringMask   = smoothstep(_RingWidth, 0.0, ringDist);

                // 距离衰减（边缘更弱）
                float falloff    = 1.0 - smoothstep(0.0, _MaxRadius * 1.1, dist);

                // 时间淡出（后期减弱）
                float timeFade   = pow(1.0 - _Progress, 0.7);

                float weight     = ringMask * falloff * timeFade * _Alpha;

                // 径向扭曲方向
                float2 dir = (dist > 0.001)
                    ? normalize(float2(delta.x / aspect, delta.y))
                    : float2(0, 0);

                // 扭曲采样背景
                float2 distUV  = uv + dir * weight * _Strength;
                half4  bgCol   = tex2D(_MainTex, distUV);

                // 高亮圆环叠加（在波前位置叠一道亮边）
                float  ringHighlight = ringMask * falloff * timeFade;
                half4  ringCol       = _RingColor * ringHighlight;

                // 混合：背景扭曲 + 高亮圆环
                half4 result;
                result.rgb = lerp(bgCol.rgb, ringCol.rgb, ringCol.a);
                // alpha = 有扭曲的地方才可见
                result.a   = saturate(weight * 1.5 + ringHighlight * _RingColor.a) * _Alpha;

                return result;
            }
            ENDCG
        }
    }
    FallBack Off
}
