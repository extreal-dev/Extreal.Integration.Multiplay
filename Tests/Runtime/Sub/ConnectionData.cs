namespace Extreal.Integration.Multiplay.NGO.Test.Sub
{
    public class ConnectionData : IConnectionData
    {
        private byte[] serializedData;

        public byte[] Serialize()
             => serializedData;

        public void SetData(byte[] serializedData)
            => this.serializedData = serializedData;
    }
}
