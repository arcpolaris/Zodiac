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
    public class ClientNetworkController : MonoBehaviour, ILiteNetEventListener
    {
        [SerializeField, LabelOverride("Entity Types Map")] private GameTypes gameTypes;

        private LiteNetManager _netManager;
        private ClientEntityManager _entityManager;

        public string key;
        public string remote;

        void Start()
        {
            NetTypes.EnsureRegistered();
            _netManager = new LiteNetManager(this) { AutoRecycle = true };
            _netManager.Start();
        }

        void Update()
        {
            _netManager.PollEvents();
            _entityManager?.Update();
        }


        public void OnPeerConnected(LiteNetPeer peer)
        {
            _entityManager = new ClientEntityManager(
                gameTypes.Build(),
                new LiteNetLibNetPeer(peer, true),
                (byte)PacketType.EntitySystem
            );

            /*_entityManager.GetEntities<BasePlayer>().SubscribeToConstructed(
                player => { },
                callOnExisting: true);*/
        }

        public void OnPeerDisconnected(LiteNetPeer peer, DisconnectInfo disconnectInfo)
        {
            _entityManager.Reset();
            _entityManager = null;
        }

        public void OnNetworkReceive(LiteNetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            switch ((PacketType)reader.PeekByte())
            {
                case PacketType.EntitySystem:
                    _entityManager.Deserialize(reader.GetRemainingBytesSpan());
                    break;
            }
        }
         
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            print("unconnected recv");
        }

        public void OnConnectionRequest(ConnectionRequest request) => request.Reject(); // clients don't accept
        public void OnNetworkLatencyUpdate(LiteNetPeer peer, int latency) { } //TODO: make a display for this
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) => Debug.LogError($"{socketError} {{{endPoint}}}");
    }
}
