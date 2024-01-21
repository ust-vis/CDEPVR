Shader "Custom/PointShader" {
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Depth ("Depth", 2D) = "white" {}
    }
    SubShader {
        Pass {
            Name "POINT"
            Tags { "LightMode" = "ForwardBase" }

            Cull Off
            Lighting Off
            ZWrite On
            Fog { Mode Off }

            CGPROGRAM
            #define M_PI 3.1415926535897932384626433832795
            #define EPSILON 0.000001

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint vertexId : SV_VERTEXID;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                uint vertexId : ID;
            };

            sampler2D _MainTex;
            sampler2D _Depth;
            float4 _MainTex_ST;

            float4 SphericalToCartesian(float azimuth, float elevation, float r)
            {
                return float4(
                r * sin(elevation) * cos(azimuth),
                r * sin(elevation) * sin(azimuth),
                r * cos(elevation),
                0
                );
            }

            v2f vert(appdata v) {
                v2f o;
                o.vertexId = v.vertexId;
                float depth = tex2Dlod(_Depth, float4(v.uv.xy,0,0)).x;
                o.vertex = SphericalToCartesian(v.uv.x * 2 * M_PI, (1 - v.uv.y) * M_PI, depth);
                o.vertex = UnityObjectToClipPos(o.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}