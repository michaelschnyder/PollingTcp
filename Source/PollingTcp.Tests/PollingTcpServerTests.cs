using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Server;
using PollingTcp.Shared;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    [TestClass]
    class PollingTcpServerTests
    {
        private ClientDataFrame initConnectionFrame = new ClientDataFrame() { SequenceId = 7 };

        [TestMethod]
        public void FreshServer_ReceivesEmptyClientId_ShouldResponseWithAClientId()
        {
            /*
            var networkLayer = new ServerTestNetworkLinkLayer();
            var server = new PollingTcpServer<ClientDataFrame, ServerDataFrame>(networkLayer, new BinaryClientFrameEncoder(), new BinaryServerFrameEncoder(), 10);

            server.Start();

            var session = server.Accept();

            // networkLayer.Receive(new BinaryClientFrameEncoder().Encode(initConnectionFrame));

            Assert.IsTrue(networkLayer.SentBytes.Any(), "There shoudl be at least one captured frame");
            
            var sentFrameBytes = networkLayer.SentBytes[0];
            var sentFrame = new GenericSerializer<ClientDataFrame>().Deserialze(sentFrameBytes);

            Assert.IsNotNull(sentFrame);
            Assert.IsTrue(sentFrame.SequenceId != 0);
            Assert.AreEqual(0, sentFrame.ClientId);
            Assert.IsNull(sentFrame.Payload);
            */
        }
    }
}
