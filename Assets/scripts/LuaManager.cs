using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NLua;

public class LuaManager : MonoBehaviour {
    private static object lockObj = new object();
    private static LuaInstance lua;
    public static string ThreadName = "Lua";
    public static Lua gLua
    {
        get
        {
            lock (lockObj)
            {
                return lua.instance;
            }
        }
    }
    public static Dictionary<string, Hashtable> LuaFunctions
    {
        get
        {
            lock (lockObj)
            {
                return lua.luaFunctions;
            }
        }
    }
    public static List<string> ClassList
    {
        get
        {
            lock (lockObj)
            {
                return lua.classList;
            }
        }
    }
    public static Dictionary<string, List<string>> PackageList
    {
        get
        {
            lock (lockObj)
            {
                return lua.packageList;
            }
        }
    }

    void Awake()
    {
        ConsoleWpr.Log("Starting Lua");
        Loom.AddAsyncThread(ThreadName);
        Loom.QueueAsyncTask(ThreadName, () => {
            try
            {
                ConsoleWpr.Log(ThreadName + " thread Started.");
                lua = new LuaInstance("global");
                RegisterLuaFunctions(lua, this);
                RegisterLuaFunctions(lua, GameManager.Instance);
                RegisterLuaFunctions(lua, typeof(ConsoleWpr));
            }
            catch (System.Exception e)
            {
                ConsoleWpr.LogError("\nMessage: " + e.Message + "\nFunction: LuaManager.Awake");
            }
        });
        DConsole.RegisterCommand(new DConsole.CommandDescription("Game", "Lua", "<input>", "make lua do things.", LuaCMD));
    }


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public static void AddToGlobalInstance(object pTarget)
    {
        RegisterLuaFunctions(lua, pTarget);
    }

