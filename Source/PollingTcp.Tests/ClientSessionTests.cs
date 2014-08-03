using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Frame;
using PollingTcp.Server;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    [TestClass]
    public class ClientSessionTests
    {
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void CreatedSession_WhenTryingToSendData_ShouldThrowException()
        {
            var session = CreateDefaultSession();

            session.Send(new ServerDataFrame());            
        }

        [TestMethod]
        public void CreatedSession_WhenStarted_ShouldBeInHandshakingState()
        {
            var session = CreateDefaultSession();

            session.Start();

            Assert.AreEqual(SessionState.Handshaking, session.SessionState);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void HandshakingSession_WhenTryingToSendData_ShouldThrowException()
        {
            var session = CreateDefaultSession();

            session.Start();

            Assert.AreEqual(SessionState.Handshaking, session.SessionState);

            session.Send(new ServerDataFrame());
        }

        [TestMethod]
        public void HandshakingSession_DoesntReceivedData_ShouldCloseWithCorrectReason()
        {
            var sessionCloseReason = CloseReason.Unknown;

            var waitEvent = new ManualResetEvent(false);

            var session = CreateDefaultSession();

            session.SessionClosed += (sender, args) =>
            {
                sessionCloseReason = args.Reason;
                waitEvent.Set();
            };

            session.Start();
            Assert.AreEqual(SessionState.Handshaking, session.SessionState);

            var eventHasBeenRaised = waitEvent.WaitOne(TimeSpan.FromMilliseconds(2000));

            Assert.IsTrue(eventHasBeenRaised, "There should be an event within the desired timeout!");
            Assert.AreEqual(SessionState.Closed, session.SessionState);
            Assert.AreEqual(CloseReason.HandshakeTimeout, sessionCloseReason);
        }

        [TestMethod]
        public void EstablishedSession_ReceivesNoData_ShouldCloseWithCorrectReason()
        {
            var sessionCloseReason = CloseReason.Unknown;

            var waitEvent = new ManualResetEvent(false);

            var networkLayer = new CombinedTestNetworkLayer();

            var client = new TestPollingClient(networkLayer);
            var server = new TestPollingServer(networkLayer);

            var session = ConnectionHelper.WaitForConnectionHandshake(server, client);
            ConnectionHelper.WaitForConnectionEstablishment(session);

            Assert.AreEqual(SessionState.Connected, session.SessionState);

            session.SessionClosed += (sender, args) =>
            {
                sessionCloseReason = args.Reason;
                waitEvent.Set();
            };

            client.DisconnectAsync().Wait();

            Assert.AreEqual(0, client.CurrentPollingPoolSize);

            var eventHasBeenRaised = waitEvent.WaitOne(5000);

            Assert.IsTrue(eventHasBeenRaised, "There should be a timeout event within the desired timeout!");
            Assert.AreEqual(SessionState.Closed, session.SessionState);
            Assert.AreEqual(CloseReason.HandshakeTimeout, sessionCloseReason);

        }

        private static PollingClientSession<ClientDataFrame, ServerDataFrame> CreateDefaultSession()
        {
            return new PollingClientSession<ClientDataFrame, ServerDataFrame>(12345, new TestProtocolSpecification(), TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(1000));
        }
    }
}
