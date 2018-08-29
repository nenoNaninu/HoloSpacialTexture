using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonScript : MonoBehaviour
{
    private Text textUI;
    // Use this for initialization
	void Start ()
	{
	    textUI = gameObject.transform.parent.Find("Text").GetComponent<Text>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnClidk()
    {
        textUI.text = "click!";
    }
}
