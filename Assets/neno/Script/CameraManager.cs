using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.WSA.WebCam;

namespace Neno.Scripts
{
    /// <summary>
    /// カメラで写真の撮影、及び撮影した画像の(テクスチャとして)保存,
    /// 撮影されたタイミングでのカメラ行列などを保存しておくクラス
    /// めんどうなのでどこからでもアクセスできます。
    /// </summary>
    public class CameraManager : Singleton<CameraManager>
    {
        [SerializeField] private Text text;
        private Texture2DArray texture2DArray;//GPUのメモリに保存される。

        private PhotoCapture photoCaptureObject;
        private CameraParameters cameraParameters;
        private Resolution resolution;

        private List<Matrix4x4> projectionMatrixList = new List<Matrix4x4>();

        private List<Matrix4x4> world2CameraMatrixList = new List<Matrix4x4>();
        private int maxPhotoNum = 1;

        //GPUに優しくなるため、2^nにしておく。
        private const int TEXTURE_WIDTH = 1024;
        private const int TEXTURE_HEIGHT = 512;

        private int currentPhotoCount = 0;

        private bool isCapturingPhoto = false;

        public event Action OnTextureUpdated;

        public bool CanTakePhoto { get; set; } = false;

        /// <summary>
        /// テクスチャ配列が更新したら呼ばれる。
        /// </summary>
        public Action UpdateTextureArray { get; set; }

        void Start()
        {

            //this.resolution = PhotoCapture.SupportedResolutions.OrderByDescending((res => res.width * res.height)).First();
            this.resolution = PhotoCapture.SupportedResolutions.First();//1280*720

            this.cameraParameters = new CameraParameters(WebCamMode.PhotoMode)
            {
                cameraResolutionHeight = resolution.height,
                cameraResolutionWidth = resolution.width,
                hologramOpacity = 0f,
                pixelFormat = CapturePixelFormat.BGRA32
            };

            this.texture2DArray = new Texture2DArray(TEXTURE_WIDTH, TEXTURE_HEIGHT, this.maxPhotoNum, TextureFormat.DXT5, false);//DXT5がDirectXが良しなにするためのフォーマット
            //var clearTexture = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT, TextureFormat.ARGB32, false);

            //var resetClearArray = clearTexture.GetPixels();

            //for (int i = 0; i < resetClearArray.Length; i++)
            //{
            //    resetClearArray[i] = Color.clear;
            //}
            //clearTexture.SetPixels(resetClearArray);
            //clearTexture.Apply();
            //clearTexture.Compress(true);
            //Graphics.CopyTexture(clearTexture, 0, 0, texture2DArray, 0, 0);

            PhotoCapture.CreateAsync(false, CreateCaptureObj);
            this.CanTakePhoto = true;
        }


        void CreateCaptureObj(PhotoCapture captureObject)
        {
            this.photoCaptureObject = captureObject;
            this.photoCaptureObject.StartPhotoModeAsync(cameraParameters, _ => { });
        }

        public void TakePhoto()
        {
            if (this.isCapturingPhoto)
            {
                return;
            }

            text.text += "start take photo\n";

            isCapturingPhoto = true;
            this.photoCaptureObject.TakePhotoAsync(OnPhotoCaptured);
            isCapturingPhoto = false;

        }

        /// <summary>
        /// カメラ行列、プロジェクション行列の保存とテクスチャの保存(あとでサーバーに投げて変換された画像の保存に変える)
        /// </summary>
        /// <param name="result"></param>
        /// <param name="photoCaptureFrame"></param>
        void OnPhotoCaptured(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            Matrix4x4 cameraToWorldMatrix;

            photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
            Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

            Matrix4x4 projectionMatrix;
            photoCaptureFrame.TryGetProjectionMatrix(out projectionMatrix);

            this.projectionMatrixList.Add(projectionMatrix);
            this.world2CameraMatrixList.Add(worldToCameraMatrix);

            var texture = new Texture2D(this.cameraParameters.cameraResolutionWidth, this.cameraParameters.cameraResolutionHeight, TextureFormat.ARGB32, false);
            photoCaptureFrame.UploadImageDataToTexture(texture);
            ////ここから
            //var bytesTmp = texture.EncodeToPNG();
            //File.WriteAllBytes(Application.persistentDataPath + "/RoomFull" + (currentPhotoCount + 1) + ".png", bytesTmp);
            ////ここまでリサイズ前の画像を見たいがためのデバッグ用コード

            texture.wrapMode = TextureWrapMode.Clamp;
            texture = CropTexture(texture, TEXTURE_WIDTH, TEXTURE_HEIGHT);
            photoCaptureFrame.Dispose();

            //var bytes = texture.EncodeToPNG();
            //text.text += "save photo \n" + Application.persistentDataPath + "/Room" + (currentPhotoCount + 1) + ".png";

            ////write to LocalState folder
            //File.WriteAllBytes(Application.persistentDataPath + "/Room" + (currentPhotoCount + 1) + ".png", bytes);

            texture.Compress(true);//ここでの圧縮はDXTフォーマットに圧縮するということ。
            Graphics.CopyTexture(texture, 0, 0, texture2DArray, currentPhotoCount, 0);
            currentPhotoCount++;

            OnTextureUpdated?.Invoke();
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// カメラから得られた画像を左下(0, input.height - height)を原点にcropする。inputは破壊されます。
        /// </summary>
        /// <param name="input"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        Texture2D CropTexture(Texture2D input, int width, int height)
        {
            Color[] pix = input.GetPixels(0, input.height - height, width, height);
            input.Resize(width, height);
            input.SetPixels(pix);
            input.Apply();
            return input;
        }

        void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
        {
            photoCaptureObject.Dispose();
            photoCaptureObject = null;
        }

        public void StopCamera()
        {
            photoCaptureObject?.StopPhotoModeAsync(OnStoppedPhotoMode);
        }
    }
}

