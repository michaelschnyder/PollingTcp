using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Frame;
using PollingTcp.Tests.Helper;

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

            var networkLayer = new CombinedTestNetworkLayer();
            
            var client = new TestPollingClient(networkLayer);
            var server = new TestPollingServer(networkLayer);

            var session = ConnectionHelper.WaitForConnectionHandshake(server, client);

            session.FrameReceived += (sender, args) => receivedMessagesInSession.Add(Encoding.UTF8.GetString(args.Frame.Payload));

            client.Send(new ClientDataFrame() { Payload = Encoding.UTF8.GetBytes(helloWorld)});

            Assert.IsTrue(receivedMessagesInSession.Any());
            Assert.AreEqual(helloWorld, receivedMessagesInSession[0]);

            server.Stop();
            client.DisconnectAsync().Wait();
        }

        [TestMethod]
        public void ConnectionEstablished_SendDataFromServer_ShoudBeReceivedOnClient()
        {
            var helloWorld = "Hello World!";

            var receivedMessagesOnClient = new List<string>();
            var atLeastOneFrameReceivedOnClient = new AutoResetEvent(false);
            var networkLayer = new CombinedTestNetworkLayer();

            var client = new TestPollingClient(networkLayer);
            var server = new TestPollingServer(networkLayer);

            server.Start();
            
            return;

            var session = ConnectionHelper.WaitForConnectionHandshake(server, client);
            ConnectionHelper.WaitForConnectionEstablishment(session);

            client.FrameReceived += (sender, args) =>
            {
                receivedMessagesOnClient.Add(Encoding.UTF8.GetString(args.Frame.Payload));
                atLeastOneFrameReceivedOnClient.Set();
            };

            session.Send(new ServerDataFrame()
            {
                Payload = Encoding.UTF8.GetBytes(helloWorld)
            });

            atLeastOneFrameReceivedOnClient.WaitOne(5000);

            Assert.IsTrue(receivedMessagesOnClient.Any());
            Assert.AreEqual(helloWorld, receivedMessagesOnClient[0]);

            server.Stop();
            client.DisconnectAsync().Wait();
        }

        [TestMethod]
        public void ConnectionEstablished_ServerIsStopped_ClientDisconnects()
        {
            
        }
    }
}
