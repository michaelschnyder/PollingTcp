using System;
using PollingTcp.Client;
using PollingTcp.Common;
using PollingTcp.Server;

namespace PollingTcp.Tests
{
    public class CombinedTestNetworkLayer : IClientNetworkLinkLayer, IServerNetworkLinkLayer
    {
        public int MaxWindowSize { get; private set; }
        public void Send(byte[] bytesToSend)
        {
            // This is called by the client, so forward it to the server
            var serverResult = this.PollHandler(bytesToSend);

            if (this.PollHandler == null)
            {
                return;
            }

            if (serverResult != null)
            {
                this.OnDataReceived(new DataReceivedEventArgs()
                {
                    Data = serverResult
                });
            }
        }

        public event EventHandler<DataReceivedEventArgs> DataReceived;

        protected virtual void OnDataReceived(DataReceivedEventArgs e)
        {
            EventHandler<DataReceivedEventArgs> handler = this.DataReceived;
            if (handler != null) handler(this, e);
        }

        public Func<byte[], byte[]> PollHandler { get; set; }
    }
}