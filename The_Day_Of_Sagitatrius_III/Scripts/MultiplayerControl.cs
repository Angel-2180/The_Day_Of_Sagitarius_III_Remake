using Godot;
using Godot.Collections;
using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

public partial class MultiplayerControl : Control
{
    public static MultiplayerControl instance;

    [Signal]
    public delegate void PlayerConnectedEventHandler(int peer_id, Dictionary<string, string> player_info);

    [Signal]
    public delegate void PlayerDisconnectedEventHandler(long peer_id);

    [Signal]
    public delegate void ServerDisconnectedEventHandler();


    private const int MAX_CONNECTIONS = 8;
    private const int PORT = 7000;
    private const string DEFAULT_SERVER_IP = "127.0.0.1";


    private ENetMultiplayerPeer _peer = new();

    private Upnp _upnp = new();



    private int _loadedPlayer = 0;

    #region Nodes
    [Export]
    private LineEdit _pseudoInput;

    [Export]
    private LineEdit _addressInput;

    [Export]
    private Control _clientHUD;

    [Export]
    private Control _serverButton;

    [Export]
    private Control _ClientsInfos;

    [Export]
    private Control _ConnectionsInfos;


    // [Export]
    // private OptionButton _teamOptions;

    [Export]
    private Label _playerCount;

    [Export]
    private Label _serverAddress;

    #endregion

    [Export]
    private PackedScene _character;

	private Node2D _world;

    public override void _Ready()
    {
        GameManager.Instance.PlayerInfos["team"] = "0";
        GameManager.Instance.PlayerInfos["pseudo"] = "Monkey";

        _pseudoInput.TextChanged += _ => GameManager.Instance.PlayerInfos["pseudo"] =_pseudoInput.Text;

        // _teamOptions.ItemSelected += _ => GameManager.Instance.PlayerInfos["team"] = (_teamOptions.Selected - 1).ToString();

        instance = this;

        _serverButton.Visible = false;
        _ClientsInfos.Visible = false;
        _ConnectionsInfos.Visible = false;
        _clientHUD.Visible = true;
        
        Multiplayer.PeerConnected += OnPlayerConnected;
        Multiplayer.PeerDisconnected += OnPlayerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedOK;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;

        PlayerDisconnected += OnPlayerDisconnected;
    }

    private void OnHostPressed()
    {
        // if (_teamOptions.Selected != 0)
        if (_pseudoInput.Text != "")
		    CreateServer();
    }

    private void OnJoinPressed()
    {
        // if (_teamOptions.Selected != 0)
        if (_pseudoInput.Text != "")
        {
            JoinServer(_addressInput.Text);

        }

		   
    }

    private void StartGame()
    {
		Rpc("LoadGame", "res://Scenes/Main.tscn");

        if (Multiplayer.IsServer())
        {
            _serverButton.Visible = false;
            
            foreach(var(key, value) in GameManager.Instance.PlayerIDs)
            {
				Rpc("AddPlayer", key, value);
            }
        }
       
    }

