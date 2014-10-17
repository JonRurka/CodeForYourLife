using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using uLink;

public class NetworkManager : uLink.MonoBehaviour
{
    public enum ConnectionState
    {
        notConnected,
        Server,
        Client,
    }
    public struct Player
    {
        public string Name;
        public uLink.NetworkPlayer NetPlayer;

        public Player(string _name, uLink.NetworkPlayer _netPlayer)
        {
            Name = _name;
            NetPlayer = _netPlayer;
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

    public Dictionary<string ,uLink.HostData> hostList = new Dictionary<string, uLink.HostData>();
    public Dictionary<string, Player> Clients = new Dictionary<string, Player>();
    private Player Server;

    void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(this);
        BitStreamCodec.Add<Player>(Player.ReadPlayer, Player.WritePlayer);
    }

    // Use this for initialization
    void Start () {
        uLink.MasterServer.ipAddress = "127.0.0.1";
        state = ConnectionState.notConnected;
    }
	
	// Update is called once per frame
	void Update () {
	
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
                    UImanager.Instance.DeletePlayerListButton(_player.Name);
                    uLink.NetworkView.Get(this).RPC("CleintRemoveUser_RPC", uLink.RPCMode.Others, _player);
                }
            }
        }
    }

    private void DisconnectedFromServer()
    {
        Clients.Clear();
        hostList.Clear();
        state = ConnectionState.notConnected;
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
        UImanager.Instance.DeletePlayerListButton(_player.Name);
    }
}
