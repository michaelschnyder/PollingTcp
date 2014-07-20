namespace PollingTcp.Client
{
    public class ConnectionStateChangedEventArgs
    {
        public ConnectionState State { get; set; }
        public ConnectionState PreviousState { get; set; }
    }
}