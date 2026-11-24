
Shader "Custom/AirCurrentScroll"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ScrollOffset ("Scroll Offset", Vector) = (0,0,0,0)
        _UVRect ("UV Rect", Vector) = (0,0,1,1)
        _WindAlpha ("Wind Alpha", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
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

            sampler2D _MainTex;

            float4 _Color;
            float4 _ScrollOffset;
            float4 _UVRect;

            float _WindAlpha;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uvLocal =
                    (i.uv - _UVRect.xy) / _UVRect.zw;

                uvLocal = frac(
                    uvLocal - _ScrollOffset.xy
                );

                float2 uvFinal =
                    _UVRect.xy + uvLocal * _UVRect.zw;

                fixed4 col =
                    tex2D(_MainTex, uvFinal) * i.color;

                // Transparencia directa del viento.
                col.a *= saturate(_WindAlpha);

                col.rgb *= col.a;

                return col;
            }

            ENDCG
        }
    }
}
