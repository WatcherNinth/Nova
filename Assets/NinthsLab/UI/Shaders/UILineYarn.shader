Shader "NinthsLab/UI/Line Yarn"
{
    Properties
    {
        [HideInInspector] _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (0.73, 0.08, 0.08, 1.0)
        _CordCount ("Cord Count", Float) = 3.0
        _TwistRate ("Twist Rate", Float) = 0.12
        _FuzzWidth ("Fuzz Width", Range(0.02, 0.3)) = 0.08
        _SpecularBright ("Specular", Range(0, 1)) = 0.15
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
            #include "Assets/Nova/CGInc/Rand.cginc"

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
            float _CordCount;
            float _TwistRate;
            float _FuzzWidth;
            float _SpecularBright;
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
                float edgeAlpha = smoothstep(0, _FuzzWidth, i.uv.y) * smoothstep(0, _FuzzWidth, 1.0 - i.uv.y);

                float phase = frac(i.uv.x * _TwistRate * _LineLength + i.uv.y * 2.0);
                float strand = sin(phase * UNITY_PI * _CordCount) * 0.5 + 0.5;

                float centerGlow = 1.0 - abs(i.uv.y - 0.5) * 2.5;
                centerGlow = smoothstep(0, 1, centerGlow);

                float noiseVal = noise(i.uv * 50.0) * 0.1;

                float3 rgb = _Color.rgb * (0.6 + 0.3 * strand + 0.15 * centerGlow * _SpecularBright + noiseVal);
                float alpha = _Color.a * i.color.a * edgeAlpha;

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
