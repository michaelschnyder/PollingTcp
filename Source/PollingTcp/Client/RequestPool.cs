using System.Collections.Generic;
using System.Threading.Tasks;
using PollingTcp.Frame;

namespace PollingTcp.Client
{
    public class RequestPool<TSendControlFrameType> where TSendControlFrameType : ClientControlFrame, new()
    {
        private readonly ISendControlFrame<TSendControlFrameType> transportLayer;
        List<RequestClient<TSendControlFrameType>> clients = new List<RequestClient<TSendControlFrameType>>();
        private readonly int initialClientSize;

        public RequestPool(ISendControlFrame<TSendControlFrameType> transportLayer, int initialNumberOfClients)
        {
            this.transportLayer = transportLayer;
            this.initialClientSize = initialNumberOfClients;
        }

        public RequestPool(ISendControlFrame<TSendControlFrameType> transportLayer) : this(transportLayer, 44)
        {
        }

        public int InitialClientSize
        {
            get { return this.initialClientSize; }
        }

        public int ActiveClients
        {
            get { return this.clients.Count; }
        }

        public int ClientId { get; set; }

        public void Start()
        {
            for (int i = 0; i < this.InitialClientSize; i++)
            {
                var client = new RequestClient<TSendControlFrameType>(this.transportLayer, this.ClientId);
                client.Start();
                this.clients.Add(client);
            }    
        }

        public void Stop()
        {
            this.StopAsync().Wait();
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
