using PollingTcp.Frame;
using PollingTcp.Server;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    internal class TestPollingServer : PollingServer<ClientControlFrame, ClientDataFrame, ServerDataFrame>
    {
        public TestPollingServer(IServerNetworkLinkLayer networkLinkLayer) : 
            base(networkLinkLayer, new TestProtocolSpecification() { ClientEncoder = new BinaryClientFrameEncoder(), ServerEncoder = new BinaryServerFrameEncoder()})
        {
        }

        public TestPollingServer(IServerNetworkLinkLayer networkLinkLayer, TestProtocolSpecification specification)
            : base(networkLinkLayer, specification)
        {

        }
    }
}