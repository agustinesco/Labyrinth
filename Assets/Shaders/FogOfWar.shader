Shader "Labyrinth/FogOfWar"
{
    Properties
    {
        _VisibilityTex ("Visibility Map", 2D) = "black" {}
        _ExplorationTex ("Exploration Map", 2D) = "black" {}
        _MazeSize ("Maze Size", Vector) = (25, 25, 0, 0)
        _TexelSize ("Texel Size", Vector) = (0.006, 0.006, 0, 0)
        _UnexploredOpacity ("Undiscovered Opacity", Range(0, 1)) = 1.0
        _ExploredOpacity ("Discovered Opacity", Range(0, 1)) = 0.7
        _VisibleOpacity ("Visible Opacity", Range(0, 1)) = 0.0
        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _VisibilityTex;
            sampler2D _ExplorationTex;
            float4 _MazeSize;
            float4 _TexelSize;
            float _UnexploredOpacity;
            float _ExploredOpacity;
            float _VisibleOpacity;
            float4 _FogColor;
            float _EdgeSoftness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // Gaussian-weighted 13-tap blur sample
            // Samples center + 4 cardinal (1 texel away) + 4 cardinal (2 texels away) + 4 diagonal (1 texel away)
            float SampleBlurred(sampler2D tex, float2 uv, float2 ts)
            {
                float sum = tex2D(tex, uv).r * 4.0;

                // Cardinal neighbors (1 texel)
                sum += tex2D(tex, uv + float2( ts.x, 0)).r * 2.0;
                sum += tex2D(tex, uv + float2(-ts.x, 0)).r * 2.0;
                sum += tex2D(tex, uv + float2(0,  ts.y)).r * 2.0;
                sum += tex2D(tex, uv + float2(0, -ts.y)).r * 2.0;

                // Diagonal neighbors (1 texel)
                sum += tex2D(tex, uv + float2( ts.x,  ts.y)).r;
                sum += tex2D(tex, uv + float2(-ts.x,  ts.y)).r;
                sum += tex2D(tex, uv + float2( ts.x, -ts.y)).r;
                sum += tex2D(tex, uv + float2(-ts.x, -ts.y)).r;

                // Cardinal neighbors (2 texels) for wider spread
                sum += tex2D(tex, uv + float2( ts.x * 2.0, 0)).r;
                sum += tex2D(tex, uv + float2(-ts.x * 2.0, 0)).r;
                sum += tex2D(tex, uv + float2(0,  ts.y * 2.0)).r;
                sum += tex2D(tex, uv + float2(0, -ts.y * 2.0)).r;

                return sum / 20.0;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Convert world position to UV for texture sampling
                float2 uv = i.worldPos.xy / _MazeSize.xy;
                float2 ts = _TexelSize.xy;

                // Sample with GPU blur for smooth edges
                float visible = SampleBlurred(_VisibilityTex, uv, ts);
                float explored = SampleBlurred(_ExplorationTex, uv, ts);

                // Soft transitions for smoother edges
                float isVisible = smoothstep(0.0, _EdgeSoftness + 0.1, visible);
                float isExplored = smoothstep(0.0, _EdgeSoftness + 0.1, explored);

                // Create colors with configurable opacities
                float4 unexploredCol = float4(_FogColor.rgb, _UnexploredOpacity);
                float4 exploredCol = float4(_FogColor.rgb, _ExploredOpacity);
                float4 visibleCol = float4(_FogColor.rgb, _VisibleOpacity);

                // Start with unexplored
                float4 result = unexploredCol;

                // If explored but not visible, blend to explored color
                result = lerp(result, exploredCol, isExplored);

                // If currently visible, blend to visible color
                result = lerp(result, visibleCol, isVisible);

                return result;
            }
            ENDCG
        }
    }
}
