Shader "UI/Rain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _RainColor ("Rain Color", Color) = (0.7, 0.8, 1, 0.5)
        _RainSpeed ("Rain Speed", Float) = 2.0
        _RainIntensity ("Rain Intensity", Range(0, 10)) = 1.0
        _RainDensity ("Rain Density", Float) = 20.0
        _RainAngle ("Rain Angle (Radians)", Float) = 0.2
        
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

            float4 _RainColor;
            float _RainSpeed;
            float _RainIntensity;
            float _RainDensity;
            float _RainAngle;

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

            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float UnityGet2DClipping (in float2 position, in float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
                return inside.x * inside.y;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                // Rotate and scale UV for rain lines
                float s = sin(_RainAngle);
                float c = cos(_RainAngle);
                float2 uv = i.texcoord;
                uv = float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c);
                uv.y -= _Time.y * _RainSpeed;
                uv.x *= _RainDensity;

                // Create rain drops/lines
                float2 id = floor(uv);
                float2 gv = frac(uv);
                
                float h = hash(id);
                // Offset each column
                float yOffset = hash(float2(id.x, 31.4)) * 10.0;
                float xOffset = (h - 0.5) * 0.5;
                
                // Rain line shape
                float rainLine = smoothstep(0.05, 0.0, abs(gv.x - 0.5 + xOffset));
                // Vary line length and appearance
                rainLine *= smoothstep(0.1, 0.5, h); 
                
                // Final rain intensity
                float finalRain = rainLine * _RainIntensity * _RainColor.a;

                // Add to original color
                color.rgb += _RainColor.rgb * finalRain;
                color.a = saturate(color.a + finalRain);

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
