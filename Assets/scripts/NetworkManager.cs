﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using uLink;

public class NetworkManager : uLink.MonoBehaviour
{
    [System.Serializable]
    public class Player
    {
        public string Name;
        public uLink.NetworkPlayer NetPlayer;
        public GameObject Player_Object;

        public Player(string _name, uLink.NetworkPlayer _netPlayer)
        {
            Name = _name;
            NetPlayer = _netPlayer;
            Player_Object = null;
        }

        public void SetPlayerObject(GameObject obj)
        {
            Player_Object = obj;
        }

        public static void WritePlayer(uLink.BitStream stream, object value, params object[] codecOptions)
        {
            Player player = (Player)value;
            stream.Write<string>(player.Name);
            stream.Write<uLink.NetworkPlayer>(player.NetPlayer);
        }

        public static object ReadPlayer(uLink.BitStream stream, params object[] codecOptions)
        {
            Player player = new Player(stream.Read<string>(), stream.Read<uLink.NetworkPlayer>());
            return player;
        }
    }
    public enum ConnectionState
    {
        NotConnected,
        Server,
        Client,
    }

    public const int MAX_PLAYERS = 4;
    public const int PORT = 25000;
    public const string typeName = "CodeForYourLife";

    public string GameName = string.Empty;

    private static NetworkManager _instance;
    public static NetworkManager Instance
    {
        get { return _instance; }
    }

    public ConnectionState state;

    public uLinkRegisterPrefabs NetworkPrefabs;

    public Dictionary<string ,uLink.HostData> hostList = new Dictionary<string, uLink.HostData>();
    public Dictionary<string, Player> Clients = new Dictionary<string, Player>();
    public List<Player> ClientList;
    private Player Server;

