using UnityEngine;
using System;
using System.Collections;

public class LuaFunc : Attribute
{
    private string package;
    private string functionName;
    private string functionNameShort;
    private string functionDoc;
    private string[] functionParameters = null;

    public LuaFunc(string _package, string strFuncName, string strFuncDoc, params string[] strParamDocs)
    {
        package = _package;
        functionName = strFuncName;
        functionNameShort = strFuncName;
        functionDoc = strFuncDoc;
        functionParameters = strParamDocs;
    }

    public LuaFunc(string _package, string strFuncName, string strFuncDoc)
    {
        package = _package;
        functionName = strFuncName;
        functionNameShort = strFuncName;
        functionDoc = strFuncDoc;
    }

    public string GetPackageName()
    {
        return package;
    }

    public string GetFuncName()
    {
        return functionName;
    }

    public string GetShortFuncName()
    {
        return functionNameShort;
    }

    public string GetFuncDoc()
    {
        return functionDoc;
    }

    public string[] GetFuncParams()
    {
        return functionParameters;
    }
}

