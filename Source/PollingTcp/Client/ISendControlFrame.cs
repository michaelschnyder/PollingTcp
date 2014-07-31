namespace PollingTcp.Client
{
    public interface ISendControlFrame<TSendControlFrameType>
    {
        void Send(TSendControlFrameType sendFrame);
    }
}