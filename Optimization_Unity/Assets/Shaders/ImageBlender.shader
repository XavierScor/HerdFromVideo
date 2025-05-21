Shader "Hidden/ImageBlender"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Source ("Source Texture", 2D) = "white" {}
        _Ratio("Split Ratio", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _Source;
            float _Ratio;

            fixed4 frag (v2f i) : SV_Target
            {
                if (i.uv.x > _Ratio)
                {
                    return tex2D(_MainTex, i.uv);
                }
                else
                {
                    return tex2D(_Source, i.uv);
                }
            }
            ENDCG
        }
    }
    Fallback Off
}
