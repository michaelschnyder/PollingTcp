using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using PollingTcp.Common;

namespace PollingTcp.Tests.Helper
{
    abstract class TestNetworkLinkLayer : INetworkLinkLayer
    {
        private readonly ConcurrentBag<byte[]> sentBytes = new ConcurrentBag<byte[]>();

        public int MaxWindowSize { get; set; }
        
        public TimeSpan SendDelay { get; set; }

        public void Send(byte[] data)
        {
            if (sentBytes.Count < 1000)
            {
                this.sentBytes.Add(data);
            }

            Thread.Sleep(this.SendDelay);
        }

        public List<byte[]> SentBytes
        {
            get { return new List<byte[]>(this.sentBytes); }
        }

        public abstract List<byte[]> ReceivedBytes { get; }
    }
}