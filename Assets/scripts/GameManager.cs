using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {
    public string PlayerName = string.Empty;
    public UImanager UiManager;

    private string AdminModePassword = "Joymo";

    private static GameManager _instance;
    public static GameManager Instance
    {
        get { return _instance; }
    }

    void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this);
    }

	// Use this for initialization
	void Start () {
        UiManager = UImanager.Instance;
        DConsole.RegisterCommand(new DConsole.CommandDescription("Game", "AdminMode", "<-on/-off> <password>", "Enable admin mode.", "Enable admin mode. '-on' for enable, '-off' for disable.", EnableAdminCmd));
    }
	
	// Update is called once per frame
	void Update () {
	    
	}

    void OnLevelWasLoaded(int level)
    {
        StartCoroutine(LevelLoaded());
    }

    IEnumerator LevelLoaded()
    {
        yield return new WaitForEndOfFrame();
        if (Application.loadedLevelName == "Lobby")
        {
            UiManager.Refresh_Click(null);
        }
        else if (Application.loadedLevelName == "GameLobby")
        {
            if (!UiManager.isHost)
            {
                UiManager.DeleteUIelement("MatchSettings");
                NetworkManager.Instance.RequestPlayerList();
            }
            else
            {
                UiManager.SetButtonsInteractable("Lobby", false);
            }
            UiManager.cooldown = 1f;

        }
        else if (Application.loadedLevelName == "Game")
        {

        }
    }

    public void Login(string userName)
    {
        PlayerName = userName;
        Debug.Log("logged in as " + userName);
        UImanager.Instance.ChangeMenu("Lobby");
    }

    public void SetAdminMode(bool enable)
    {
        if (enable)
        {
            DConsole.RegisterCommand(new DConsole.CommandDescription("Game", "REPL", "<C# code><[-on]", "Type code into the REPL.", "Type code into the REPL. use '-on' in place of code for persistant session. This REPL has access to everything.", REPLcmd));
        }
    }

    public string REPL(string input)
    {
        return "";
    }

    public object EnableAdminCmd(params string[] args)
    {
        string result = string.Empty;
        if (args.Length == 3)
        {
            if (args[2].Equals(AdminModePassword))
            {
                if (args[1].ToLower().Equals("-on"))
                {
                    SetAdminMode(true);
                    result = "Admin mode enabled";
                }
                else if (args[1].ToLower().Equals("-off"))
                {
                    SetAdminMode(false);
                    result = "Admin mode disabled";
                }
                else
                {
                    result = "Specify '-on' or '-off' please.";
                }
            }
            else
            {
                result = "That's not the password :3";
            }
        }
        else
        {
            result = "I think you're missing something...";
        }
        return result;
    }

    public object REPLcmd(params string[] args)
    {
        string result = string.Empty;
        if (args.Length == 2)
        {
            if (args[2].Equals("-on"))
            {
                result = "Not yet implemented";
            }
            else
            {
                string input = string.Empty;
                for (int i = 1; i < args.Length; i++)
                {
                    input += args[i] + " ";
                }
                result = REPL(input);
            }
        }
        else
        {
            result = "I think you're missing something...";
        }
        return result;
    }
}
