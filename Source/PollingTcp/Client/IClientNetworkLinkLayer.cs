using System;
using PollingTcp.Common;

namespace PollingTcp.Client
{
    public interface IClientNetworkLinkLayer : INetworkLinkLayer
    {
        event EventHandler<DataReceivedEventArgs> DataReceived;

        void StartPolling();

        void StopPolling();
    }
}