using System;

namespace PollingTcp.Shared
{
    [Serializable]
    public class DataFrame
    {
        public byte[] Payload { get; set; }
    }

    [Serializable]
    public class SequencedDataFrame : DataFrame
    {
        public int SequenceId { get; set; }
    }

    [Serializable]
    public class ServerDataFrame : SequencedDataFrame
    {
    }

    [Serializable]
    public class ClientDataFrame : SequencedDataFrame
    {
        public int ClientId { get; set; }
    }

    [Serializable]
    public class ClientPollFrame : DataFrame
    {
        public int ClientId { get; set; }
    }
}