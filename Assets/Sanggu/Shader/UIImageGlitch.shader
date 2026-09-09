// Unity 6 / Built-in Render Pipeline UI Image glitch shader.
// Create a Material from this shader and assign it to a UI Image's Material slot.
Shader "UI/Glitch Image"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [Toggle] _EnableGlitch ("Enable Glitch", Float) = 1
        _GlitchStrength ("Glitch Strength", Range(0, 1)) = 0.18
        _GlitchSpeed ("Glitch Speed", Range(0, 20)) = 8
        _BandCount ("Horizontal Bands", Range(2, 160)) = 42
        _MaxHorizontalShift ("Max Horizontal Shift", Range(0, 0.2)) = 0.035
        _RGBSplit ("RGB Split", Range(0, 0.05)) = 0.006
        _BlockNoise ("Block Noise", Range(0, 1)) = 0.2
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.12
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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Glitch"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _EnableGlitch, _GlitchStrength, _GlitchSpeed, _BandCount;
            float _MaxHorizontalShift, _RGBSplit, _BlockNoise, _ScanlineStrength;

            // Stable, inexpensive pseudo-random value for a band or block coordinate.
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 SampleSprite(float2 uv)
            {
                return tex2D(_MainTex, uv) + _TextureSampleAdd;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Keep the material usable as a normal UI Image when the toggle is off.
                if (_EnableGlitch < 0.5)
                {
                    fixed4 original = SampleSprite(i.uv) * i.color;
                    #ifdef UNITY_UI_CLIP_RECT
                    original.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                    #endif
                    #ifdef UNITY_UI_ALPHACLIP
                    clip(original.a - 0.001);
                    #endif
                    return original;
                }

                float timeStep = floor(_Time.y * _GlitchSpeed);
                float band = floor(i.uv.y * _BandCount);
                float bandRandom = hash21(float2(band, timeStep));

                // Only some bands jump, avoiding a permanently distorted image.
                float bandEnabled = step(1.0 - _GlitchStrength, bandRandom);
                float horizontalShift = (bandRandom * 2.0 - 1.0) * _MaxHorizontalShift * bandEnabled;

                // Occasional chunky digital displacement, independently of the scan bands.
                float2 blockCell = floor(i.uv * float2(18.0, 36.0) + timeStep * 0.13);
                float blockEnabled = step(1.0 - _BlockNoise * _GlitchStrength, hash21(blockCell));
                horizontalShift += (hash21(blockCell + 9.17) * 2.0 - 1.0) * _MaxHorizontalShift * blockEnabled;

                float2 shiftedUV = i.uv + float2(horizontalShift, 0.0);
                float split = _RGBSplit * (0.35 + bandEnabled * 0.65);
                fixed4 center = SampleSprite(shiftedUV);
                fixed4 red = SampleSprite(shiftedUV + float2(split, 0.0));
                fixed4 blue = SampleSprite(shiftedUV - float2(split, 0.0));
                fixed4 color = fixed4(red.r, center.g, blue.b, center.a) * i.color;

                float scanline = sin(i.uv.y * 900.0 + _Time.y * 8.0) * 0.5 + 0.5;
                color.rgb *= 1.0 - scanline * _ScanlineStrength;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
