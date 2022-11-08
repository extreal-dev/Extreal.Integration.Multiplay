namespace Extreal.Integration.Multiplay.NGO
{
    public interface IConnectionData
    {
        byte[] Serialize();
        void SetData(byte[] serializedData);
    }
}
