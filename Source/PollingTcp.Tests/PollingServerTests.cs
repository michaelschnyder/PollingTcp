using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Server;
using PollingTcp.Shared;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    [TestClass]
    public class PollingServerTests
    {
        private ClientDataFrame initConnectionFrame = new ClientDataFrame() { SequenceId = 7 };

        [TestMethod]
        public void FreshServer_ReceivesEmptyClientId_ShouldResponseWithAClientId()
        {
            
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new PollingServer<ClientControlFrame, ClientDataFrame, ServerDataFrame>(networkLayer, new BinaryClientControlFrameEncoder(), new BinaryClientDataFrameEncoder(), new BinaryServerFrameEncoder(), 10, 10);

            server.Start();

            var session = server.Accept();

            networkLayer.Receive(new BinaryClientDataFrameEncoder().Encode(initConnectionFrame));

            Assert.IsTrue(networkLayer.SentBytes.Any(), "There should be at least one captured frame");
            
            var sentFrameBytes = networkLayer.SentBytes[0];
            var sentFrame = new GenericSerializer<ServerDataFrame>().Deserialze(sentFrameBytes);

            Assert.IsNotNull(sentFrame);
            Assert.IsTrue(sentFrame.SequenceId != 0);
            Assert.IsNotNull(sentFrame.Payload);
            Assert.AreNotEqual(0, BitConverter.ToInt32(sentFrame.Payload, 0));
        }
    }
}
