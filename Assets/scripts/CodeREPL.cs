using UnityEngine;
using uLink;
using System;
using System.Collections;
using System.Collections.Generic;
using NLua;

public class CodeREPL : uLink.MonoBehaviour {
    [Serializable]
    public class REPLInfo
    {
        public int ID;
        public string Name;
        public List<string> Text;
        public Vector2 ScrollPos;
        public DConsole.History History;

        public REPLInfo(int id, string name)
        {
            ID = id;
            Name = name;
            Text = new List<string>();
            ScrollPos = new Vector2();
            History = new DConsole.History();
        }

        public void AddText(string text)
        {
            Text.Add("> " + text);
            History.Add(text);
        }

        public void SetScrollPos(Vector2 newPos)
        {
            ScrollPos = newPos;
            //DConsole.Log("Scoll bar pos set to " + newPos + ", " + ScrollPos);
        }

        public override string ToString()
        {
            string result = string.Empty;
            foreach (string str in Text)
            {
                result += str + "\n";
            }
            return result;
        }
    }

    public static CodeREPL _instance;
    public static CodeREPL Instance
    {
        get { 
            if (_instance == null)
            {
                DConsole.LogError("CodeREPL is null!");
            }

            return _instance; 
        }
    }

    public CharacterControl player;
    public LuaInstance lua;
    public string inputString = string.Empty;
    public List<REPLInfo> PlayerREPL = new List<REPLInfo>();

    GUIContent guiContent = new GUIContent();

    //Vector2[] scollPos = new Vector2[4];

    void Awake()
    {
        _instance = this;
    }

	// Use this for initialization
	void Start () {
        PlayerREPL.Add(new REPLInfo(PlayerREPL.Count, GameManager.Instance.PlayerName));
        //PlayerREPL.Add(new REPLInfo(PlayerREPL.Count, "Bob"));
        //PlayerREPL.Add(new REPLInfo(PlayerREPL.Count, "Yola"));
        //PlayerREPL.Add(new REPLInfo(PlayerREPL.Count, "jim"));
        foreach (string _player in NetworkManager.Instance.Clients.Keys)
        {
            if (_player != GameManager.Instance.PlayerName)
            {
                PlayerREPL.Add(new REPLInfo(PlayerREPL.Count, _player));
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {
        for (int i = 0; i < PlayerREPL.Count; i++)
        {
            createReplGuiWindow(i);
        }
    }

    void createReplGuiWindow(int playerIndex)
    {
        GUI.Window(playerIndex, new Rect(playerIndex * (Screen.width / 4), 2 * (Screen.height / 3), Screen.width / 4, Screen.height / 3), Window, PlayerREPL[playerIndex].Name);
    }

    void Window(int id)
    {
        if (id == 0)
        {
            var evt = Event.current;
            inputString = GUI.TextField(new Rect(0, (Screen.height / 3) - 20, (Screen.width / 4), 20), inputString);
            if (evt.isKey && evt.type == EventType.KeyUp)
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    EvalInputString(inputString);
                    inputString = string.Empty;
                }
                else if (evt.keyCode == KeyCode.UpArrow)
                {
                    inputString = PlayerREPL[id].History.Fetch(inputString, true);
                }
                else if (evt.keyCode == KeyCode.DownArrow)
                {
                    inputString = PlayerREPL[id].History.Fetch(inputString, false);
                }
            }
        }
        else
        {
            GUI.TextField(new Rect(0, (Screen.height / 3) - 20, (Screen.width / 4), 20), "");
        }

        // Display history.
        Rect scrollRect = new Rect(0, 17, (Screen.width / 4), (Screen.height / 3) - 37);
        Rect innerRect = new Rect(0, 0, (Screen.width / 4) - 17, (Screen.height / 3) - 40);

        guiContent.text = PlayerREPL[id].ToString();
        var calcHeight = GUI.skin.textArea.CalcHeight(guiContent, (Screen.width / 4) - 15);

        innerRect.height = calcHeight < scrollRect.height ? scrollRect.height : calcHeight;

        PlayerREPL[id].SetScrollPos(GUI.BeginScrollView(scrollRect, PlayerREPL[id].ScrollPos, innerRect, false, true));
        GUI.TextArea(innerRect, guiContent.text);
        GUI.EndScrollView();
    }

    public void EvalInputString(string _input)
    {
        PlayerREPL[0].AddText(_input);
        uLink.NetworkView.Get(this).RPC("SyncText", uLink.RPCMode.Others, PlayerREPL[0].Name, _input);
        Loom.QueueAsyncTask(LuaManager.ThreadName, () =>
        {
            try
            {
                lua.instance.DoString(_input);
            }
            catch (System.Exception exception)
            {
                PlayerREPL[0].AddText(exception.Message);
            }
        });
    }

    public void SetPlayer(CharacterControl _player)
    {
        player = _player;
        lua = new LuaInstance("Player");
        LuaManager.AddToGlobalInstance(this);
        LuaManager.RegisterLuaFunctions(lua, this);
    }

    [LuaFunc("Player", "Move", "Move the player", "direction")]
    public void Move(int _dir)
    {
        player.Move(_dir);
    }

    [LuaFunc("Player", "Attack", "Attack object or player", "direction")]
    public void Attack(int _dir)
    {
        player.Attack(_dir);
    }

    [LuaFunc("", "print", "Print to player log", "input")]
    public void Log(string _input)
    {
        PlayerREPL[0].AddText(_input);
    }

    [LuaFunc("", "ListFunctions", "Shows -every- function of every package.")]
    public void ListFunctions()
    {
        Loom.QueueOnMainThread(() =>
        {
            Log("Available functions: ");
            Log("");

            foreach (string package in lua.luaFunctions.Keys)
            {
                IDictionaryEnumerator funcs = lua.luaFunctions[package].GetEnumerator();
                while (funcs.MoveNext())
                {
                    Log("= " + ((LuaFuncDescriptor)funcs.Value).GetFuncHeader());
                }
            }
        });
    }

    [RPC]
    public void SyncText(string player, string text)
    {
        int id = 0;
        for (int i = 0; i < PlayerREPL.Count; i++)
        {
            if (PlayerREPL[i].Name == player)
            {
                id = i;
            }
        }
        PlayerREPL[id].AddText(text);
    }
}
