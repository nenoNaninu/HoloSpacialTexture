Shader "Custom/SpatialTexture"
{
     Properties
     {
          _TextureArray("TextureArray", 2DArray) = "" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Cull off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            
            // make fog work
            // #pragma multi_compile_fog

            #include "UnityCG.cginc"

            uniform StructuredBuffer<float2> _UVArray;
            uniform StructuredBuffer<int> _TextureIndexArray;
            uniform int _TextureCount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint id : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;//xにテクスチャidx,yに頂点id
                // UNITY_FOG_COORDS(1)
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            UNITY_DECLARE_TEX2DARRAY(_TextureArray);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);//クリッピング空間上の座標に変換
                //この段階ではuvにはテクスチャidと頂点idを突っ込んでおく。
                o.uv.x = _TextureIndexArray[v.id];
                o.uv.y = v.id;
                return o;
            }

            [maxvertexcount(9)]
            void geom(triangle v2f IN[3], inout TriangleStream<g2f> tristream)//ここ不必要なものいろいろありそうだからデバッグしていきたい。
            {
                bool existTexture = false;
                g2f o;

                for(int vertexIdx = 0; vertexIdx < 3; ++vertexIdx)//頂点毎に見ていって、他の頂点も同じテクスチャでuvの値持っていれば使い、持っていなければ使わない。
                {
                    bool visible = true;
                    for(int i = 0; i < 3; ++i)
                    {
                        uint vertexId = floor(IN[i].uv.x);
                        uint textureId = floor(IN[vertexIdx].uv.x);

                        float2 uv = _UVArray[_TextureCount * vertexId + textureId];

                        if(uv.x < -0.5 || uv.y < -0.5)
                        {
                            visible = false;
                        }
                    }
                    if(visible){
                        existTexture = true;

                        for(int i = 0; i < 3; ++i)
                        {                        
                            uint vertexId = floor(IN[i].uv.x);
                            uint textureId = floor(IN[vertexIdx].uv.x);
                            
                            o.pos = IN[i].vertex;

                            o.uv = _UVArray[_TextureCount * vertexId + textureId];
                            o.uv2.x = floor(IN[vertexIdx].uv.x);
                            o.uv2.y = -1;
                            tristream.Append(o);
                        }

                        tristream.RestartStrip();
                    }
                }

                if(!existTexture){
                    for(int i = 0; i< 3; ++i)
                    {
                        o.pos = IN[i].vertex;
                        o.uv = float2(-1,-1);
                        tristream.Append(o);
                    }
                    tristream.RestartStrip();
                }
            }
               
            fixed4 frag (g2f i) : SV_Target
            {
                fixed4 color = fixed4(0, 0, 0, 0);

                if (i.uv.x < 0 || i.uv.x > 1 || i.uv.y < 0 || i.uv.y > 1) 
                {
                   return color;
                }

                uint index = floor(i.uv2.x);
                float3 uvz = float3(i.uv.x, i.uv.y, index);

                color = UNITY_SAMPLE_TEX2DARRAY(_TextureArray, uvz);
                return color;
            }
            ENDCG
          }
     }
}
