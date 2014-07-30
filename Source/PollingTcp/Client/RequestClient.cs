using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PollingTcp.Client
{
    public class RequestClient<TSendControlFrameType>
    {
        private readonly ISendControlFrame<TSendControlFrameType> transportLayer;

        public RequestClient(ISendControlFrame<TSendControlFrameType> transportLayer)
        {
            this.transportLayer = transportLayer;
        }

        public void Start()
        {

        }

        public void Stop()
        {

        }
    }
}
