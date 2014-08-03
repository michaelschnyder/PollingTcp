using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Frame;
using PollingTcp.Server;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    [TestClass]
    public class PollingServerTests
    {
        private ClientDataFrame initConnectionFrame = new ClientDataFrame() { SequenceId = 7 };

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InitializeServer_WithoutLinkLayer_ShouldRaiseException()
        {
            var server = new TestPollingServer(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InitializeServer_WithoutProtocolSpecification_ShouldThrowAnException()
        {
            var client = new TestPollingServer(new ServerTestNetworkLinkLayer(), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InitializeServer_WithEmptyClientEncoder_ShouldThrowAnException()
        {
            var specification = new TestProtocolSpecification()
            {
                ClientEncoder = null,
                ServerEncoder = new BinaryServerFrameEncoder()
            };

            var server = new TestPollingServer(new ServerTestNetworkLinkLayer(), specification);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InitializeServer_WithEmptyServerEncoder_ShouldThrowAnException()
        {
            var specification = new TestProtocolSpecification()
            {
                ClientEncoder = new BinaryClientFrameEncoder(),
                ServerEncoder = null
            };

            var server = new TestPollingServer(new ServerTestNetworkLinkLayer(), specification);
        }


        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void NewServer_TryToAcceptSessionButDidNotStartYet_ShouldThrowAnException()
        {
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new TestPollingServer(networkLayer);

            server.Accept();
        }

        [TestMethod]
        public void NewServer_ReceivesConnectionRequest_NothingHappens()
        {
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new TestPollingServer(networkLayer);

            networkLayer.Receive(new BinaryClientFrameEncoder().Encode(this.initConnectionFrame));
            Assert.AreEqual(0, server.SessionCount);
        }

        [TestMethod]
        public void StartedServer_ReceivesEmptyClientId_ShouldUnblockAcceptAndReturnSession()
        {
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new TestPollingServer(networkLayer);

            PollingClientSession<ClientDataFrame, ServerDataFrame> session = null;

            server.Start();

            session = ConnectionHelper.WaitForClientSession(server, networkLayer, this.initConnectionFrame);

            Assert.IsNotNull(session, "There was no session captured");
        }

        [TestMethod]
        public void StartedServer_ReceivesEmptyClientId_ShouldResponseWithAClientId()
        {
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new TestPollingServer(networkLayer);

            server.Start();

            ConnectionHelper.WaitForClientSession(server, networkLayer, this.initConnectionFrame);

            networkLayer.Receive(new BinaryClientFrameEncoder().Encode(initConnectionFrame));

            Assert.IsTrue(networkLayer.SentBytes.Any(), "There should be at least one captured frame");
            
            var sentFrameBytes = networkLayer.SentBytes[0];
            var sentFrame = new GenericSerializer<ServerDataFrame>().Deserialze(sentFrameBytes);

            Assert.IsNotNull(sentFrame);
            Assert.IsTrue(sentFrame.SequenceId != 0);
            Assert.IsNotNull(sentFrame.Payload);
            Assert.AreNotEqual(0, BitConverter.ToInt32(sentFrame.Payload, 0));
        }

        [TestMethod]
        public void EstablishedSession_ReceivesData_ShouldTriggerEventOnSession()
        {
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new TestPollingServer(networkLayer);

            var serverOnFrameReceived = new List<ClientDataFrame>();

            server.Start();

            var session = ConnectionHelper.WaitForClientSession(server, networkLayer, this.initConnectionFrame);
            session.FrameReceived += (sender, args) => serverOnFrameReceived.Add(args.Frame);

            // Find out the ClientId
            Assert.IsTrue(networkLayer.SentBytes.Any(), "There should be at least one captured frame");

            var sentFrameBytes = networkLayer.SentBytes[0];
            var sentFrame = new GenericSerializer<ServerDataFrame>().Deserialze(sentFrameBytes);

            Assert.IsNotNull(sentFrame);
            Assert.IsTrue(sentFrame.SequenceId != 0);
            Assert.IsNotNull(sentFrame.Payload);
            Assert.AreNotEqual(0, BitConverter.ToInt32(sentFrame.Payload, 0));

            var clientId = BitConverter.ToInt32(sentFrame.Payload, 0);
            var clientFrame = new ClientDataFrame()
            {
                ClientId = clientId,
                SequenceId = initConnectionFrame.SequenceId + 1,
                Payload = Encoding.UTF8.GetBytes("Hello World")
            };

            networkLayer.Receive(new GenericSerializer<ClientDataFrame>().Serialize(clientFrame));

            Assert.AreEqual(1, serverOnFrameReceived.Count);
            Assert.AreEqual("Hello World", Encoding.UTF8.GetString(serverOnFrameReceived[0].Payload));
        }
    }
}
