using System;

namespace PollingTcp.Common
{
    public interface INetworkLinkLayer
    {
        int MaxWindowSize { get; }
        void Send(byte[] bytesToSend);
    }
}