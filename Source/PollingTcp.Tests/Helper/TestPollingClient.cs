using PollingTcp.Client;
using PollingTcp.Frame;
using PollingTcp.Shared;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    internal class TestPollingClient : PollingClient<ClientControlFrame, ClientDataFrame, ServerDataFrame>
    {
        public TestPollingClient(IClientNetworkLinkLayer clientNetworkLinkLayer) : base(clientNetworkLinkLayer, new BinaryClientFrameEncoder(), new BinaryServerFrameEncoder(), 50, 50)
        {

        }

        public TestPollingClient(IClientNetworkLinkLayer clientNetworkLinkLayer, IProtocolSpecification<ClientControlFrame, ClientDataFrame, ServerDataFrame> specification)
            : base(clientNetworkLinkLayer, specification)
        {

        }
    }
}