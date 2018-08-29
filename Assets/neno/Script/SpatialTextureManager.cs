using System;
using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using UnityEngine;

namespace Neno.Scripts
{
    public class SpatialTextureManager : Singleton<SpatialTextureManager>
    {

        [SerializeField] private GameObject spatialMapping;
        [SerializeField] private Material spatialTextureMaterial;

        // Use this for initialization
        void Start()
        {
            SpatialMappingObserver spatialMappingObserver = spatialMapping.GetComponent<SpatialMappingObserver>();

            //メッシュが増えたらそのメッシュのGameオブジェクトにSpatialTextureをAddComponetする。
            spatialMappingObserver.SurfaceAdded += (sender, e) =>
            {
                GameObject meshObj = e.Data.Object;
                if (meshObj.GetComponent<SpatialTexture>() == null)
                {
                    meshObj.AddComponent<SpatialTexture>();
                }
            };

            CameraManager.Instance.UpdateTextureArray = new Action(ApplyAllTexture);
        }

        /// <summary>
        /// 
        /// </summary>
        void ApplyAllTexture()
        {
            if (SpatialMappingManager.Instance.SurfaceMaterial != this.spatialTextureMaterial)
            {
                SpatialMappingManager.Instance.SurfaceMaterial = this.spatialTextureMaterial;
            }

            SpatialTexture[] spatialTextures = spatialMapping.GetComponentsInChildren<SpatialTexture>();

            //foreach (var spatialTexture in spatialTextures)
            //{
            //    spatialTexture.ApplyTexture(CameraManager.Instance.texture2DArray, CameraManager.Instance.world2CameraMatrixList, CameraManager.Instance.projectionMatrixList);
            //}
        }
    }
}

