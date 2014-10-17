using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class UImanager : MonoBehaviour {
    public delegate void Button_Click(GameObject obj);
    public delegate void InputField_Submit(GameObject obj, string text);

    public static UImanager _instance;
    public static UImanager Instance
    {
        get { return _instance; }
    }

    public GameObject buttonPrefab;
    public GameObject MatchSettingsPrefab;
    public Dictionary<string, GameObject> UI = new Dictionary<string, GameObject>();
    public bool isHost = false;

    private List<GameObject> MatchList = new List<GameObject>();
    private Dictionary<string, GameObject> PlayerButtonList = new Dictionary<string, GameObject>();
    public float cooldown = 5f;
    float timer = 0;

    void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this);
    }

	// Use this for initialization
	void Start () {

    }
	
	// Update is called once per frame
	void Update () {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
        if (timer < 0)
        {
            timer = 0;
        }
        if (timer == 0)
        {
            switch (Application.loadedLevelName)
            {
                case "Lobby":
                    Refresh_Click(null);
                    break;

                case "GameLobby":
                    DisplayPlayerList();
                    break;
            }
            timer = cooldown;
        }

	}

    public void ReloadGamesList(uLink.HostData[] hosts)
    {
        if (Application.loadedLevelName == "Lobby")
        {
            DeleteListItems(MatchList);
            RectTransform gamesTextRect = UI["GamesText"].GetComponent<RectTransform>();
            RectTransform Panel = (RectTransform)UI["Panel"].transform;
            int i = 2;
            foreach (uLink.HostData host in hosts)
            {
                if (!UI.ContainsKey(host.gameName))
                {
                    Vector3 postion = new Vector3(gamesTextRect.position.x, gamesTextRect.position.y - (i * 20), gamesTextRect.position.z);
                    GameObject button = (GameObject)Instantiate(buttonPrefab, postion, Quaternion.identity);
                    button.name = host.gameName;
                    button.transform.parent = Panel;
                    button.GetComponentInChildren<Text>().text = string.Format("{0}, {1}/{2}", host.gameName, host.connectedPlayers, NetworkManager.MAX_PLAYERS);
                    UiButtonListen buttonListner = button.GetComponent<UiButtonListen>();
                    buttonListner.CallFunction = "Match_Click";
                    MatchList.Add(button);
                    i++;
                }
                else
                {
                    GameObject button = UI[host.gameName];
                    if (button != null)
                    {
                        button.GetComponentInChildren<Text>().text = string.Format("{0}, {1}/{2}", host.gameName, host.connectedPlayers, NetworkManager.MAX_PLAYERS);
                    }
                }
            }
        }
    }

    public void DisplayPlayerList()
    {
        if (Application.loadedLevelName == "GameLobby")
        {
            RectTransform TextRect = (RectTransform)UI["PlayersText"].transform;
            RectTransform Panel = (RectTransform)UI["Lobby"].transform;
            int i = 2;
            foreach (string player in NetworkManager.Instance.Clients.Keys)
            {
                if (!PlayerButtonList.ContainsKey(player))
                {
                    Vector3 postion = new Vector3(TextRect.position.x, TextRect.position.y - (i * 30), TextRect.position.z);
                    GameObject button = (GameObject)Instantiate(buttonPrefab, postion, Quaternion.identity);
                    button.name = player;
                    button.transform.parent = Panel;
                    button.GetComponentInChildren<Text>().text = player;
                    UiButtonListen buttonListner = button.GetComponent<UiButtonListen>();
                    buttonListner.CallFunction = "PlayerButton_Click";
                    PlayerButtonList.Add(player, button);
                }
                i++;
            }
        }
    }

    public void DeletePlayerListButton(string name)
    {
        if (PlayerButtonList.ContainsKey(name) && Application.loadedLevelName.Equals("GameLobby"))
        {
            Destroy(PlayerButtonList[name]);
            PlayerButtonList.Remove(name);
            DisplayPlayerList();
        }
    }

    public void ListUIcontents()
    {
        string contents = "registered UI elements: \n";
        foreach (string item in UI.Keys)
        {
            contents += item + "\n";
        }
        Debug.Log(contents);
    }

    public void DeleteUIelement(string element)
    {
        if (UI.ContainsKey(element))
        {
            Destroy(UI[element]);
            UI.Remove(element);
        }
    }

    public void SetButtonsInteractable(string parent, bool interactable)
    {
        if (UI.ContainsKey(parent))
        {
            Button[] buttons = UI["parent"].GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                button.interactable = interactable;
            }
        }
    }

    #region callbacks
    #region Login Screen
    public void Login_Click(GameObject obj)
    {
        Text text = UI["userNameField"].GetComponentInChildren<Text>();
        GameManager.Instance.Login(text.text);
    }

    public void UserName_Submit(GameObject obj, string input)
    {
        GameManager.Instance.Login(input);
    }
    #endregion
    #region Lobby
    public void Refresh_Click(GameObject obj)
    {
        cooldown = 5f;
        NetworkManager.Instance.RefreshHostList();
        Debug.Log("Requesting host list.");
    }

    public void Host_Click(GameObject obj)
    {
        isHost = true;
        ChangeMenu("GameLobby");
    }

    public void Match_Click(GameObject obj)
    {
        NetworkManager.Instance.JoinServer(obj.name);
        isHost = false;        
    }
    #endregion
    #region Game Lobby
    public void PlayerButton_Click(GameObject obj)
    {
        // player stats and options eventually.
    }

    public void gameSettings_Click(GameObject obj)
    {
        string gameName = UI["gameNameField"].GetComponentInChildren<Text>().text;
        if (gameName != string.Empty)
        {
            NetworkManager.Instance.StartServer(gameName);
            DeleteUIelement("MatchSettings");
            SetButtonsInteractable("Lobby", true);
            DisplayPlayerList();

        }
    }

    public void Start_Click(GameObject obj)
    {
        NetworkManager.Instance.UnregisterFromLobby();
        ChangeMenu("Game");
    }
    #endregion
    #endregion

    public void ChangeMenu(string menu)
    {
        UI.Clear();
        Application.LoadLevel(menu);
    }

    public static void RegisterItem(GameObject obj)
    {
        if (!Instance.UI.ContainsKey(obj.name))
        {
            Instance.UI.Add(obj.name, obj);
        }
    }

    public static Delegate GetCallback<T>(string callBack)
    {
        // Get the callback function based on the callBack name.
        Type type = typeof(UImanager);
        MethodInfo method = type.GetMethod(callBack);
        if (method != null)
        {
            return Delegate.CreateDelegate(typeof(T), Instance, method, false);
        }
        else
        {
            Debug.LogError("Could not Find UI callback function \"" + callBack + "\".");
        }
        return null;
    }

    private void DeleteListItems(List<GameObject> list)
    {
        foreach (GameObject obj in list)
        {
            Destroy(obj);
        }
        list.Clear();
    }
}
