using System.Collections.Generic;
using System.Threading.Tasks;
using PollingTcp.Frame;

namespace PollingTcp.Client
{
    public class RequestPool<TSendControlFrameType> where TSendControlFrameType : ClientControlFrame, new()
    {
        private readonly ISendControlFrame<TSendControlFrameType> transportLayer;
        List<RequestClient<TSendControlFrameType>> clients = new List<RequestClient<TSendControlFrameType>>();
        private int maxClientsActive;

        public RequestPool(ISendControlFrame<TSendControlFrameType> transportLayer)
        {
            this.transportLayer = transportLayer;
            this.maxClientsActive = 1;
        }

        public int MaxClientsActive
        {
            get { return this.maxClientsActive; }
            set { this.maxClientsActive = value; }
        }

        public int ActiveClients
        {
            get { return this.clients.Count; }
        }

        public int ClientId { get; set; }

        public void Start()
        {
            for (int i = 0; i < this.MaxClientsActive; i++)
            {
                var client = new RequestClient<TSendControlFrameType>(this.transportLayer, this.ClientId);
                client.Start();
                this.clients.Add(client);
            }    
        }

        public async void Stop()
        {
            await this.StopAsync();
        }

        public Task StopAsync()
        {
            var task = new Task(() =>
            {
                var allClients = new List<RequestClient<TSendControlFrameType>>(this.clients);

                foreach (var client in allClients)
                {
                    client.Stop();
                    this.clients.Remove(client);
                }
            });

            task.Start();
            return task;
        }
    }
}
