using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PollingTcp.Frame;

namespace PollingTcp.Client
{
    public class RequestClient<TSendControlFrameType> where TSendControlFrameType: ClientControlFrame, new()
    {
        private readonly ISendControlFrame<TSendControlFrameType> transportLayer;
        private readonly int clientId;
        private readonly Thread workerThread;
        private bool shouldStop;

        public RequestClient(ISendControlFrame<TSendControlFrameType> transportLayer, int clientId)
        {
            this.transportLayer = transportLayer;
            this.clientId = clientId;
            this.workerThread = new Thread(this.DoWork);
        }

        private void DoWork()
        {
            try
            {
                while (!this.shouldStop)
                {
                    var controlFrame = new TSendControlFrameType
                    {
                        ClientId = this.clientId
                    };

                    this.transportLayer.Send(controlFrame);
                }
            }
            catch (ThreadInterruptedException)
            {
                
            }
        }

        public void Start()
        {
            this.workerThread.Start();
        }

        public void Stop()
        {
            this.shouldStop = true;

            this.workerThread.Join(2000);

            if (this.workerThread.IsAlive)
            {
                this.workerThread.Interrupt();
            }
        }
    }
}
