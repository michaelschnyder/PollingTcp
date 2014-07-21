namespace PollingTcp.Common
{
    public abstract class FrameEncoder<TFrameDataType>
    {
        public abstract TFrameDataType Decode(byte[] bytes);

        public abstract byte[] Encode(TFrameDataType clientFrame);
    }
}