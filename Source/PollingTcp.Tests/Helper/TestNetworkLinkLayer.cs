using System.Collections.Generic;
using PollingTcp.Common;

namespace PollingTcp.Tests.Helper
{
    abstract class TestNetworkLinkLayer : INetworkLinkLayer
    {
        private readonly List<byte[]> sentBytes = new List<byte[]>();

        public int MaxWindowSize { get; set; }
        
        public void Send(byte[] data)
        {
            this.sentBytes.Add(data);
        }

        public List<byte[]> SentBytes
        {
            get { return this.sentBytes; }
        }

        public abstract List<byte[]> ReceivedBytes { get; }
    }
}