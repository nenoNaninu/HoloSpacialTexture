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
                uint id : SV_VertexID;
            };

            struct v2f {
                float4 vertex : POSITION; //クリッピング空間に投影された頂点を格納(たぶん-1~1になる)
                float2 uv : TEXCOORD0;    //uv配列に入っていたuv座標を
                float2 uv2 : TEXCOORD1;   //テクスチャ番号(x)と頂点id(y)を格納
            };

            struct g2f
            {
                float4 pos : SV_POSITION; //pos使わないけどね
                float2 uv : TEXCOORD0;    //uv座標
                float2 uv2 : TEXCOORD1;   //xにテクスチャのidx入れる。yは使わない。
            };

            //頂点シェーダー
            //頂点番号とテクスチャ番号と
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);//クリッピング空間の座標系に変換
                o.uv = _UVArray[v.id];//uv座標入れてた配列のuvArray[index]
                o.uv2.x = _TextureIndexArray[v.id]; //これはスコアが高かった画像のindexが入ってる。
                o.uv2.y = v.id;//頂点id
                return o;
            }

            UNITY_DECLARE_TEX2DARRAY(_TextureArray);
            
            //ジオメトリシェーダー
            //入力に3角のプリミティブな面を入力として各頂点についてみていく。
            [maxvertexcount(9)]
            void geom(triangle v2f IN[3], inout TriangleStream<g2f> tristream)
            {
                bool existTexture = false;
                g2f o;
                
                //頂点毎に見ていって、他の頂点も同じテクスチャでuvの値持っていれば使い、持っていなければ使わない。
                for(int vIndex = 0; vIndex < 3; ++vIndex)
                {
                    bool visible = true; //vIndexが持っているテクスチャ番号を、他の2点も候補点を持っていればtrue。持ってなければfalse
                    for(int i = 0; i < 3; ++i)
                    {
                        uint vertexId = floor(IN[i].uv2.y);
                        uint textureId = floor(IN[vIndex].uv2.x);

                        if(textureId < 0)//該当テクスチャがなければ-1が入っている
                        {
                            visible = false;
                        }
                        else
                        {
                            float2 uv = _UVArray[_TextureCount * vertexId + textureId];

                            if(uv.x < -0.5 || uv.y < -0.5)//まともなではないuvが入ってる場合は-1が入ってる。
                            {
                                visible = false;
                            }
                        }
                    }
                    if(visible){
                        existTexture = true;

                        for(int i = 0; i < 3; ++i)
                        {                        
                            uint vertexId = floor(IN[i].uv2.y);
                            uint textureId = floor(IN[vIndex].uv2.x);
                            
                            o.pos = IN[i].vertex;

                            o.uv = _UVArray[_TextureCount * vertexId + textureId];
                            o.uv2.x = textureId;
                            o.uv2.y = -1;
                            tristream.Append(o);
                        }

                        tristream.RestartStrip();//break的な感じの理解でよろしいのだろうか。
                    }
                }

                //表示するべきテクスチャがいい感じ
                if(!existTexture){
                    for(int i = 0; i< 3; ++i)
                    {
                        o.pos = IN[i].vertex;
                        o.uv = float2(-1,-1);
                        o.uv2 = float2(-1,-1);
                        tristream.Append(o);
                    }
                    tristream.RestartStrip();
                }
            }
        
            //フラグメントシェーダ
            //頂点のuv座標とテクスチャのindex
            fixed4 frag (g2f i) : SV_Target
            {
                fixed4 color = fixed4(0, 0, 0, 0);
                uint textureIdex = floor(i.uv2.x);

                if (i.uv.x < 0 || i.uv.x > 1 || i.uv.y < 0 || i.uv.y > 1 || textureIdex < 0)
                {
                   return color;
                }

                float3 uvz = float3(i.uv.x, i.uv.y, textureIdex);
                if (!any(saturate(i.uv) - i.uv)) {
                    color = UNITY_SAMPLE_TEX2DARRAY(_TextureArray, uvz);
                }
                return color;
            }
            ENDCG
          }
     }
}
