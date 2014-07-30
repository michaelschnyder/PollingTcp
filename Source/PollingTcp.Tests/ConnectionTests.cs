using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Client;
using PollingTcp.Common;
using PollingTcp.Frame;
using PollingTcp.Server;

namespace PollingTcp.Tests
{
    [TestClass]
    public class ConnectionTests
    {

        [TestMethod]
        public void ConnectionEstablished_SendDataFromClient_ShoudBeReceivedOnServer()
        {
            var helloWorld = "Hello World!";

            var receivedMessagesInSession = new List<string>();
            var receivedMessagesOnClient = new List<string>();

            var networkLayer = new CombinedTestNetworkLayer();
            
            var client = new TestPollingClient(networkLayer);
            var server = new TestPollingServer(networkLayer, 10, 10);

            client.FrameReceived += (sender, args) => receivedMessagesOnClient.Add(Encoding.UTF8.GetString(args.Frame.Payload));
            

            server.Start();

            var sessionAcceptTask = server.AcceptAsync();

            client.ConnectAsync().Wait();
            sessionAcceptTask.Wait();

            var session = sessionAcceptTask.Result;
            
            Assert.AreEqual(ConnectionState.Connected, client.ConnectionState);
            Assert.IsNotNull(session);

            session.FrameReceived += (sender, args) => receivedMessagesInSession.Add(Encoding.UTF8.GetString(args.Frame.Payload));

            client.Send(new ClientDataFrame() { Payload = Encoding.UTF8.GetBytes(helloWorld)});

            Assert.IsTrue(receivedMessagesInSession.Any());
            Assert.AreEqual(helloWorld, receivedMessagesInSession[0]);
        }

    }

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
