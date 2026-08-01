namespace Zodiac.Net
{
    public static class NetConfig
    {
        public const byte PACKET_HEADER = 0x69;
        public const int MAX_PLAYERS = 4;
        internal const ushort PORT = 5067;
        public const int TICKRATE = 30;
    }

    public enum PacketType : byte {
        EntitySystem = 0x69,
        Serialized,
    }
}