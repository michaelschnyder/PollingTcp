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
        public static PollingClientSession<ClientDataFrame, ServerDataFrame> WaitForConnectionHandshake(TestPollingServer server, TestPollingClient client)
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

            for (int i = 0; i < 3; i++)
            {
                while (!isSessionAccepted.WaitOne(10))
                {
                    client.ConnectAsync().Wait();

                }

                // Backup for Build server (some extra time)
                if (client.ConnectionState == ConnectionState.Connected)
                {
                    break;
                }

                Thread.Sleep(1000 * (i + 1));
            }

            Assert.AreEqual(ConnectionState.Connected, client.ConnectionState);
            Assert.IsNotNull(session);
            
            return session;
        }

        public static PollingClientSession<ClientDataFrame, ServerDataFrame> WaitForConnectionEstablishment(PollingClientSession<ClientDataFrame, ServerDataFrame> session)
        {
            var isConnected = new AutoResetEvent(false);

            session.StateChanged += (sender, args) =>
            {
                if (args.State == SessionState.Connected)
                {
                    isConnected.Set();
                }
            };

            while (session.SessionState != SessionState.Connected)
            {
                isConnected.WaitOne(500);
            }

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
