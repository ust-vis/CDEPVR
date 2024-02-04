Shader "Unlit/CDEP" {
    Properties
    {
        _img_index ("image index", Float) = 0.0
        //_capture_position ("capture positon", Vector) = (0, 0, 0, 1)
        _camera_position ("camera positon", Vector) = (0, 0, 0, 1)
        _camera_ipd ("ipd", Float) = 0.0608
        _camera_focal_dist ("focal distance", Float) = 1.95
        _camera_eye ("camera eye", Float) = -1.0
        _xr_fovy ("fov y", Float) = 75
        _xr_aspect ("aspect", Float) = 2
        _xr_view_dir ("view direction", Vector) = (0, 0, 0, 1)
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
                float size: PSIZE;
                float depth: myDepth;
            };

            sampler2D _MainTex;
            sampler2D _Depth;
            float4 _MainTex_ST;
            float _img_index;
            //float4 _capture_position;
            float4 _camera_position;
            float _camera_ipd;
            float _camera_focal_dist;
            float _camera_eye;
            float _xr_fovy;
            float _xr_aspect;
            float4 _xr_view_dir;

            float4 SphericalToCartesian(float azimuth, float elevation, float r)
            {
                return float4(
                r * sin(elevation) * cos(azimuth),
                r * sin(elevation) * sin(azimuth),
                r * cos(elevation),
                0
                );
            }
            
            float mod(float x, float y)
            {
                return x - y * floor(x/y);
            }

            v2f vert(appdata v) {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                o.vertexId = v.vertexId;
                float vertex_depth = tex2Dlod(_Depth, float4(v.uv.xy,0,0)).x;

                // Calculate projected point position (relative to projection sphere center)
                float3 pt = SphericalToCartesian(v.uv.x * 2 * M_PI, (1 - v.uv.y) * M_PI, vertex_depth).xyz;
                //pt = pt + _capture_position.xyz;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.x = o.uv.x * -1;

                // Backproject to new ODS panorama
                //float3 camera_spherical = float3(_WorldSpaceCameraPos.z, _WorldSpaceCameraPos.x, _WorldSpaceCameraPos.y);
                //float3 camera_spherical = float3(0,0,0);
                float3 camera_spherical = _camera_position.zxy;
                float3 vertex_direction = pt - camera_spherical;
                float magnitude = length(vertex_direction);

                float center_azimuth = (abs(vertex_direction.x) < EPSILON && abs(vertex_direction.y) < EPSILON) ?
                (1.0 - 0.5 * sign(vertex_direction.z)) * M_PI :
                atan2(vertex_direction.y , vertex_direction.x);

                float center_inclination = acos(vertex_direction.z / magnitude);

                float camera_radius = 0.5 * _camera_ipd * cos(center_inclination - (M_PI / 2.0));
                float camera_azimuth = center_azimuth + _camera_eye * acos(camera_radius / magnitude);

                float3 camera_pt = float3(camera_radius * cos(camera_azimuth),
                camera_radius * sin(camera_azimuth),
                0.0);

                float3 camera_to_pt = vertex_direction - camera_pt;
                float camera_distance = length(camera_to_pt);
                float3 camera_ray = camera_to_pt / camera_distance;
                float img_sphere_dist = sqrt(_camera_focal_dist * _camera_focal_dist - camera_radius * camera_radius);
                float3 img_sphere_pt = camera_pt + img_sphere_dist * camera_ray;

                float projected_azimuth = (abs(img_sphere_pt.x) < EPSILON && abs(img_sphere_pt.y) < EPSILON) ?
                (1.0 - 0.5 * sign(img_sphere_pt.z)) * M_PI :
                mod(atan2(img_sphere_pt.y , img_sphere_pt.x), 2.0 * M_PI);

                float projected_inclination = acos(img_sphere_pt.z / _camera_focal_dist);

                
                // Set point size (1.25 seems to be a good balance between filling small holes and blurring image)
                o.size = 1.0;
                float size_ratio = vertex_depth / camera_distance;
                float size_scale = 1.1 + (0.4 - (0.16 * min(camera_distance, 2.5))); // scale ranges from 1.1 to 1.5
                o.size = size_scale * size_ratio;

                
                // XR viewport only
                float diag_aspect = sqrt(_xr_aspect * _xr_aspect + 1.0);
                float vertical_fov = 0.5 * _xr_fovy + 0.005;
                //float horizontal_fov = atan(tan(vertical_fov) * xr_aspect);
                float diagonal_fov = atan(tan(vertical_fov) * diag_aspect);
                float3 point_dir = normalize(img_sphere_pt.yzx);
                // discard point (move outside view volume) if angle between point direction and view diretion > diagonal FOV
                projected_azimuth -= float(dot(point_dir, _xr_view_dir * 1.3) < cos(diagonal_fov)) * 10.0;

                
                // Set point position
                float depth_hint = -0.015 * _img_index; // favor image with lower index when depth's match (index should be based on dist)
                //float depth_hint = -0.015;
                o.vertex = float4(projected_azimuth - M_PI, projected_inclination - M_PI/2, -camera_distance - depth_hint, 1.0);
                o.vertex = UnityObjectToClipPos(o.vertex);                    
                o.depth = o.vertex.z;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return float4(tex2D(_MainTex, i.uv).rgb, i.depth);
            }
            ENDCG
        }
    }
}
