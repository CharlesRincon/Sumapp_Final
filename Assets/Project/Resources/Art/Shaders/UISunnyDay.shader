Shader "UI/SunnyDay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _SunColor ("Sun Color", Color) = (1, 0.9, 0.6, 1)
        _SunPos ("Sun Position (UV)", Vector) = (0.9, 0.9, 0, 0)
        _RayIntensity ("Ray Intensity", Range(0, 5)) = 1.0
_RayCount ("Ray Count", Float) = 8.0
_RaySpeed ("Ray Speed", Float) = 1.0
        
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
        ZTest [unit_ZTest]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                half4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            half4 _Color;
            half4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float4 _SunColor;
            float4 _SunPos;
            float _RayIntensity;
            float _RayCount;
            float _RaySpeed;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // Pseudo-random function
            float hash(float n) { return frac(sin(n) * 43758.5453123); }

            float noise(float x)
            {
                float i = floor(x);
                float f = frac(x);
                float u = f * f * (3.0 - 2.0 * f);
                return lerp(hash(i), hash(i + 1.0), u);
            }

            // Clipping function for UI
            float UnityGet2DClipping (in float2 position, in float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
                return inside.x * inside.y;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Standard UI Texture sample
                half4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                // Calculate sun rays
                float2 dir = i.texcoord - _SunPos.xy;
                float dist = length(dir);
                float angle = atan2(dir.y, dir.x);
                
                // Procedural rays using noise and sine
                float ray = sin(angle * _RayCount + _Time.y * _RaySpeed);
                ray += noise(angle * _RayCount * 2.0 - _Time.y * _RaySpeed * 0.5);
                ray = saturate(ray);
                
                // Fade rays with distance
                float rayFade = saturate(1.0 - dist * 1.5);
                float rays = ray * rayFade * _RayIntensity;
                
                // Add sun glow center
                float centerGlow = saturate(1.0 - dist * 4.0) * 2.0;
                
                // Total sun intensity
                float sunIntensity = rays + centerGlow;
                float3 sunEffect = _SunColor.rgb * sunIntensity;
                
                // Additive blend with the base color
                // We add the sun color and ensure the alpha is increased by the sun's intensity
                color.rgb += sunEffect;
                color.a = saturate(color.a + sunIntensity * _SunColor.a);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDHLSL
        }
    }
}
