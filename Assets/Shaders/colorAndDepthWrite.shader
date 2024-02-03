Shader "Unlit/colorAndDepthWrite"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Depth ("Depth", 2D) = "white" {}
        _Scale ("Scale", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            ZWrite On
            Cull Off
            Lighting Off


            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

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

            sampler2D _MainTex;
            sampler2D _Depth;
            float4 _MainTex_ST;
            float _Scale;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.y *= _Scale;
                return o;
            }

            struct ForwardFragmentOutput
            {
                float4 Color : SV_Target;
                float  Depth : SV_Depth;
            };

            float LinearDepthToRawDepth(float linearDepth)
            {
                return (1.0f - (linearDepth * _ZBufferParams.y)) / (linearDepth * _ZBufferParams.x);
            }

            ForwardFragmentOutput frag(v2f i)
            {
                ForwardFragmentOutput output;
                float far = 10;
                float near = 0;

                output.Color = tex2D(_MainTex, i.uv);
                //output.Depth = tex2D(_Depth, i.uv);
                output.Depth = LinearDepthToRawDepth(tex2D(_Depth, i.uv)).r;
                //output.Color = output.Depth;
                //output.Color = tex2D(_Depth, i.uv).rrrr;

                return output;
            }
            ENDCG
        }
    }
}
