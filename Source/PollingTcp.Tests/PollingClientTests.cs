using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using PollingTcp.Client;
using PollingTcp.Frame;
using PollingTcp.Shared;
using PollingTcp.Tests.Helper;

namespace PollingTcp.Tests
{
    [TestClass]
    public class PollingClientTests
    {
        private GenericSerializer<ServerDataFrame> serverDataFrameSerializer = new GenericSerializer<ServerDataFrame>();
        private GenericSerializer<ClientDataFrame> clientDataFrameSerializer = new GenericSerializer<ClientDataFrame>();
        private GenericSerializer<ClientControlFrame> clientControlFrameSerializer = new GenericSerializer<ClientControlFrame>();
        private GenericSerializer<ClientFrame> clientAnyFrameSerializer = new GenericSerializer<ClientFrame>();

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InitializeClient_WithoutLinkLayer_ShouldThrowAnException()
        {
            var client = new TestPollingClient(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InitializeClient_WithoutProtocolSpecification_ShouldThrowAnException()
        {
            var client = new TestPollingClient(new ClientTestNetworkLinkLayer(), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InitializeClient_WithEmptyClientEncoder_ShouldThrowAnException()
        {
            var specification = new TestProtocolSpecification()
            {
                ClientEncoder = null,
                ServerEncoder = new BinaryServerFrameEncoder()
            };

            var client = new TestPollingClient(new ClientTestNetworkLinkLayer(), specification);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InitializeClient_WithEmptyServerEncoder_ShouldThrowAnException()
        {
            var specification = new TestProtocolSpecification()
            {
                ClientEncoder = new BinaryClientFrameEncoder(),
                ServerEncoder = null
            };

            var client = new TestPollingClient(new ClientTestNetworkLinkLayer(), specification);
        }

        [TestMethod]
        public void ReadyClient_WhenConnecting_ShouldTriggerConnectingStateEvent()
        {
            var client = new TestPollingClient(new ClientTestNetworkLinkLayer());

            var numberOfConnectingEventsRaised = 0;
            client.ConnectionStateChanged += (sender, args) => numberOfConnectingEventsRaised += args.State == ConnectionState.Connecting ?  + 1 : 0;

            client.ConnectAsync();

            Assert.AreEqual(1, numberOfConnectingEventsRaised);        
            Assert.AreEqual(ConnectionState.Connecting, client.ConnectionState);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ReadyClient_WhenTryToSend_RaiseException()
        {
            var client = new TestPollingClient(new ClientTestNetworkLinkLayer());

            client.Send(new ClientDataFrame());
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ConnectingClient_WhenConnection_ShouldRaiseException()
        {
            var client = new TestPollingClient(new ClientTestNetworkLinkLayer());

            client.ConnectAsync();
            client.ConnectAsync();
        }

        [TestMethod]
        public void ConnectingClient_WithNoResponse_ShouldGoBacktoDisconnectedState()
        {
            var client = new TestPollingClient(new ClientTestNetworkLinkLayer());
            
            client.ConnectionAttemptTimeout = TimeSpan.FromMilliseconds(5);
            client.ConnectAsync().Wait();

            Assert.AreEqual(ConnectionState.Disconnected, client.ConnectionState);
        }

        [TestMethod]
        public void ConnectingClient_WithNoResponse_ShouldShouldTriggerTimeoutConnectionAndDisconnectedEvent()
        {
            var client = new TestPollingClient(new ClientTestNetworkLinkLayer());
            var recordedConnectionStates = new List<ConnectionState>();

            client.ConnectionAttemptTimeout = TimeSpan.FromMilliseconds(5);
            client.ConnectionStateChanged += (sender, args) => recordedConnectionStates.Add(args.State);
            
            client.ConnectAsync().Wait();

            Assert.AreEqual(3, recordedConnectionStates.Count);
            Assert.AreEqual(ConnectionState.Connecting, recordedConnectionStates[0]);
            Assert.AreEqual(ConnectionState.Timeout, recordedConnectionStates[1]);
            Assert.AreEqual(ConnectionState.Disconnected, recordedConnectionStates[2]);
        }

        [TestMethod]
        public void ReadyClient_WhenConnecting_SendAnEmptyClientId()
        {
            var networkLayer = new ClientTestNetworkLinkLayer();

            var client = new TestPollingClient(networkLayer);
            
            client.ConnectAsync();

            Assert.AreEqual(1, networkLayer.SentBytes.Count);
            
            var sentFrameBytes = networkLayer.SentBytes[0];
            var sentFrame = new GenericSerializer<ClientDataFrame>().Deserialze(sentFrameBytes);

            Assert.IsNotNull(sentFrame);
            Assert.IsTrue(sentFrame.SequenceId != 0);
            Assert.AreEqual(0, sentFrame.ClientId);
            Assert.IsNull(sentFrame.Payload);
        }

        [TestMethod]
        public void ConnectingClient_ReceivesResponseWhileConnecting_ShouldSwitchToConnectedState()
        {
            var connectionRequestResponse = new ServerDataFrame()
            {
                SequenceId = 7,
                Payload = BitConverter.GetBytes(12345)
            };

            var networkLayer = new ClientTestNetworkLinkLayer();

            var client = new TestPollingClient(networkLayer);

            client.ConnectAsync();

            byte[] data = this.serverDataFrameSerializer.Serialize(connectionRequestResponse);

            networkLayer.Receive(data);
            Assert.AreEqual(ConnectionState.Connected, client.ConnectionState);

            client.DisconnectAsync();
        }

        [TestMethod]
        public void ConnectingClient_ReceivesResponseWhileConnecting_ShouldUseClientIdInData()
        {
            var connectionRequestResponse = new ServerDataFrame()
            {
                SequenceId = 7,
                Payload = BitConverter.GetBytes(12345)
            };

            var networkLayer = new ClientTestNetworkLinkLayer();

            var client = new TestPollingClient(networkLayer);

            client.ConnectAsync();

            networkLayer.Receive(this.serverDataFrameSerializer.Serialize(connectionRequestResponse));
            client.Send(new ClientDataFrame());
            client.DisconnectAsync();
        }

        [TestMethod]
        public void ConnectedClient_WhenConnected_ShouldSendAControlFrameAtLeast()
        {
            var connectionRequestResponse = new ServerDataFrame()
            {
                SequenceId = 7,
                Payload = BitConverter.GetBytes(12345)
            };

            var networkLayer = new ClientTestNetworkLinkLayer();

            var client = new TestPollingClient(networkLayer);

            client.ConnectAsync();

            byte[] data = this.serverDataFrameSerializer.Serialize(connectionRequestResponse);
            networkLayer.Receive(data);

            Assert.AreEqual(ConnectionState.Connected, client.ConnectionState);

            client.DisconnectAsync().Wait(5000);

            var allSentControlFrames = networkLayer.SentBytes.Select(b => this.clientAnyFrameSerializer.Deserialze(b)).ToList();

            Assert.IsTrue(allSentControlFrames.Any());
            Assert.IsTrue(allSentControlFrames.OfType<ClientControlFrame>().Any());
        }
    }

    class TestPollingClient : PollingClient<ClientControlFrame, ClientDataFrame, ServerDataFrame>
    {
        public TestPollingClient(IClientNetworkLinkLayer clientNetworkLinkLayer) : base(clientNetworkLinkLayer, new BinaryClientFrameEncoder(), new BinaryServerFrameEncoder(), 10, 10)
        {

        }

        public TestPollingClient(IClientNetworkLinkLayer clientNetworkLinkLayer, IProtocolSpecification<ClientControlFrame, ClientDataFrame, ServerDataFrame> specification)
            : base(clientNetworkLinkLayer, specification)
        {

        }

    }

    class GenericSerializer<TDataType>
    {
        public byte[] Serialize(TDataType obj)
        {
            var bformatter = new BinaryFormatter();
            var mStream = new MemoryStream();

            bformatter.Serialize(mStream, obj);
            return mStream.ToArray();
        }

        public TDataType Deserialze(byte[] data)
        {
            var bformatter = new BinaryFormatter();
            var mStream = new MemoryStream(data);

            var value = bformatter.Deserialize(mStream);
            return (TDataType)value;
        }
    }
}