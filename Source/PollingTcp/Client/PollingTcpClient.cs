using System;
using PollingTcp.Common;

namespace PollingTcp.Client
{
    public class PollingTcpClient
    {
        private readonly ILogicalLinkLayer logicalLinkLayer;

        public PollingTcpClient(ILogicalLinkLayer logicalLinkLayer)
        {
            this.logicalLinkLayer = logicalLinkLayer;
        }

        public void Connect()
        {
            if (this.logicalLinkLayer == null)
            {
                throw new Exception("Locgical Link Layer is not set!");
            }
        }

        public void Quit()
        {
        
        }
    }
}
