using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using Neno.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Neno.Scripts
{
    public class ButtonManager : MonoBehaviour
    {

        [SerializeField] private GameObject spatialMappingObj;
        private Text spatialButtonText;
        private Text cameraActiveBottonText;
        private Text clearAllTextureButtonText;
        private bool isSpacialMappingUpdate = true;
        private bool isActiveCamera = false;

        // Use this for initialization
        void Start()
        {
            //Text textUI = gameObject.transform.parent.Find("Text").GetComponent<Text>();
            this.spatialButtonText =
                gameObject.transform.parent.Find("SpatialControllButton").GetComponentInChildren<Text>();
            this.cameraActiveBottonText =
                gameObject.transform.parent.Find("CameraActiveButton").GetComponentInChildren<Text>();
            this.clearAllTextureButtonText =
                gameObject.transform.parent.Find("ClearTextureButton").GetComponentInChildren<Text>();

        }

        public void ChangeSpatialmappingUpdate()
        {
            this.isSpacialMappingUpdate = !this.isSpacialMappingUpdate;
            if (this.isSpacialMappingUpdate)
            {
                //SpatialMappingManager.Instance.StartObserver();
                SpatialUnderstanding.Instance.RequestBeginScanning();
                FlipCameraActive();
            }
            else
            {
                //SpatialMappingManager.Instance.StopObserver();
                SpatialUnderstanding.Instance.RequestFinishScan();
                FlipCameraActive();
                //SpatialTextureManager.Instance.ApplyAllTexture();
            }
        }

        /// <summary>
        /// カメラの有効無効を反転させる関数
        /// </summary>
        public void FlipCameraActive()
        {
            this.isActiveCamera = !this.isActiveCamera;
            cameraActiveBottonText.text = isActiveCamera ? "Camera\nActive" : "Camera\nDisactive";
            CameraManager.Instance.CanTakePhoto = this.isActiveCamera;

        }
    }
}