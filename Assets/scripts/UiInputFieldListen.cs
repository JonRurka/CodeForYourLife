using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UiInputFieldListen : MonoBehaviour
{
    public string CallFunction;
    private UImanager.InputField_Submit CallBack;

    // Use this for initialization
    void Start()
    {
        UImanager.RegisterItem(gameObject);
        GetComponent<InputField>().onSubmit.AddListener((string s) => { Event(s); });
        if (CallFunction != string.Empty)
        {
            CallBack = (UImanager.InputField_Submit)UImanager.GetCallback<UImanager.InputField_Submit>(CallFunction);

            if (CallBack == null)
            {
                Debug.LogError(name + ": Failed to get callback function " + CallFunction);
            }
        }
    }

    public void Event(string input)
    {
        if (CallBack != null)
        {
            CallBack(gameObject, input);
        }
        else
            Debug.LogError(name + " has no function callback set.");
    }
}
