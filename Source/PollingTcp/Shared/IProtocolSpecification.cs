using PollingTcp.Client;
using PollingTcp.Common;
using PollingTcp.Frame;

namespace PollingTcp.Shared
{
    public interface IProtocolSpecification<in TClientControlFrameType, in TClientDataFrameType, TServerDataFrameType> : IProtocolRuntimeSpefication
        where TClientControlFrameType : ClientControlFrame, new()
        where TClientDataFrameType : ClientDataFrame, new()
        where TServerDataFrameType : ServerDataFrame, new()
    {
        IClientFrameEncoder<TClientControlFrameType, TClientDataFrameType> ClientEncoder { get; }
        FrameEncoder<TServerDataFrameType> ServerEncoder { get; }
    
    }
}