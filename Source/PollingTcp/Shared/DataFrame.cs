namespace PollingTcp.Shared
{
    public class DataFrame
    {
        public int FrameId { get; set; }
    }

    public class ClientDataFrame : DataFrame
    {
        public int ClientId { get; set; }
    }
}