using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UiInputFieldListen : MonoBehaviour
{
    public string ChangeFunction = string.Empty;
    public string SubmitFunction = string.Empty;
    private UImanager.InputField_Submit ChangeCallback;
    private UImanager.InputField_Submit SubmitCallBack;

    // Use this for initialization
    void Start()
    {
        UImanager.RegisterItem(gameObject);
        GetComponent<InputField>().onValueChange.AddListener((string s) => { Change(s); });
        GetComponent<InputField>().onEndEdit.AddListener((string s) => { Submit(s); });

        ChangeCallback = GetCallback(ChangeFunction);
        SubmitCallBack = GetCallback(SubmitFunction);
    }

    public void Change(string input)
    {
        if (ChangeCallback != null)
        {
            ChangeCallback(gameObject, input);
        }
    }

    public void Submit(string input)
    {
        if (SubmitCallBack != null)
        {
            SubmitCallBack(gameObject, input);
        }
    }

    UImanager.InputField_Submit GetCallback(string _callback)
    {
        UImanager.InputField_Submit result = null;
        if (_callback != string.Empty)
        {
            DConsole.Log(_callback);
            result = (UImanager.InputField_Submit)UImanager.GetCallback<UImanager.InputField_Submit>(_callback);
            if (result == null)
            {
                Debug.LogError(name + ": Failed to get callback function " + _callback);
            }
        }
        return result;
    }
}
