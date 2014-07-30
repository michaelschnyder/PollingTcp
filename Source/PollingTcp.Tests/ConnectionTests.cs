using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Client;
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

            var session = WaitForConnectionEstablishment(server, client);

            client.FrameReceived += (sender, args) => receivedMessagesOnClient.Add(Encoding.UTF8.GetString(args.Frame.Payload));
            session.FrameReceived += (sender, args) => receivedMessagesInSession.Add(Encoding.UTF8.GetString(args.Frame.Payload));

            client.Send(new ClientDataFrame() { Payload = Encoding.UTF8.GetBytes(helloWorld)});

            Assert.IsTrue(receivedMessagesInSession.Any());
            Assert.AreEqual(helloWorld, receivedMessagesInSession[0]);
        }

        [TestMethod]
        public void ConnectionEstablished_SendDataFromServer_ShoudBeReceivedOnClient()
        {
            var helloWorld = "Hello World!";

            var receivedMessagesOnClient = new List<string>();

            var networkLayer = new CombinedTestNetworkLayer();

            var client = new TestPollingClient(networkLayer);
            var server = new TestPollingServer(networkLayer, 10, 10);

            server.Start();

            var session = WaitForConnectionEstablishment(server, client);
            client.FrameReceived += (sender, args) => receivedMessagesOnClient.Add(Encoding.UTF8.GetString(args.Frame.Payload));

            session.Send(new ServerDataFrame()
            {
                Payload = Encoding.UTF8.GetBytes(helloWorld)
            });

            Assert.IsTrue(receivedMessagesOnClient.Any());
            Assert.AreEqual(helloWorld, receivedMessagesOnClient[0]);

        }

        private static ClientSession<ClientDataFrame, ServerDataFrame> WaitForConnectionEstablishment(TestPollingServer server, TestPollingClient client)
        {
            server.Start();

            var sessionAcceptTask = server.AcceptAsync();

            client.ConnectAsync().Wait();
            sessionAcceptTask.Wait();

            var session = sessionAcceptTask.Result;

            Assert.AreEqual(ConnectionState.Connected, client.ConnectionState);
            Assert.IsNotNull(session);
            return session;
        }
    }
}
