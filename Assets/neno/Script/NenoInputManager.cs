using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using UnityEngine;

namespace Neno.Scripts
{
    public class NenoInputManager : Singleton<NenoInputManager>, IInputClickHandler
    {
        public void OnInputClicked(InputClickedEventData eventData)
        {
            if (CameraManager.Instance.CanTakePhoto)
            {
                RaycastHit hitInfo;

                if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo, Mathf.Infinity))
                {
                    if (hitInfo.transform.gameObject.layer == 31)
                    {
                        CameraManager.Instance.TakePhotoAsync();
                    }
                }
            }
        }

        // Use this for initialization
        void Start()
        {
            InputManager.Instance.AddGlobalListener(gameObject);
        }
    }
}
