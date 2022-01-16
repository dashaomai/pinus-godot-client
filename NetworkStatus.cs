namespace PinusClient
{
    public enum NetworkStatus
    {
        /// <summary>默认/未知状态</summary>
        Unknown = 0,
        /// <summary>关闭状态</summary>
        Closed,
        /// <summary>正在连接状态</summary>
        Connecting,
        /// <summary>已连接状态</summary>
        Connected,
        /// <summary>正在握手状态</summary>
        Handshaking,
        /// <summary>就绪状态</summary>
        Ready,
    }
}