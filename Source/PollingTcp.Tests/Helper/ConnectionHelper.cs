using System;
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
            PollingClientSession<ClientDataFrame, ServerDataFrame> session = null;
            server.Start();

            var acceptTask = server.AcceptAsync();

            EventHandler<ConnectionStateChangedEventArgs> clientOnConnectionStateChanged = delegate(object sender, ConnectionStateChangedEventArgs args)
            {
                Console.WriteLine("Changed from {0} to {1}", args.PreviousState, args.State);
            };

            client.ConnectionStateChanged += clientOnConnectionStateChanged;

            var connectTask = client.ConnectAsync();

            var allCompletedWithoutTimeout = Task.WaitAll(new Task[] { acceptTask, connectTask }, 10000);

            if (allCompletedWithoutTimeout)
            {
                session = acceptTask.Result;
            }

            Assert.IsTrue(allCompletedWithoutTimeout, string.Format("There was a timeout while waiting for all tasks to complete! AcceptTask: {0}, ConnectTask: {1}", acceptTask.Status, connectTask.Status));
            
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
