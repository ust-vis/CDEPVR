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
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members position)
#pragma exclude_renderers d3d11
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
                float4 position: TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _Depth;
            float4 _MainTex_ST;
            float _Scale;

            v2f vert (appdata v)
            {
                v2f o;
                o.position = v.vertex;
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
                return (1.0 - (linearDepth * _ZBufferParams.y)) / (linearDepth * _ZBufferParams.x);
            }

            ForwardFragmentOutput frag(v2f i)
            {
                ForwardFragmentOutput output;

                //convert distance to depth
                float objectDistance = tex2D(_Depth, i.uv).r;
                float4 position = i.position;
                float4 fragPos = objectDistance * position;
                float4 fragClipPosition = UnityObjectToClipPos(fragPos);
                float fragDepth = (fragClipPosition.z / fragClipPosition.w);
                
                //fragDepth = (fragDepth - _ProjectionParams.y) / (_ProjectionParams.z - _ProjectionParams.y);

                //output.Color = tex2D(_MainTex, i.uv);
                //output.Depth = fragDepth;
                output.Depth = fragDepth;

                output.Color = output.Depth.rrrr;
                if(output.Depth > 1){
                    output.Color = float4(1,0,1,1);
                }
                //output.Color = position;
                //output.Depth = LinearDepthToRawDepth(fragDepth);
                
                
                return output;
            }
            ENDCG
        }
    }
}
