using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UiButtonListen : MonoBehaviour {
    public string CallFunction;
    private UImanager.Button_Click CallBack;

	// Use this for initialization
	void Start () {
        UImanager.RegisterItem(gameObject);
        GetComponent<Button>().onClick.AddListener(() => { Event(); });
        if (CallFunction != string.Empty)
        {
            CallBack = (UImanager.Button_Click)UImanager.GetCallback<UImanager.Button_Click>(CallFunction);

            if (CallBack == null)
            {
                Debug.LogError(name + ": Failed to get callback function " + CallFunction);
            }
        }
	}

    public void Event()
    {
        if (CallBack != null)
        {
            CallBack(gameObject);
        }
        else
            Debug.LogError(name + " has no function callback set.");
    }
}
