namespace PollingTcp.Server
{
    public enum SessionState
    {
        Created,
        Handshaking,
        Connected,
        Timeout,
        Closed
    }
}