using System;
using PollingTcp.Client;
using PollingTcp.Common;
using PollingTcp.Frame;

namespace PollingTcp.Shared
{
    internal class DefaultProtocolSpecification<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType> : IProtocolSpecification<TClientControlFrameType, TClientDataFrameType, TServerDataFrameType>
        where TClientControlFrameType : ClientControlFrame, new()
        where TClientDataFrameType : ClientDataFrame, new()
        where TServerDataFrameType : ServerDataFrame, new()
    {
        public IClientFrameEncoder<TClientControlFrameType, TClientDataFrameType> ClientEncoder { get; set; }
        public FrameEncoder<TServerDataFrameType> ServerEncoder { get; set; }
        public int MaxClientSequenceValue { get; set; }
        public int MaxServerSequenceValue { get; set; }
        public int MaxClientIdValue { get; private set; }

        public TimeSpan KeepAliveClientInterval { get; set; }
        public TimeSpan KeepAliveServerInterval { get; set; }

        public DefaultProtocolSpecification()
        {
            this.MaxClientSequenceValue = 512;
            this.MaxServerSequenceValue = 512;

            this.KeepAliveClientInterval = TimeSpan.FromSeconds(1);
            this.KeepAliveClientInterval = TimeSpan.FromSeconds(1);

            this.MaxClientIdValue = int.MaxValue;
        }
    }
}