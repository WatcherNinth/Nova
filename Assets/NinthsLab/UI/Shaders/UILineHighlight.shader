Shader "NinthsLab/UI/Line Highlight"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Highlight Color", Color) = (1.0, 0.85, 0.2, 1.0)
        _GlowWidth ("Glow Width", Float) = 6.0
        _GlowColor ("Glow Color", Color) = (1.0, 0.6, 0.0, 0.5)
        _AnimationTime ("Animation Time", Float) = 0.0
        _ScrollSpeed ("Scroll Speed", Float) = 0.8
        _ArrowPerUnit ("Arrows Per Unit", Float) = 0.05
        _ArrowBodyLen ("Arrow Body Length", Float) = 0.25
        [HideInInspector] _LineLength ("Line Length", Float) = 100.0

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }
            ColorMask [_ColorMask]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _Color;
            float _GlowWidth;
            float4 _GlowColor;
            float _AnimationTime;
            float _ScrollSpeed;
            float _ArrowPerUnit;
            float _ArrowBodyLen;
            float _LineLength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float glowIntensity = exp(-abs(i.uv.y - 0.5) * _GlowWidth);
                float3 glow = _GlowColor.rgb * glowIntensity;

                float coreIntensity = saturate(1.0 - abs(i.uv.y - 0.5) * 3.0);
                float3 core = _Color.rgb * coreIntensity;

                float capped = step(0.001, i.uv.x) * step(i.uv.x, 0.999);
                float phase = frac(i.uv.x * _ArrowPerUnit * _LineLength - _AnimationTime * _ScrollSpeed);

                float halfW = (1.0 - phase) * 0.35;
                float distY = abs(i.uv.y - 0.5);
                float arrow = capped * (1.0 - smoothstep(halfW - 0.02, halfW, distY));

                float3 rgb = glow + core + arrow * 1.2;
                float alpha = saturate(glowIntensity * _GlowColor.a + coreIntensity * i.color.a + arrow * 0.9);

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
