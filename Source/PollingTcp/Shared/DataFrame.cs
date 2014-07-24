using System;

namespace PollingTcp.Shared
{
    [Serializable]
    public class DataFrame
    {
        public byte[] Payload { get; set; }
    }

    public interface ISequencedDataFrame
    {
        int SequenceId { get; set; }
    }

    [Serializable]
    public class ServerDataFrame : DataFrame, ISequencedDataFrame
    {
        public int SequenceId { get; set; }
    }

    [Serializable]
    public abstract class ClientFrame : DataFrame
    {
        public int ClientId { get; set; }
    }

    [Serializable]
    public class ClientDataFrame : ClientFrame, ISequencedDataFrame
    {
        public int SequenceId { get; set; }
    }

    [Serializable]
    public class ClientPollFrame : ClientFrame
    {
    }
}