	[Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void LoadGame(string scenePath)
	{
		Node2D scene = ResourceLoader.Load<PackedScene>(scenePath).Instantiate() as Node2D;
		_world = scene;
		GetTree().Root.AddChild(scene);
		Hide();
	}

    private Error? CreateServer()
    {
        Error error = _peer.CreateServer(PORT, MAX_CONNECTIONS);

        if (error != Error.Ok)
            return error;

        Multiplayer.MultiplayerPeer = _peer;

        GameManager.Instance.PlayerIDs[1] = GameManager.Instance.PlayerInfos;
        EmitSignal(SignalName.PlayerConnected, 1, GameManager.Instance.PlayerInfos);

        _clientHUD.Visible = false;
        _serverButton.Visible = true;
        _ConnectionsInfos.Visible = true;
        _playerCount.Text = GameManager.Instance.PlayerIDs.Count + " / " + MAX_CONNECTIONS + " players";

        SetupUPNP();

        return null;
    }

    private Error? JoinServer(string address )
    {
        if (address  == "")
            address  = DEFAULT_SERVER_IP;
        Error error = _peer.CreateClient(address , PORT);
        if (error != Error.Ok)
        {
            GetTree().ReloadCurrentScene();
            return error;
        }
        
        Multiplayer.MultiplayerPeer = _peer;
        _clientHUD.Visible = false;
        _ClientsInfos.Visible = true;
        _ConnectionsInfos.Visible = true;
        return null;
    }

    private void OnPlayerConnected(long id)
    {
        RpcId(id, "RegisterPlayer", GameManager.Instance.PlayerInfos);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RegisterPlayer(Dictionary<string, string> newPlayerInfo)
    {
        int newPlayerID = Multiplayer.GetRemoteSenderId();

        GameManager.Instance.PlayerIDs[newPlayerID] = newPlayerInfo;
        _playerCount.Text = GameManager.Instance.PlayerIDs.Count + " / " + MAX_CONNECTIONS + " players";
        if (Multiplayer.IsServer())
        {
            _serverAddress.Text = "Server Address : " + _upnp.GetGateway().QueryExternalAddress() + " : " + PORT;
        }
        else
        {
            var address = _addressInput.Text == "" ? DEFAULT_SERVER_IP : _addressInput.Text;
            _serverAddress.Text = "Server Address : " + address + " : " + PORT;
        }

        EmitSignal(SignalName.PlayerConnected, newPlayerID, newPlayerInfo);
    }

    private void RemoveMultiplayerPeer()
    {
        Multiplayer.MultiplayerPeer = null;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void UpdatePlayerCount()
    {
        _playerCount.Text = GameManager.Instance.PlayerIDs.Count + " / " + MAX_CONNECTIONS + " players";
    }

    private void OnPlayerDisconnected(long id)
    {
        GameManager.Instance.PlayerIDs.Remove((int)id);
        var player = _world.GetNodeOrNull<Player>(id.ToString());
        player?.QueueFree();
        Rpc(nameof(UpdatePlayerCount));
        Multiplayer.MultiplayerPeer = null;
    }



    private void OnConnectedOK()
    {
        int peerId = Multiplayer.GetUniqueId();
        GameManager.Instance.PlayerIDs.Add(peerId, GameManager.Instance.PlayerInfos);
        EmitSignal(SignalName.PlayerConnected, peerId, GameManager.Instance.PlayerInfos);
    }

    private void OnConnectionFailed()
    {
        Multiplayer.MultiplayerPeer = null;
        GetTree().ReloadCurrentScene();
    }

    public void PlayerLoaded()
    {
        if (Multiplayer.IsServer())
            return;

        _loadedPlayer++;

        if (_loadedPlayer < GameManager.Instance.PlayerIDs.Count)
            return;
        

        GD.Print("Start Game");

        _loadedPlayer = 0;
    }

	int _spawnIndex = 0;

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void AddPlayer(long id, Dictionary<string, string> PlayerInfos)
    {
        Player player = _character.Instantiate() as Player;

        player.Name = id.ToString();
		player.PlayerID = (int)id;
	

		_world.AddChild(player);
		foreach (var Spawn in GetTree().GetNodesInGroup("PlayerSpawnPoint"))
		{
			var spawnPoint = (Node2D)Spawn;
			if (spawnPoint.Name == _spawnIndex.ToString())
			{
				GD.Print("SpawnPoint: " + spawnPoint.Name);
				GD.Print("Spawned player at: " + spawnPoint.GlobalPosition);
				player.Position = spawnPoint.GlobalPosition;
			}
		}
		_spawnIndex++;
    }

    private void OnServerDisconnected()
    {
        Multiplayer.MultiplayerPeer = null;
        EmitSignal(SignalName.ServerDisconnected);

        _clientHUD.Visible = true;
        _serverButton.Visible = false;
        GetTree().Root.GetNodeOrNull("Main")?.QueueFree();
        GetTree().ReloadCurrentScene();

    }

	private void SetupUPNP()
	{
		_upnp = new ();
		_upnp.Discover();
        _upnp.AddPortMapping(PORT);
        Debug.Print("Success! Join Address : " + _upnp.GetGateway().QueryExternalAddress() + " : " + PORT);
        _serverAddress.Text = "Server Address : " + _upnp.GetGateway().QueryExternalAddress() + " : " + PORT;

	}

    private void ReturnToMenu()
    {
        EmitSignal(SignalName.PlayerDisconnected, Multiplayer.GetUniqueId());

    }



}
