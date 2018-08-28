using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine;

namespace Neno.Scripts
{
    public class NenoInputManager : Singleton<NenoInputManager>, IInputClickHandler
    {
        public void OnInputClicked(InputClickedEventData eventData)
        {
            if (CameraManager.Instance.CanTakePhoto)
            {
                CameraManager.Instance.TakePhoto();
            }
        }

        // Use this for initialization
        void Start()
        {
            InputManager.Instance.AddGlobalListener(gameObject);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
