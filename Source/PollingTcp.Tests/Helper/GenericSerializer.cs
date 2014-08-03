using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PollingTcp.Tests
{
    class GenericSerializer<TDataType>
    {
        public byte[] Serialize(TDataType obj)
        {
            var bformatter = new BinaryFormatter();
            var mStream = new MemoryStream();

            bformatter.Serialize(mStream, obj);
            return mStream.ToArray();
        }

        public TDataType Deserialze(byte[] data)
        {
            var bformatter = new BinaryFormatter();
            var mStream = new MemoryStream(data);

            var value = bformatter.Deserialize(mStream);
            return (TDataType)value;
        }
    }
}