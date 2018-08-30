using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Neno.Scripts
{
    /// <summary>
    /// それぞれのmeshに対して
    /// </summary>
    public class SpatialTexture : MonoBehaviour
    {

        private ComputeBuffer uvBuffer;
        private ComputeBuffer textureIndexBuffer;

        public void ApplyTexture(Texture2DArray texture2DArray, List<Matrix4x4> world2cameraList, List<Matrix4x4> projectionMatrixList)
        {
            Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;

            Vector3[] vertecs = mesh.vertices;
            int photoNum = world2cameraList.Count;

            //x,yの2つの要素を一つの要素として写真の枚数分この配列に格納。あとでGPU側に投げる
            var uvArray = new float[vertecs.Length * 2 * photoNum];

            //使うテクスチャの番号使わない場合は-1を格納します。これもあとでGPUに投げる。
            var textureIdxArray = new int[vertecs.Length];

            //初期化
            for (int i = 0; i < uvArray.Length; i++)
            {
                uvArray[i] = -1;
            }

            for (int i = 0; i < vertecs.Length; i++)
            {
                textureIdxArray[i] = -1;
            }

            int vertecsIdx = 0;
            foreach (var v in vertecs)
            {
                int textureIdx = -1;
                float score = 0;

                for (int i = 0; i < photoNum; i++)
                {
                    //vをローカル座標から世界座標系に
                    Vector3 worldSpacePosition = transform.TransformPoint(new Vector3(v.x, v.y, v.z));

                    Matrix4x4 world2CameraMatrix = world2cameraList[i];
                    Matrix4x4 projectionMatrix = projectionMatrixList[i];

                    //カメラ座標系に変換
                    Vector3 cameraSpacePosition = world2CameraMatrix.MultiplyPoint(worldSpacePosition);

                    //Hololensでのカメラの座標系は、カメラが向いている方向がzが負の方向。カメラの売りろ側は映ってないからスルー
                    //詳しくは https://docs.microsoft.com/en-us/windows/mixed-reality/locatable-camera
                    if (0 <= cameraSpacePosition.z)
                    {
                        continue;
                    }

                    //カメラ座標系上の点とプロジェクション行列かけて表示するところの空間に写像する
                    //(あの空間のことをhomogeneous coordinatesっていうところもあるみたいだけど。
                    //同次座標って意味だとなんか違うのでprojected spaceとかにしておきます。)

                    Vector3 projectedSpacePosition = projectionMatrix.MultiplyPoint(cameraSpacePosition);

                    //左下原点。平面で考える。
                    Vector2 projectedPanelPosition = new Vector2(projectedSpacePosition.x, projectedSpacePosition.y);

                    if (float.IsNaN(projectedPanelPosition.x) || float.IsNaN(projectedPanelPosition.y))
                    {
                        continue;
                    }

                    //プロジェクション行列かけられた空間は(普通)-1~1の空間の中。
                    //ここめっちゃわかりやすい。 http://marupeke296.com/DXG_No70_perspective.html
                    if (Mathf.Abs(projectedPanelPosition.x) <= 2f && Mathf.Abs(projectedPanelPosition.y) <= 2f)
                    {
                        //-1~1の空間を0~1の空間に写像
                        Vector2 normalizedProjectPosition = 0.5f * Vector2.one + 0.5f * projectedPanelPosition;

                        if (CameraManager.Instance.CorrespondingPositionInTexture(normalizedProjectPosition))
                        {
                            Vector2 uvPos = CameraManager.Instance.ConvertTexturePoint(normalizedProjectPosition);
                            float newScore = 10 - Mathf.Abs(uvPos.x - 0.5f) - Mathf.Abs(uvPos.y - 0.5f);
                            if (score < newScore)
                            {
                                score = newScore;
                                textureIdx = i;
                            }

                            uvArray[2 * vertecsIdx * photoNum + 2 * i] = uvPos.x;
                            uvArray[2 * vertecsIdx * photoNum + 2 * i + 1] = uvPos.y;
                        }
                    }
                }

                textureIdxArray[vertecsIdx] = textureIdx;
                vertecsIdx++;
            }//foreach (var v in vertecs)

            if (vertecs.Length == 0)
            {
                return;
            }

            Material material = gameObject.GetComponent<Renderer>().material;

            uvBuffer?.Release();
            uvBuffer = new ComputeBuffer(vertecs.Length * photoNum, sizeof(float) * 2);
            uvBuffer.SetData(uvArray);
            material.SetBuffer("_UVArray", uvBuffer);

            textureIndexBuffer?.Release();
            textureIndexBuffer = new ComputeBuffer(textureIdxArray.Length, sizeof(int));
            textureIndexBuffer.SetData(textureIdxArray);
            material.SetBuffer("_TextureIdxArray", textureIndexBuffer);

            material.SetInt("_TextureCount", photoNum);
            material.SetTexture("_TextureArray", texture2DArray);
        }
    }
}

//worldCordinate -> cameraCordinate -> projectionCordinate