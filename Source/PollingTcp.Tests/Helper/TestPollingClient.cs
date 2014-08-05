using PollingTcp.Client;
using PollingTcp.Frame;
using PollingTcp.Shared;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    internal class TestPollingClient : PollingClient<ClientControlFrame, ClientDataFrame, ServerDataFrame>
    {
        public TestPollingClient(IClientNetworkLinkLayer clientNetworkLinkLayer) : base(clientNetworkLinkLayer, new TestProtocolSpecification() { ClientEncoder = new BinaryClientFrameEncoder(), ServerEncoder = new BinaryServerFrameEncoder()})
        {

        }

        public TestPollingClient(IClientNetworkLinkLayer clientNetworkLinkLayer, IProtocolSpecification<ClientControlFrame, ClientDataFrame, ServerDataFrame> specification)
            : base(clientNetworkLinkLayer, specification)
        {

        }
    }
}