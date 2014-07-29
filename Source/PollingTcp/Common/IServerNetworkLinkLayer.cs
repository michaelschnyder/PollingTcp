using System;

namespace PollingTcp.Common
{
    public interface IServerNetworkLinkLayer : INetworkLinkLayer
    {
        Func<byte[], byte[]> PollHandler { get; set; }
    }
}