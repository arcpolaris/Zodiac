using System;
using System.Net;
using UnityEngine;

namespace Zodiac.Net
{
    [Serializable]
    public struct SerializableIPEndPoint
    {
        [SerializeField]
        private string ip;
        [SerializeField, Range(IPEndPoint.MinPort, IPEndPoint.MaxPort)]
        private int port;

        public SerializableIPEndPoint(IPEndPoint ep)
        {
            ip = ep.Address.ToString();
            port = ep.Port;
        }

        public SerializableIPEndPoint(string ip, int port){
            this.ip = ip;
            this.port = port;
        }

        public static implicit operator IPEndPoint(SerializableIPEndPoint sep) => new(IPAddress.Parse(sep.ip), sep.port);
        public static implicit operator SerializableIPEndPoint(IPEndPoint ep) => new(ep);
    }
}