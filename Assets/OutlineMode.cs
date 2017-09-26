using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineMode : MonoBehaviour {
    
	// Update is called once per frame
	public void SetColor(Color32 color) {
        gameObject.GetComponent<Renderer>().material.SetColor("_OutlineColor", color);
	}
}
