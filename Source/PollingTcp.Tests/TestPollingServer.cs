using PollingTcp.Frame;
using PollingTcp.Server;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    internal class TestPollingServer : PollingServer<ClientControlFrame, ClientDataFrame, ServerDataFrame>
    {
        public TestPollingServer(IServerNetworkLinkLayer networkLinkLayer) : 
            base(networkLinkLayer, new BinaryClientFrameEncoder(), new BinaryServerFrameEncoder(), 50, 50)
        {
        }

        public TestPollingServer(IServerNetworkLinkLayer networkLinkLayer, TestProtocolSpecification specification)
            : base(networkLinkLayer, specification)
        {

        }
    }
}