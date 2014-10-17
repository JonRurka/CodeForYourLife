using UnityEngine;
using System.Collections;

/// <summary>
/// UI Listener that only registers the object with UImanager.
/// </summary>
public class UiListen : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
        UImanager.RegisterItem(gameObject);
    }
}
