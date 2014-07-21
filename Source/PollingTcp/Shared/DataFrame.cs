using System;

namespace PollingTcp.Shared
{
    [Serializable]
    public class DataFrame
    {
        public int SequenceId { get; set; }

        public byte[] Payload { get; set; }
    }

    [Serializable]
    public class ServerDataFrame : DataFrame
    {
        
    }

    [Serializable]
    public class ClientDataFrame : DataFrame
    {
        public int ClientId { get; set; }
    }
}