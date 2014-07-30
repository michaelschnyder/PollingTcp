using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PollingTcp.Client
{
    public class RequestClient<TSendControlFrameType> where TSendControlFrameType: new()
    {
        private readonly ISendControlFrame<TSendControlFrameType> transportLayer;
        private Thread workerThread;
        private bool shouldStop;

        public RequestClient(ISendControlFrame<TSendControlFrameType> transportLayer)
        {
            this.transportLayer = transportLayer;
            this.workerThread = new Thread(this.DoWork);
        }

        private void DoWork()
        {
            try
            {
                while (!this.shouldStop)
                {
                    this.transportLayer.Send(new TSendControlFrameType());
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
