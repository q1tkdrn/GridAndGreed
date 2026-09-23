Shader "GridAndGreed/UI/Main Menu Ambient Light"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 1)) = 0.55
        _Motion ("Motion", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        // Screen blending preserves bright silhouettes without clipping them to white.
        Blend One OneMinusSrcColor
        ColorMask RGB
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Intensity;
            float _Motion;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float shaft(float x, float center, float width)
            {
                float distance = (x - center) / width;
                return exp(-distance * distance * 2.0);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.uv;
                float depth = 1.0 - uv.y;
                // A slow, very small drift instead of a flashing or sweeping spotlight.
                float drift = sin(_Time.y * 0.16) * 0.014 * _Motion;
                float rayX = uv.x + depth * 0.32 + drift;
                float spread = 1.0 + depth * 1.2;
                float rays = shaft(rayX, 0.68, 0.045 * spread) * 0.10
                           + shaft(rayX, 0.84, 0.075 * spread) * 0.14
                           + shaft(rayX, 1.01, 0.026 * spread) * 0.06;
                rays *= smoothstep(0.02, 0.80, uv.y);

                float2 haloOffset = (uv - float2(0.81, 1.06)) / float2(0.40, 0.56);
                float halo = exp(-dot(haloOffset, haloOffset) * 2.0) * 0.27;
                float topLight = uv.y * uv.y * 0.025;
                float breathe = 1.0 + sin(_Time.y * 0.23) * 0.035 * _Motion;
                float light = saturate((halo + rays + topLight) * _Intensity * breathe);
                light *= input.color.a;
                return fixed4(light, light, light, 1.0);
            }
            ENDCG
        }
    }
}
