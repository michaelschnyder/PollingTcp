using System;
using System.Collections.Generic;
using PollingTcp.Common;
using PollingTcp.Server;

namespace PollingTcp.Tests.Helper
{
    class ServerTestNetworkLinkLayer : IServerNetworkLinkLayer
    {
        public int MaxWindowSize { get; private set; }
        public Func<byte[], byte[]> PollHandler { get; set; }

        public List<byte[]> SentBytes { get; set; }

        public ServerTestNetworkLinkLayer()
        {
            this.SentBytes = new List<byte[]>();
        }

        public void Receive(byte[] data)
        {
            if (this.PollHandler != null)
            {
                var response = this.PollHandler(data);

                if (response != null)
                {
                    this.SentBytes.Add(response);
                }
            }
        }
    }
}