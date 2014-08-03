using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Client;
using PollingTcp.Frame;
using PollingTcp.Server;

namespace PollingTcp.Tests.Helper
{
    internal class ConnectionHelper
    {
        public static PollingClientSession<ClientDataFrame, ServerDataFrame> WaitForConnectionEstablishment(TestPollingServer server, TestPollingClient client)
        {
            var isSessionAccepted = new AutoResetEvent(false);

            PollingClientSession<ClientDataFrame, ServerDataFrame> session = null;
            server.Start();

            var task = new Task(() =>
            {
                session = server.Accept();
                isSessionAccepted.Set();
            });

            task.Start();

            while (!isSessionAccepted.WaitOne(10))
            {
                client.ConnectAsync().Wait();

            }

            Assert.AreEqual(ConnectionState.Connected, client.ConnectionState);
            Assert.IsNotNull(session);
            
            return session;
        }

        internal static PollingClientSession<ClientDataFrame, ServerDataFrame> WaitForClientSession(TestPollingServer server, ServerTestNetworkLinkLayer networkLayer, ClientDataFrame initConnectionFrame)
        {
            PollingClientSession<ClientDataFrame, ServerDataFrame> session = null;
            var resetEvent = new AutoResetEvent(false);
            var t = new Task(() =>
            {
                session = server.Accept();
                resetEvent.Set();
            });

            t.Start();

            while (!resetEvent.WaitOne(10))
            {
                networkLayer.Receive(new BinaryClientFrameEncoder().Encode((ClientDataFrame)initConnectionFrame));
            }
            return session;
        }
    }
}
