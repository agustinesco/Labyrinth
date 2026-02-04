Shader "Labyrinth/FogOfWar"
{
    Properties
    {
        _VisibilityTex ("Visibility Map", 2D) = "black" {}
        _ExplorationTex ("Exploration Map", 2D) = "black" {}
        _MazeSize ("Maze Size", Vector) = (25, 25, 0, 0)
        _UnexploredColor ("Unexplored Color", Color) = (0, 0, 0, 1)
        _ExploredColor ("Explored Color", Color) = (0, 0, 0, 0.7)
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
            float4 _UnexploredColor;
            float4 _ExploredColor;
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

                // Fully transparent for visible areas
                float4 clearCol = float4(0, 0, 0, 0);

                // Unexplored: solid black
                float4 unexploredCol = _UnexploredColor;

                // Explored but not visible: semi-transparent black
                float4 exploredCol = _ExploredColor;

                // Start with unexplored (solid black)
                float4 result = unexploredCol;

                // If explored but not visible, blend to explored color
                result = lerp(result, exploredCol, isExplored);

                // If currently visible, fade to transparent
                result = lerp(result, clearCol, isVisible);

                return result;
            }
            ENDCG
        }
    }
}
