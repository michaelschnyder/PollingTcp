using System.Collections.Generic;

namespace PollingTcp.Client
{
    public class RequestPool<TSendControlFrameType>
    {
        private readonly ISendControlFrame<TSendControlFrameType> transportLayer;
        List<RequestClient<TSendControlFrameType>> clients = new List<RequestClient<TSendControlFrameType>>();
        private int maxClientsActive;

        public RequestPool(ISendControlFrame<TSendControlFrameType> transportLayer)
        {
            this.transportLayer = transportLayer;
            this.maxClientsActive = 5;
        }

        public int MaxClientsActive
        {
            get { return this.maxClientsActive; }
            set { this.maxClientsActive = value; }
        }

        public void Start()
        {
            for (int i = 0; i < this.MaxClientsActive; i++)
            {
                var client = new RequestClient<TSendControlFrameType>(this.transportLayer);
                client.Start();
            }    
        }

        public void Stop()
        {
            var allClients = new List<RequestClient<TSendControlFrameType>>(this.clients);

            foreach (var client in allClients)
            {
                client.Stop();
                this.clients.Remove(client);
            }
        }
    }
}
