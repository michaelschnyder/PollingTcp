using System;

namespace PollingTcp.Common
{
    public interface INetworkLinkLayer
    {
        int MaxWindowSize { get; }
    }
}