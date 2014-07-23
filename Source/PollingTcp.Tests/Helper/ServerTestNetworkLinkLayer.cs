using System.Collections.Generic;

namespace PollingTcp.Tests.Helper
{
    class ServerTestNetworkLinkLayer : TestNetworkLinkLayer, IServerNetworkLinkLayer
    {
        public override List<byte[]> ReceivedByteses
        {
            get { return new List<byte[]>(); }
        }
    }
}