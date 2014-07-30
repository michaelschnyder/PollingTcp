using System;
using PollingTcp.Common;

namespace PollingTcp.Server
{
    public interface IServerNetworkLinkLayer : INetworkLinkLayer
    {
        Func<byte[], byte[]> PollHandler { get; set; }
    }
}