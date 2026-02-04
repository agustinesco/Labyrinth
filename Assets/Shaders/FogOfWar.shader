Shader "Labyrinth/FogOfWar"
{
    Properties
    {
        _VisibilityTex ("Visibility Map", 2D) = "black" {}
        _ExplorationTex ("Exploration Map", 2D) = "black" {}
        _MazeSize ("Maze Size", Vector) = (25, 25, 0, 0)
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

            fixed4 frag (v2f i) : SV_Target
            {
                // Convert world position to UV for texture sampling
                float2 uv = i.worldPos.xy / _MazeSize.xy;

                // Sample textures
                float visible = tex2D(_VisibilityTex, uv).r;
                float explored = tex2D(_ExplorationTex, uv).r;

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
