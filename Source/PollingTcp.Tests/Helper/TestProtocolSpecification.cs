using PollingTcp.Client;
using PollingTcp.Common;
using PollingTcp.Frame;
using PollingTcp.Shared;

namespace PollingTcp.Tests.Helper
{
    class TestProtocolSpecification : IProtocolSpecification<ClientControlFrame, ClientDataFrame, ServerDataFrame>
    {
        public IClientFrameEncoder<ClientControlFrame, ClientDataFrame> ClientEncoder { get; set; }
        public FrameEncoder<ServerDataFrame> ServerEncoder { get; set; }
        public int MaxClientSequenceValue { get; private set; }
        public int MaxServerSequenceValue { get; private set; }

        public TestProtocolSpecification()
        {
            this.MaxClientSequenceValue = 10;
            this.MaxServerSequenceValue = 10;
        }
    }
}