﻿using System;
using PollingTcp.Client;
using PollingTcp.Common;
using PollingTcp.Frame;

namespace PollingTcp.Shared
{
    public interface IProtocolSpecification<in TClientControlFrameType, in TClientDataFrameType, TServerDataFrameType>
        where TClientControlFrameType : ClientControlFrame, new()
        where TClientDataFrameType : ClientDataFrame, new()
        where TServerDataFrameType : ServerDataFrame, new()
{
    IClientFrameEncoder<TClientControlFrameType, TClientDataFrameType> ClientEncoder { get; }
    FrameEncoder<TServerDataFrameType> ServerEncoder { get; }
    int MaxClientSequenceValue { get; }
    int MaxServerSequenceValue { get; }
    TimeSpan KeepAliveClientInterval { get; }
    TimeSpan KeepAliveServerInterval { get; }
}
}