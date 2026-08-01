using LiteEntitySystem;
using LiteEntitySystem.Transport;
using LiteNetLib;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using Zodiac.Util;

namespace Zodiac.Net
{
    public class HostNetworkController : MonoBehaviour, ILiteNetEventListener
    {
        [SerializeField, LabelOverride("Entity Types Map")] private GameTypes gameTypes;

        private LiteNetManager _netManager;
        private ServerEntityManager _entityManager;

        public string Key;

        void Start()
        {
            NetTypes.EnsureRegistered();

            _entityManager = new ServerEntityManager(
                gameTypes.Build(),
                (byte)PacketType.EntitySystem,
                NetConfig.TICKRATE,
                ServerSendRate.EqualToFPS);

            _netManager = new LiteNetManager(this) { AutoRecycle = true };
            _netManager.Start(NetConfig.PORT);
        }

        void Update()
        {
            _netManager.PollEvents();
            _entityManager.Update();
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            if (_netManager.ConnectedPeersCount + 1 > NetConfig.MAX_PLAYERS) request.Reject();
            else if (string.IsNullOrWhiteSpace(Key)) request.Accept();
            else request.AcceptIfKey(Key);
        }

        public void OnPeerConnected(LiteNetPeer peer)
        {
            NetPlayer player = _entityManager.AddPlayer(new LiteNetLibNetPeer(peer, true));
            var pawn = _entityManager.AddEntity<BasePlayer>();
            _entityManager.AddController<BasePlayerController>(player, pawn);
        }

        public void OnPeerDisconnected(LiteNetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (peer.Tag != null)
                _entityManager.RemovePlayer((LiteNetLibNetPeer)peer.Tag);
        }

        public void OnNetworkReceive(LiteNetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            switch ((PacketType)reader.PeekByte()){
                case PacketType.EntitySystem:
                    _entityManager.Deserialize((LiteNetLibNetPeer)peer.Tag, reader.GetRemainingBytesSpan());
                    break;
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) => print("unconnected recv");
        public void OnNetworkLatencyUpdate(LiteNetPeer peer, int latency) { } //TODO: make a display for this
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => Debug.LogError($"{socketError} {{{endPoint}}}");
    }
}
