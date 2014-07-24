using System;
using PollingTcp.Common;

namespace PollingTcp.Tests.Helper
{
    public interface IServerNetworkLinkLayer : INetworkLinkLayer
    {
        Func<byte[], byte[]> PollHandler { get; set; }
    }
}