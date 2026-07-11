// NetworkController.cs
// Minimal LiteEntitySystem (LES) + LiteNetLib demo.
//
// What it does: when Press(key) is called, every connected peer prints WHO pressed it.
// Runtime role: call StartHost(...) to be the listen-server, or JoinAsClient(...) to be a client.
// Drive these from your in-game console; every command takes its values as parameters.
//
// Why it's shaped this way (important LES detail):
//   - A ControllerLogic is [EntityFlags(EntityFlags.OnlyForOwner)] -> it replicates ONLY to its owner.
//     So a SendToAll RPC fired from a controller would never reach the other players.
//   - Therefore the broadcast lives on a GameManager : SingletonEntityLogic, which replicates to
//     everyone. The server fires one RPC (SendToAll | ExecuteOnServer) and everybody prints.
//   - Clients announce a press over the reliable "client request" channel (one-shot, ordered).
//     The server identifies the sender from the controller's owner and broadcasts. The host, being
//     authoritative, broadcasts directly.
//
// Verified against LiteEntitySystem (LiteNetLib 2.x transport) and LiteNetLib 2.1.x.

using IngameDebugConsole;
using LiteEntitySystem;
using LiteEntitySystem.Transport;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Net;
using UnityEngine;

namespace Zodiac.Net{ 
// Every networked entity type gets an id here. Host and client must register the same set.
public enum GameEntities : ushort
{
    GameManager,
    PlayerController,
}

public class NetworkController : MonoBehaviour
{
    private const byte HeaderByte = 1;  // LES packet header; must match on host + client
    private const byte Tickrate = 30; // server logic ticks per second (client syncs to this)
    private const byte HostId = 0;  // == EntityManager.ServerPlayerId; the host plays as id 0

    private EntityTypesMap _typesMap;
    private EventBasedNetListener _listener;
    private NetManager _netManager;

    private ServerEntityManager _server;          // non-null while hosting
    private ClientEntityManager _client;          // non-null while a client
    private GameManager _hostGameManager; // server-side singleton ref (host broadcasts through it)
    private PlayerController _localController; // cached client-side controller

    // Per-session settings, set by whichever start command was called (not read from inspector fields).
    private string _key;
    private bool _useNatPunch;
    private string _facilitatorHost;
    private int _facilitatorPort;

    // NAT punch objects (only created when a facilitator command is used)
    private NatPunchModule _natPunch;
    private EventBasedNatPunchListener _punchListener;

    private bool IsHost => _server != null;
    private bool IsClient => _client != null;

    // ---------------------------------------------------------------- setup
    private void Awake()
    {
        Application.runInBackground = true;
        // Register all entity types once. The same map is handed to whichever manager we create.
        _typesMap = new EntityTypesMap<GameEntities>()
            .Register(GameEntities.GameManager, e => new GameManager(e))
            .Register(GameEntities.PlayerController, e => new PlayerController(e));

        DebugLogConsole.AddCommand<int, string>("host", "", StartHost, "port", "key");
        DebugLogConsole.AddCommand<string, int, string>("join", "", JoinAsClient, "ip", "port", "key");
        DebugLogConsole.AddCommand("disconnect", "", Disconnect);
    }

    // ---------------------------------------------------------------- host / join / leave
    // Direct listen server. Reachable only if this machine is (public IP, port-forward, or open NAT).
    public void StartHost(int port, string key)
    {
        if (_netManager != null) { Debug.LogWarning("[net] already running"); return; }

        _key = key; _useNatPunch = false;
        _netManager = new NetManager(BuildListener());
        _netManager.Start(port);

        _server = new ServerEntityManager(_typesMap, HeaderByte, Tickrate, ServerSendRate.EqualToFPS);
        _hostGameManager = _server.AddSingleton<GameManager>(); // shared entity that carries the broadcast RPC
        Debug.Log($"[net] hosting on :{port}");
    }

    // Listen server behind a strict NAT: registers with the facilitator so a client can be paired to us.
    public void StartHostViaFacilitator(int port, string key, string facilitatorHost, int facilitatorPort)
    {
        if (_netManager != null) { Debug.LogWarning("[net] already running"); return; }

        _key = key; _useNatPunch = true; _facilitatorHost = facilitatorHost; _facilitatorPort = facilitatorPort;
        _netManager = new NetManager(BuildListener());
        EnableNatPunch();
        _netManager.Start(port);

        _server = new ServerEntityManager(_typesMap, HeaderByte, Tickrate, ServerSendRate.EqualToFPS);
        _hostGameManager = _server.AddSingleton<GameManager>();
        _natPunch.SendNatIntroduceRequest(facilitatorHost, facilitatorPort, key); // announce presence for pairing
        Debug.Log($"[net] hosting on :{port} (NAT punch via {facilitatorHost}:{facilitatorPort})");
    }

