namespace Extreal.Integration.Multiplay.NGO
{
    /// <summary>
    /// Interface for implementation handling the serialization data used in connection.
    /// </summary>
    public interface IConnectionData
    {
        /// <summary>
        /// Outputs the serialization data.
        /// </summary>
        /// <returns>Serialization data.</returns>
        byte[] Serialize();

        /// <summary>
        /// Sets the data.
        /// </summary>
        /// <param name="data">Data to be set.</param>
        void SetData(byte[] data);
    }
}
