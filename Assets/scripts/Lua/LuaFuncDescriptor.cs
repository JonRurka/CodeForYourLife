using UnityEngine;
using System.Collections;

public class LuaFuncDescriptor {
    private string functionName;
    private string functionDoc;
    private ArrayList functionParameters;
    private ArrayList functionParamDocs;
    private string functionDocString;

    public LuaFuncDescriptor(string strFuncName, string strFuncDoc, ArrayList strParams, ArrayList strParamDocs) {
        functionName = strFuncName;
        functionDoc = strFuncDoc;
        functionParameters = strParams;
        functionParamDocs = strParamDocs;

        string strFunctionHeader = strFuncName + "(%params%) - " + strFuncDoc;
        string strFuncBody = "\n\n";
        string strFuncParams = "";

        bool bFirst = true;

        for (int i = 0; i < strParams.Count; i++) {
            if (!bFirst) {
                strFuncParams += ", ";
            }
            strFuncParams += strParams[i];
            strFuncBody += "\t" + strParams[i] + "\t\t" + strParamDocs[i] + "\n";

            bFirst = false;
        }

        strFuncBody = strFuncBody.Substring(0, strFuncBody.Length - 1);
        if (bFirst)
            strFuncBody = strFuncBody.Substring(0, strFuncBody.Length - 1);

        functionDocString = strFunctionHeader.Replace("%params%", strFuncParams);
    }

    public string GetFuncName() {
        return functionName;
    }

    public string GetFuncDoc() {
        return functionDoc;
    }

    public ArrayList GetFuncParams() {
        return functionParameters;
    }

    public string GetFuncHeader() {
        if (functionDocString.IndexOf("\n") == -1)
            return functionDocString;

        return functionDocString.Substring(0, functionDocString.IndexOf("\n"));
    }

    public string GetFuncFullDoc() {
        return functionDocString;
    }
}

