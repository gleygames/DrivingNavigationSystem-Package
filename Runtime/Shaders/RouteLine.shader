Shader "Gley/NavigationSystem/RouteLine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Line Color", Color) = (0.16,0.47,1,1)
        _OutlineColor ("Outline Color", Color) = (0.05,0.2,0.55,1)
        _FadedColor ("Faded Color", Color) = (0.5,0.5,0.5,0.5)
        _HalfWidth ("Half Width (canvas units)", Float) = 3
        _OutlineWidth ("Outline Width (canvas units)", Float) = 1
        _CanvasUnitsPerMeter ("Canvas Units Per Meter", Float) = 1
        _TrimDistance ("Trim Distance (meters)", Float) = 0
        _TrimMode ("Trim Mode (0 remove, 1 fade)", Float) = 0
        _DashLength ("Dash Length (canvas units)", Float) = 8
        _GapLength ("Gap Length (canvas units)", Float) = 6
        [HideInInspector] _CanvasOffsetMatrix ("Canvas Offset Matrix", Vector) = (1,0,0,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float4 texcoord0 : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                float4 texcoord2 : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float3 lineData : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _OutlineColor;
            fixed4 _FadedColor;
            float _HalfWidth;
            float _OutlineWidth;
            float _CanvasUnitsPerMeter;
            float _TrimDistance;
            float _TrimMode;
            float _DashLength;
            float _GapLength;
            float4 _CanvasOffsetMatrix;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float2 localOffset = v.texcoord1.xy;
                float2 canvasOffset;
                canvasOffset.x = _CanvasOffsetMatrix.x * localOffset.x + _CanvasOffsetMatrix.y * localOffset.y;
                canvasOffset.y = _CanvasOffsetMatrix.z * localOffset.x + _CanvasOffsetMatrix.w * localOffset.y;
                v.vertex.xy += canvasOffset * (_HalfWidth + _OutlineWidth);

                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                OUT.lineData = float3(v.texcoord0.x, v.texcoord0.y, v.texcoord2.x);

                if (_UIVertexColorAlwaysGammaSpace)
                {
                    if(!IsGammaSpace())
                    {
                        v.color.rgb = UIGammaToLinear(v.color.rgb);
                    }
                }

                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                float distanceAlong = IN.lineData.x;
                float side = IN.lineData.y;
                float dashed = IN.lineData.z;

                float totalHalfWidth = max(_HalfWidth + _OutlineWidth, 0.0001);
                float inner = _HalfWidth / totalHalfWidth;

                half4 color = _Color;
                if (abs(side) > inner)
                {
                    color = _OutlineColor;
                }

                if (distanceAlong < _TrimDistance)
                {
                    if (_TrimMode < 0.5)
                    {
                        discard;
                    }
                    color = _FadedColor;
                }

                if (dashed > 0.5)
                {
                    float along = distanceAlong * _CanvasUnitsPerMeter;
                    float period = _DashLength + _GapLength;
                    if (period > 0.0001)
                    {
                        if (fmod(along, period) > _DashLength)
                        {
                            discard;
                        }
                    }
                }

                color *= IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                color.rgb *= color.a;

                return color;
            }
        ENDCG
        }
    }
}