    // Direct connect to a reachable host.
    public void JoinAsClient(string hostIp, int port, string key)
    {
        if (_netManager != null) { Debug.LogWarning("[net] already running"); return; }

        _key = key; _useNatPunch = false;
        _netManager = new NetManager(BuildListener());
        _netManager.Start(); // ephemeral local port
        _netManager.Connect(hostIp, port, key);
        Debug.Log($"[net] connecting to {hostIp}:{port}");
    }

    // Connect via facilitator (no host IP needed; the facilitator introduces us, then we dial out).
    public void JoinViaFacilitator(string key, string facilitatorHost, int facilitatorPort)
    {
        if (_netManager != null) { Debug.LogWarning("[net] already running"); return; }

        _key = key; _useNatPunch = true; _facilitatorHost = facilitatorHost; _facilitatorPort = facilitatorPort;
        _netManager = new NetManager(BuildListener());
        EnableNatPunch();
        _netManager.Start();
        _natPunch.SendNatIntroduceRequest(facilitatorHost, facilitatorPort, key); // Connect() on NatIntroductionSuccess
        Debug.Log($"[net] connecting via facilitator {facilitatorHost}:{facilitatorPort}");
    }

    public void Disconnect()
    {
        _netManager?.DisconnectAll();
        _netManager?.Stop();
        _netManager = null;
        _server = null;
        _client = null;
        _hostGameManager = null;
        _localController = null;
        _natPunch = null;
        _useNatPunch = false;
        Debug.Log("[net] stopped");
    }

    private void OnDestroy() => Disconnect();
    private void OnApplicationQuit() => Disconnect();

    // ---------------------------------------------------------------- the "print who pressed" command
    public void Press(KeyCode key)
    {
        if (DebugLogManager.Instance.IsLogWindowVisible) return;

        int keyCode = (int)key;
        if (IsHost)
        {
            // Authoritative: broadcast straight away (host also prints, via ExecuteOnServer).
            _hostGameManager?.Announce(HostId, keyCode);
        }
        else if (IsClient && TryGetLocalController(out var controller))
        {
            // Reliable request -> server identifies us and broadcasts to everyone.
            controller.SendKeyPress(keyCode);
        }
    }

    // ---------------------------------------------------------------- main loop
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X)) Press(KeyCode.X);

        _netManager?.PollEvents();
        if (_useNatPunch) _natPunch?.PollEvents();

        _server?.Update(); // EntityManager.Update() self-paces logic ticks via an internal clock
        _client?.Update();
    }

    // The local player's controller is replicated to us shortly after we join; find and cache it.
    private bool TryGetLocalController(out PlayerController controller)
    {
        if (_localController != null) { controller = _localController; return true; }

        if (_client != null)
            foreach (var c in _client.GetControllers<PlayerController>())
                if (c.IsLocalControlled) { _localController = c; controller = c; return true; }

        controller = null;
        return false;
    }

    // ---------------------------------------------------------------- networking callbacks
    private EventBasedNetListener BuildListener()
    {
        _listener = new EventBasedNetListener();

        // Host side: accept an incoming connection if the key matches.
        _listener.ConnectionRequestEvent += request => request.AcceptIfKey(_key);

        _listener.PeerConnectedEvent += peer =>
        {
            var wrapped = new LiteNetLibNetPeer(peer, assignToTag: true); // bridge LiteNetLib <-> LES
            if (IsHost)
            {
                // A remote player joined -> register them and spawn their controller.
                var player = _server.AddPlayer(wrapped);
                _server.AddController<PlayerController>(player);
                Debug.Log($"[net] player {player.Id} joined");
            }
            else
            {
                // This is our link to the host -> build the client manager around it.
                _client = new ClientEntityManager(_typesMap, wrapped, HeaderByte);
                Debug.Log("[net] connected to host");
            }
        };

        _listener.PeerDisconnectedEvent += (peer, info) =>
        {
            if (IsHost && peer.Tag is LiteNetLibNetPeer wrapped)
            {
                _server.RemovePlayer(wrapped); // also destroys their controller (null-safe if half-joined)
                Debug.Log("[net] player left");
            }
            else if (IsClient)
            {
                _client = null;
                _localController = null;
                Debug.Log("[net] disconnected from host");
            }
        };

        _listener.NetworkReceiveEvent += (peer, reader, channel, method) =>
        {
            if (peer.Tag is LiteNetLibNetPeer wrapped)
            {
                var data = reader.GetRemainingBytesSpan();
                if (IsHost) _server.Deserialize(wrapped, data);
                else _client?.Deserialize(data);
            }
            reader.Recycle();
        };

        return _listener;
    }

    private void EnableNatPunch()
    {
        _netManager.NatPunchEnabled = true;
        _natPunch = _netManager.NatPunchModule;
        _punchListener = new EventBasedNatPunchListener();

        // Fires once the facilitator has introduced us to the other side. The client dials out;
        // the host just listens (the introduction already opened its NAT for the incoming packet).
        _punchListener.NatIntroductionSuccess += (targetEndPoint, type, token) =>
        {
            if (!IsHost) _netManager.Connect(targetEndPoint, _key);
        };

        _natPunch.Init(_punchListener);
    }
}