    void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this);
        BitStreamCodec.Add<Player>(Player.ReadPlayer, Player.WritePlayer);
        NetworkPrefabs = GetComponent<uLinkRegisterPrefabs>();
    }

    // Use this for initialization
    void Start () {
        uLink.MasterServer.ipAddress = "127.0.0.1";
        state = ConnectionState.NotConnected;
        ClientList = new List<Player>();
    }
	
	// Update is called once per frame
	void Update () {
        ClientList = new List<Player>(Clients.Values);
	}

    public void StartServer(string gameName)
    {
        uLink.Network.InitializeServer(MAX_PLAYERS, PORT, !uLink.Network.HavePublicAddress());
        uLink.MasterServer.RegisterHost(typeName, gameName);
        GameName = gameName;
    }

    public void JoinServer(string host)
    {
        if (hostList.ContainsKey(host))
        {
            JoinServer(hostList[host]);
        }
        else
            Debug.LogError("Host " + host + " not found in host list.");
    }

    public void JoinServer(uLink.HostData host)
    {
        if (host.connectedPlayers < 4)
        {
            uLink.Network.Connect(host);
            Debug.Log("Connecting to " + host.gameName);
        }
        else
        {
            Debug.Log(host.gameName + " is full!");
        }
        
    }

    public void UnregisterFromLobby()
    {
        uLink.MasterServer.UnregisterHost();
    }

    public void RefreshHostList()
    {
        uLink.MasterServer.RequestHostList(typeName);
    }

    public void RequestPlayerList()
    {
        uLink.NetworkView.Get(this).RPC("ServerAddNewUser_RPC", uLink.RPCMode.Server, uLink.Network.player, GameManager.Instance.PlayerName);
    }

    public static Player GetPlayer(string _name)
    {
        if (Instance.Clients.ContainsKey(_name))
        {
            return Instance.Clients[_name];
        }
        return default(Player);
    }

    public void DisconnectClient(uLink.NetworkPlayer player, string reason)
    {
        uLink.NetworkView.Get(this).RPC("CleintDisconnect_RPC", player, reason);
    }

    public void Disconnect()
    {
        Disconnect("Disconnecting");
    }

    public void Disconnect(string reason)
    {
        uLink.NetworkView.Get(this).RPC("ServerDisconnectNotification_RPC", Server.NetPlayer, reason);
        uLink.Network.Disconnect();
    }

    public void StartGame(string _scene)
    {
        uLink.NetworkView.Get(this).RPC("ClientStartGame_RPC", uLink.RPCMode.Others, _scene, VoxelSettings.seed);
    }

    public void CreateMap(string _name)
    {
        uLink.NetworkView.Get(this).RPC("ClientCreateMap_RPC", Clients[_name].NetPlayer, VoxelSettings.seed);
    }

    public void SpawnPlayer(string _name, Vector3 _location)
    {
        uLink.Network.Instantiate(NetworkPrefabs.prefabs[0], _location, Quaternion.identity, 0, _name);
    }

    public void GetMapDataFromServer()
    {
        uLink.NetworkView.Get(this).RPC("ServerGetMapData_RPC", uLink.RPCMode.Server, GameManager.Instance.PlayerName);
    }

    public void AttackPlayer(string _name, float health)
    {
        uLink.NetworkView.Get(this).RPC("ClientAttacked_RPC", Clients[_name].NetPlayer, _name, health);
    }

    void uLink_OnServerInitialized()
    {
        Debug.Log("Server Initializied");
        state = ConnectionState.Server;
        Server = new Player(GameManager.Instance.PlayerName, uLink.Network.player);
        Clients.Add(GameManager.Instance.PlayerName, Server);
    }

    void uLink_OnMasterServerEvent(uLink.MasterServerEvent msEvent)
    {
        if (msEvent == uLink.MasterServerEvent.HostListReceived)
        {
            uLink.HostData[] _hostList = new uLink.HostData[0];
            _hostList = uLink.MasterServer.PollHostList();
            foreach (uLink.HostData _host in _hostList)
            {
                if (!hostList.ContainsKey(_host.gameName))
                {
                    hostList.Add(_host.gameName, _host);
                }
            }
            UImanager.Instance.ReloadGamesList(_hostList);
        }
    }

    void uLink_OnConnectedToServer(System.Net.IPEndPoint server)
    {
        Debug.Log("Server Joined");
        state = ConnectionState.Client;
        UImanager.Instance.ChangeMenu("GameLobby");
    }

    void uLink_OnDisconnectedFromServer(uLink.NetworkDisconnection mode)
    {
        Debug.Log("Disconnected from server: " + mode.ToString());
        DisconnectedFromServer();
    }

    void uLink_OnPlayerConnected(uLink.NetworkPlayer player)
    {
        Debug.Log("Player Connected!");
    }

    void uLink_OnPlayerDisconnected(uLink.NetworkPlayer netPlayer)
    {
        Debug.Log("Player Disconnected!");
        if (uLink.Network.player.isServer)
        {
            foreach (Player _player in new List<Player>(Clients.Values))
            {
                if (netPlayer.Equals(_player.NetPlayer))
                {
                    Clients.Remove(_player.Name);
                    uLink.NetworkView.Get(this).RPC("CleintRemoveUser_RPC", uLink.RPCMode.Others, _player);
                    if (Application.loadedLevelName.Equals("GameLobby"))
                    {
                        UImanager.Instance.DeletePlayerListButton(_player.Name);
                    }
                    else if (Application.loadedLevelName.Equals("Game"))
                    {
                        CodeREPL.Instance.RemovePlayer(_player.Name);
                        Destroy(_player.Player_Object);
                    }
                }
            }
        }
    }

    private void DisconnectedFromServer()
    {
        Clients.Clear();
        hostList.Clear();
        state = ConnectionState.NotConnected;
        Server = default(Player);
        UImanager.Instance.ChangeMenu("Lobby");
    }

    [RPC]
    void ServerAddNewUser_RPC(uLink.NetworkPlayer sender, string userName)
    {
        if (Application.loadedLevelName.Equals("GameLobby"))
        {
            if (!Clients.ContainsKey(userName))
                Clients.Add(userName, new Player(userName, sender));
            foreach (string user in Clients.Keys)
            {
                uLink.NetworkView.Get(this).RPC("ClientAddNewUser_RPC", sender, Clients[user]);
            }
        }
        else
            DisconnectClient(sender, "Match Started, sorry :/");
    }

    [RPC]
    void ServerDisconnectNotification_RPC(string reason)
    {
        Debug.Log("Player disconnecting: " + reason);
    }

    [RPC]
    void ServerGetMapData_RPC(uLink.NetworkPlayer sender, string _name)
    {
        CreateMap(_name);
        SpawnPlayer(_name, new Vector3(Random.Range(0, 10), 1, 0));
    }

    [RPC]
    void ClientAddNewUser_RPC(Player _player)
    {
        if (!Clients.ContainsKey(_player.Name))
            Clients.Add(_player.Name, _player);
        if (_player.NetPlayer.isServer)
        {
            Server = _player;
        }
    }

    [RPC]
    void CleintDisconnect_RPC(string reason)
    {
        Debug.Log("Disconnected from server: " + reason);
        Disconnect(reason);
    }

    [RPC]
    void CleintRemoveUser_RPC(Player _player)
    {
        Clients.Remove(_player.Name);
        if (Application.loadedLevelName.Equals("GameLobby"))
        {
            UImanager.Instance.DeletePlayerListButton(_player.Name);
        }
        else if (Application.loadedLevelName.Equals("Game"))
        {
            CodeREPL.Instance.RemovePlayer(_player.Name);
            Destroy(_player.Player_Object);
        }
    }

    [RPC]
    void ClientStartGame_RPC(string _scene, int _seed)
    {
        DConsole.Log("Menu changed to " + _scene + " by server.");
        VoxelSettings.seed = _seed;
        UImanager.Instance.ChangeMenu(_scene);
    }

    [RPC]
    void ClientCreateMap_RPC(int seed)
    {
        DConsole.Log("Generating map...");
        VoxelSettings.seed = seed;
        GameManager.Instance.InitTerrainWhenLoaded();
    }

    [RPC]
    void ClientSpawnPlayer_RPC(string name, Vector3 location)
    {
        DConsole.Log("Spawning player.");
        uLink.Network.Instantiate(NetworkPrefabs.prefabs[0], location, Quaternion.identity, 0, name);
    }

    [RPC]
    void ClientAttacked_RPC(string _name, float _health)
    {
        CharacterControl play_obj = Clients[_name].Player_Object.GetComponent<CharacterControl>();
        play_obj.AddHealth(_health);
    }
}
