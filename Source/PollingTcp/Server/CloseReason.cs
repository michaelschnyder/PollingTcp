namespace PollingTcp.Server
{
    public enum CloseReason
    {
        Unknown,
        Error,
        HandshakeTimeout,
        ReceiveTimeout
    }
}