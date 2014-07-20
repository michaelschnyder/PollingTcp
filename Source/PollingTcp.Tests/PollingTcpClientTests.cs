using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PollingTcp.Client;
using PollingTcp.Common;

namespace PollingTcp.Tests
{
    [TestClass]
    public class PollingTcpClientTests
    {
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InitializeClient_WithNotLinkLayer_ShoulThrowAnException()
        {
            var client = new PollingTcpClient(null);
            client.Connect();
        }

        [TestMethod]
        public void ReadyClient_WhenConnecting_ShouldRaiseConnectingStateViaEvent()
        {
            var client = new PollingTcpClient(new DummyClientLogicalLinkLayer());

            var numberOfConnectingEventsRaised = 0;
            client.ConnectionStateChanged += (sender, args) => numberOfConnectingEventsRaised += args.State == ConnectionState.Connecting ?  + 1 : 0;

            client.Connect();

            Assert.AreEqual(1, numberOfConnectingEventsRaised);        
            Assert.AreEqual(ConnectionState.Connecting, client.ConnectionState);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ConnectingClient_WhenConnection_ShouldRaiseException()
        {
            var client = new PollingTcpClient(new DummyClientLogicalLinkLayer());

            client.Connect();
            client.Connect();
        }
    }

    class DummyClientLogicalLinkLayer : ILogicalLinkLayer
    {
        public int MaxWindowSize { get; set; }
    }

}