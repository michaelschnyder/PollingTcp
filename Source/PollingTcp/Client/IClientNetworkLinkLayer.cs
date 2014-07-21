using PollingTcp.Common;

namespace PollingTcp.Client
{
    public interface IClientNetworkLinkLayer : INetworkLinkLayer
    {
        void StartPolling();

        void StopPolling();
    }
}