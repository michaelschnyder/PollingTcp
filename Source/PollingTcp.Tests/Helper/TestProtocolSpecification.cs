using System;
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

        public int MaxClientIdValue { get { return 746438; } }

        public TimeSpan KeepAliveClientInterval { get; set; }
        public TimeSpan KeepAliveServerInterval { get; set; }

        public TestProtocolSpecification()
        {
            this.MaxClientSequenceValue = 10;
            this.MaxServerSequenceValue = 10;

            this.KeepAliveClientInterval = TimeSpan.FromSeconds(1);
            this.KeepAliveServerInterval = TimeSpan.FromSeconds(1);
        }
    }
}