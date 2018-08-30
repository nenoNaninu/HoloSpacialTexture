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
            SpatialMappingSource[] spatialMappingObserver = spatialMapping.GetComponents<SpatialMappingSource>();

            //メッシュが増えたらそのメッシュのGameオブジェクトにSpatialTextureをAddComponetする。
            foreach (var sc in spatialMappingObserver)
            {
                sc.SurfaceAdded += (sender, e) =>
                {
                    GameObject meshObj = e.Data.Object;
                    if (meshObj.GetComponent<SpatialTexture>() == null)
                    {
                        meshObj.AddComponent<SpatialTexture>();
                    }
                };

                sc.SurfaceUpdated += (sender, e) =>
                {
                    var spatialTexture = e.Data.New.Object.GetComponent<SpatialTexture>();
                    if (spatialTexture != null)
                    {
                        e.Data.New.Object.AddComponent<SpatialTexture>();
                    }
                };
            }

            CameraManager.Instance.UpdateTextureArray = new Action(ApplyAllTexture);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ApplyAllTexture()
        {
            if (SpatialUnderstanding.Instance.UnderstandingCustomMesh.MeshMaterial != this.spatialTextureMaterial)
            {
                SpatialUnderstanding.Instance.UnderstandingCustomMesh.MeshMaterial = this.spatialTextureMaterial;
            }

            SpatialTexture[] spatialTextures = spatialMapping.GetComponentsInChildren<SpatialTexture>();

            foreach (var spatialTexture in spatialTextures)
            {
                spatialTexture.ApplyTexture(CameraManager.Instance.texture2DArray, CameraManager.Instance.world2CameraMatrixList, CameraManager.Instance.projectionMatrixList);
            }
        }
    }
}

