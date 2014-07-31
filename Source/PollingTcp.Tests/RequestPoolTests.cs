using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Client;
using PollingTcp.Frame;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    [TestClass]
    public class RequestPoolTests
    {
        readonly BinaryClientFrameEncoder clientFrameEncoder = new BinaryClientFrameEncoder();

        [TestMethod]
        public void NewRequestPool_WhenStarted_ShouldStartUpDefinedNumberOfClients()
        {
            var clientNetworkLayer = new ClientTestNetworkLinkLayer();
            var transportLayer = new ClientTestTransportLayer(clientNetworkLayer);

            var requestPool = new RequestPool<ClientControlFrame>(transportLayer);
            
            requestPool.Start();
            
            Assert.AreEqual(requestPool.InitialClientSize, requestPool.ActiveClients);
            
            requestPool.Stop();
        }

        [TestMethod]
        public void NewRequestPool_WhenStarted_ShouldSendControlMessages()
        {
            var clientNetworkLayer = new ClientTestNetworkLinkLayer();
            var transportLayer = new ClientTestTransportLayer(clientNetworkLayer);

            var requestPool = new RequestPool<ClientControlFrame>(transportLayer);

            requestPool.Start();
            requestPool.Stop();

            var frames = clientNetworkLayer.SentBytes.Select(this.clientFrameEncoder.Decode).ToList();

            Assert.IsTrue(frames.Any());
            Assert.IsTrue(frames.OfType<ClientControlFrame>().Any());
        }

        [TestMethod]
        public void NewRequestPool_WhenStarted_ShouldTearDownWhenStopped()
        {
            var clientNetworkLayer = new ClientTestNetworkLinkLayer();
            var transportLayer = new ClientTestTransportLayer(clientNetworkLayer);

            var requestPool = new RequestPool<ClientControlFrame>(transportLayer);

            requestPool.Start();
            Assert.AreEqual(requestPool.InitialClientSize, requestPool.ActiveClients);

            requestPool.StopAsync().Wait();
            Assert.AreEqual(0, requestPool.ActiveClients);
        }
    }

    class ClientTestTransportLayer : ISendControlFrame<ClientControlFrame>
    {
        private readonly ClientTestNetworkLinkLayer clientNetworkLayer;
        private BinaryClientFrameEncoder encoder;

        public ClientTestTransportLayer(ClientTestNetworkLinkLayer clientNetworkLayer)
        {
            this.clientNetworkLayer = clientNetworkLayer;
            this.encoder = new BinaryClientFrameEncoder();
        }

        public void Send(ClientControlFrame sendFrame)
        {
            this.clientNetworkLayer.Send(this.encoder.Encode(sendFrame));
        }
    }
}
