using System;
using PollingTcp.Common;

namespace PollingTcp.Client
{
    public interface IClientNetworkLinkLayer : INetworkLinkLayer
    {
        void Send(byte[] bytesToSend);

        event EventHandler<DataReceivedEventArgs> DataReceived;
    }
}