// ===================================================================== networked entities

// Shared, replicated-to-everyone entity. Owns the broadcast RPC so all players receive it.
public class GameManager : SingletonEntityLogic
{
    private static RemoteCall<KeyPressEvent> _announceRpc;

    public GameManager(EntityParams entityParams) : base(entityParams) { }

    protected override void RegisterRPC(ref RPCRegistrator r)
    {
        base.RegisterRPC(ref r);
        // SendToAll       -> every client runs OnAnnounce.
        // ExecuteOnServer -> the server runs it locally too, so a listen-server/host also prints.
        r.CreateRPCAction(this, OnAnnounce, ref _announceRpc,
            ExecuteFlags.SendToAll | ExecuteFlags.ExecuteOnServer);
    }

    // Server-only: trigger the broadcast.
    public void Announce(byte senderId, int keyCode)
    {
        if (!IsServer) return;
        ExecuteRPC(_announceRpc, new KeyPressEvent { SenderId = senderId, KeyCode = keyCode });
    }

    // Runs on the host and on every client. Swap Debug.Log for your in-game console sink if needed.
    private void OnAnnounce(KeyPressEvent e)
    {
        string who = e.SenderId == 0 ? "Host" : $"Player {e.SenderId}";
        Debug.Log($"[keypress] {who} pressed {(KeyCode)e.KeyCode}");
    }
}

// One per player, owned by that player (controllers replicate only to their owner).
// We don't use continuous input here (NoInput), only the reliable request channel.
public class PlayerController : HumanControllerLogic<NoInput>
{
    public PlayerController(EntityParams entityParams) : base(entityParams) { }

    protected override void OnConstructed()
    {
        base.OnConstructed();
        // Server handles incoming key-press requests from this controller's owning client.
        if (IsServer)
            SubscribeToClientRequestStruct<KeyPressRequest>(OnKeyPressRequest);
    }

    // Client -> server, reliable + ordered. Public so the NetworkController can call it.
    public void SendKeyPress(int keyCode)
    {
        if (!IsClient) return;
        SendRequestStruct(new KeyPressRequest { KeyCode = keyCode });
    }

    // Runs on the server when the owning client pressed the key.
    private void OnKeyPressRequest(KeyPressRequest req)
    {
        var player = GetAssignedPlayer();
        if (player == null) return; // server-owned controller has no remote player
        EntityManager.GetSingleton<GameManager>().Announce(player.Id, req.KeyCode);
    }
}

// ===================================================================== payloads

// Controller input payload. Empty: this demo carries events over requests, not per-tick input.
public struct NoInput { }

// Reliable client->server request: which key was pressed (sender identity is added server-side).
public struct KeyPressRequest : INetSerializable
{
    public int KeyCode;
    public void Serialize(NetDataWriter w) => w.Put(KeyCode);
    public void Deserialize(NetDataReader r) => KeyCode = r.GetInt();
}

// Server->all broadcast payload (must be unmanaged for an RPC). Carries who + which key.
public struct KeyPressEvent
{
    public byte SenderId;
    public int KeyCode;
}

/*
// ===================================================================== OPTIONAL: NAT punch facilitator
// Run this as a SEPARATE console app on a machine with a PUBLIC IP. It has no game logic: it just
// pairs two peers that sent the same token and introduces them so their NATs open simultaneously.
// Only needed for the *ViaFacilitator commands. Point facilitatorHost/Port at wherever this runs.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using LiteNetLib;

internal static class NatPunchFacilitator
{
    private static void Main()
    {
        var listener = new EventBasedNetListener();
        var nm = new NetManager(listener) { NatPunchEnabled = true, UnconnectedMessagesEnabled = true };
        var punch = new EventBasedNatPunchListener();
        var waiting = new Dictionary<string, (IPEndPoint local, IPEndPoint remote)>();

        punch.NatIntroductionRequest += (localEp, remoteEp, token) =>
        {
            if (waiting.TryGetValue(token, out var first))
            {
                // first peer treated as host, second as client
                nm.NatPunchModule.NatIntroduce(first.local, first.remote, localEp, remoteEp, token);
                waiting.Remove(token);
            }
            else
            {
                waiting[token] = (localEp, remoteEp);
            }
        };

        nm.NatPunchModule.Init(punch);
        nm.Start(50010); // facilitatorPort
        Console.WriteLine("Facilitator listening on :50010");
        while (true) { nm.NatPunchModule.PollEvents(); nm.PollEvents(); Thread.Sleep(10); }
    }
}
*/
}