    /// <summary>
    /// Registers the lua functions in pTarget with the global lua instance.
    /// Use for instances of classes.
    /// </summary>
    /// <param name="pTarget">Object containing functions to be registered</param>
    public static void RegisterLuaFunctions(LuaInstance _lua, object pTarget)
    {
        // sanity check
        if (_lua.instance == null || _lua.luaFunctions == null)
        {
            if (_lua.instance == null)
                ConsoleWpr.LogError("LuaManager: Lua is null");

            if (_lua.luaFunctions == null)
                ConsoleWpr.LogError("LuaManager: LuaFunctions is null");
            return;
        }

        // Get the target type
        System.Type pTrgType = pTarget.GetType();

        // Iterate through all target methods.
        foreach (MethodInfo mInfo in pTrgType.GetMethods())
        {
            // Sort through all the method's attributes.
            foreach (System.Attribute attr in System.Attribute.GetCustomAttributes(mInfo))
            {
                // if method is LuaFunc.
                if (attr.GetType() == typeof(LuaFunc))
                {
                    int error = 0;
                    try
                    {
                        // Get desired function name and doc string, along with parameter info.
                        LuaFunc pAttr = (LuaFunc)attr;
                        ArrayList pParams = new ArrayList();
                        ArrayList pParamDocs = new ArrayList();
                        string packageName = pAttr.GetPackageName();
                        string strFName = pAttr.GetFuncName();
                        string strFDoc = pAttr.GetFuncDoc();
                        string[] pPrmDocs = pAttr.GetFuncParams();

                        // Now get the expected parameters from the MethodInfo object
                        ParameterInfo[] pPrmInfo = mInfo.GetParameters();

                        // If they don't match, someone forgot to add some documentation to the
                        // attribute, complain and go to the next method

                        if (pPrmDocs != null && (pPrmInfo.Length != pPrmDocs.Length))
                        {
                            ConsoleWpr.LogError(string.Format("Function {0} (exported as {1}) argument number mismatch. Declared {2} but requires {3}.",
                                mInfo.Name, strFName, pPrmDocs.Length, pPrmInfo.Length));
                            continue;
                        }

                        // Build a parameter <-> parameter doc hashtable
                        for (int i = 0; i < pPrmInfo.Length; i++)
                        {
                            pParams.Add(pPrmInfo[i].Name);
                            pParamDocs.Add(pPrmDocs[i]);
                        }
                        error = 1;
                        // Get a new function descriptor from this information
                        string tableName = packageName;
                        string funcName = strFName;
                        if (!packageName.Equals(string.Empty))
                        {
                            LuaFuncDescriptor pDesc = new LuaFuncDescriptor(tableName + "." + funcName, strFDoc, pParams, pParamDocs);
                            // reguster it to the global hashtable and class list.
                            if (!_lua.classList.Contains(tableName))
                            {
                                _lua.classList.Add(tableName);
                                _lua.packageList.Add(tableName, new List<string>());
                                _lua.luaFunctions.Add(tableName, new Hashtable());
                                _lua.instance.NewTable(tableName);
                            }
                            if (!_lua.luaFunctions[tableName].ContainsKey(funcName))
                            {
                                _lua.packageList[tableName].Add(funcName);
                                _lua.luaFunctions[tableName].Add(tableName + "." + strFName, pDesc);
                                LuaTable table = _lua.instance.GetTable(tableName);
                                table[funcName] = _lua.instance.RegisterFunction(funcName, pTarget, mInfo);
                            }
                            //ConsoleWpr.Log(string.Format("Instanced function {0}.{1} registered.", tableName, funcName));
                        }
                        else
                        {
                            LuaFuncDescriptor pDesc = new LuaFuncDescriptor(funcName, strFDoc, pParams, pParamDocs);
                            if (!_lua.luaFunctions["global"].ContainsKey(strFName))
                            {
                                _lua.luaFunctions["global"].Add(strFName, pDesc);
                                _lua.instance.RegisterFunction(funcName, pTarget, mInfo);
                            }
                            //ConsoleWpr.Log(string.Format("Instanced function {0} registered.", funcName));
                        }
                    }
                    catch(System.Exception e){
                        ConsoleWpr.LogError("\n\tMessage: " + e.Message + "\n\tFunction: " + mInfo.Name + "\n\tScript: LuaManager" + "\n\tError: " + error);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Registers the lua function in the target type with the global lua instance.
    /// Use for static classes.
    /// </summary>
    /// <param name="pTarget">Type containing functions to be registered.</param>
    public static void RegisterLuaFunctions(LuaInstance _lua, System.Type pTarget)
    {
        // sanity check
        if (_lua.instance == null || _lua.luaFunctions == null)
        {
            if (_lua.instance == null)
                ConsoleWpr.LogError("Lua is null");

            if (_lua.luaFunctions == null)
                ConsoleWpr.LogError("LuaFunctions is null");
            return;
        }

        // Iterate through all target methods.
        foreach (MethodInfo mInfo in pTarget.GetMethods())
        {
            // Sort through all the method's attributes
            foreach (System.Attribute attr in System.Attribute.GetCustomAttributes(mInfo))
            {
                // if method happen to be one of our LuaFunc attributes
                if (attr.GetType() == typeof(LuaFunc))
                {
                    try
                    {
                        LuaFunc pAttr = (LuaFunc)attr;
                        ArrayList pParams = new ArrayList();
                        ArrayList pParamDocs = new ArrayList();
                        string packageName = pAttr.GetPackageName();
                        string strFName = pAttr.GetFuncName();
                        string strFDoc = pAttr.GetFuncDoc();
                        string[] pPrmDocs = pAttr.GetFuncParams();

                        // Now get the expected parameters from the MethodInfo object
                        ParameterInfo[] pPrmInfo = mInfo.GetParameters();

                        // If they don't match, someone forgot to add some documentation to the
                        // attribute, complain and go to the next method
                        if (pPrmDocs != null && (pPrmInfo.Length != pPrmDocs.Length))
                        {
                            ConsoleWpr.LogError(string.Format("Function {0} (exported as {1}) argument number mismatch. Declared {2} but requires {3}.",
                                mInfo.Name, strFName, pPrmDocs.Length, pPrmInfo.Length));
                            continue;
                        }

                        // Build a parameter <-> parameter doc hashtable
                        for (int i = 0; i < pPrmInfo.Length; i++)
                        {
                            pParams.Add(pPrmInfo[i].Name);
                            pParamDocs.Add(pPrmDocs[i]);
                        }

                        string tableName = packageName;
                        string funcName = strFName;
                        if (!packageName.Equals(string.Empty))
                        {
                            // Get a new function descriptor from this information
                            LuaFuncDescriptor pDesc = new LuaFuncDescriptor(tableName + "." + funcName, strFDoc, pParams, pParamDocs);
                            // register it to the global hashtable and class list.
                            if (!_lua.classList.Contains(tableName))
                            {
                                _lua.instance.NewTable(tableName);
                                _lua.classList.Add(tableName);
                                _lua.packageList.Add(tableName, new List<string>());
                                _lua.luaFunctions.Add(tableName, new Hashtable());
                            }
                            if (!_lua.luaFunctions[tableName].ContainsKey(funcName))
                            {
                                _lua.packageList[tableName].Add(funcName);
                                _lua.luaFunctions[tableName].Add(tableName + "." + strFName, pDesc);
                                LuaTable table = _lua.instance.GetTable(tableName);
                                table[funcName] = _lua.instance.RegisterFunction(funcName, pTarget, mInfo);
                            }
                            //ConsoleWpr.Log(string.Format("Static function {0}.{1} registered.", tableName, funcName));
                        }
                        else
                        {
                            // Get a new function descriptor from this information
                            LuaFuncDescriptor pDesc = new LuaFuncDescriptor(funcName, strFDoc, pParams, pParamDocs);
                            if (!_lua.luaFunctions["global"].ContainsKey(funcName))
                            {
                                _lua.luaFunctions["global"].Add(strFName, pDesc);
                                _lua.instance.RegisterFunction(funcName, pTarget, mInfo);
                            }
                            //ConsoleWpr.Log(string.Format("Static function {0} registered.", funcName));
                        }
                    }
                    catch (System.Exception e)
                    {
                        ConsoleWpr.LogError("\n\tMessage: " + e.Message + "\n\tFunction: " + mInfo.Name + "\n\tScript: LuaManager");
                    }
                }
            }
        }
    }

    object LuaCMD(params string[] args)
    {
        string input = string.Empty;
        string output = string.Empty;
        for (int i = 1; i < args.Length; i++)
        {
            input += args[i];
        }
        Loom.QueueAsyncTask(ThreadName, () =>
        {
            int error = 0;
            try
            {
                System.String[] stringArray = (System.String[])gLua.DoString(input);
                if (stringArray != null)
                {
                    foreach (System.String val in stringArray)
                    {
                        if (val != null)
                        {
                            output += val + "\n";
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {
                ConsoleWpr.LogError(exception.Message + ", error: " + error);
            }
            ConsoleWpr.Log(output);
        });
        return "";
    }

    [LuaFunc("", "ListFunctions", "Shows -every- function or every package (there are a LOT of them).")]
    public void ListFunctions()
    {
        Loom.QueueOnMainThread(() =>
        {
            ConsoleWpr.Log("Available functions: ");
            ConsoleWpr.Log("");

            foreach (string package in LuaFunctions.Keys)
            {
                IDictionaryEnumerator funcs = LuaFunctions[package].GetEnumerator();
                while (funcs.MoveNext())
                {
                    ConsoleWpr.Log("= " + ((LuaFuncDescriptor)funcs.Value).GetFuncHeader());
                }
            }
        });
    }

    [LuaFunc("", "Package", "List availible functions in package.", "package")]
    public void Package(object _package)
    {
        Loom.QueueOnMainThread(() =>
        {
            ConsoleWpr.LogFormat("Available function in {0}: ", _package);
            ConsoleWpr.Log("");

            if (PackageList.ContainsKey(_package.ToString()))
            {
                List<string> functions = PackageList[_package.ToString()];
                foreach (string function in functions)
                {
                    ConsoleWpr.Log(_package + "." + function);
                }
            }
        });
    }

    [LuaFunc("", "ListPackages", "List availible packages.")]
    public void ListPackages()
    {
        Loom.QueueOnMainThread(() =>
        {
            ConsoleWpr.LogFormat("Available packages: ");
            ConsoleWpr.Log("");

            foreach (string package in PackageList.Keys)
            {
                ConsoleWpr.Log(package);
            }
        });
    }

    [LuaFunc("", "help", "Show help for a given function", "Command to get help for.")]
    public void helpCmd(object strCmd)
    {
        Loom.QueueOnMainThread(() =>
        {
            if (strCmd.ToString().Split('.').Length == 1)
            {
                strCmd = "global." + strCmd.ToString();
            }
            string[] parts = strCmd.ToString().Split('.');


            if (LuaFunctions.ContainsKey(parts[0]))
            {
                if (LuaFunctions[parts[0]].ContainsKey(parts[1]))
                {
                    LuaFuncDescriptor pDesc = (LuaFuncDescriptor)LuaFunctions[parts[0]][parts[1]];
                    ConsoleWpr.Log(pDesc.GetFuncFullDoc());
                }
                else
                    ConsoleWpr.Log("No such function in package '" + parts[0] + "': " + parts[1]);
            }
            else
                ConsoleWpr.Log("No such package: " + parts[0]);
        });
    }

    [LuaFunc("", "test", "a test.")]
    public void Test()
    {
        ConsoleWpr.Log("a test.");
    }

    [LuaFunc("TestPackage", "test", "Test same function name in different packages.")]
    public void Test2()
    {
        ConsoleWpr.Log("a test 2.");
    }

    [LuaFunc("", "PrintArray", "Print a string array", "String array")]
    public void PrintArray(object[] input)
    {
        foreach (object str in input)
        {
            ConsoleWpr.Log(str.ToString());
        }
    }
}

public struct LuaInstance
{
    public string name;
    public Lua instance;
    public Dictionary<string, Hashtable> luaFunctions;
    public List<string> classList;
    public Dictionary<string, List<string>> packageList;

    public LuaInstance(string _name)
    {
        name = _name;
        instance = new Lua();
        luaFunctions = new Dictionary<string,Hashtable>();
        classList = new List<string>();
        packageList = new Dictionary<string, List<string>>();

        luaFunctions.Add("global", new Hashtable());
    }
}
