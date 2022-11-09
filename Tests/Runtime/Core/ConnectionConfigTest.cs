using NUnit.Framework;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class ConnectionConfigTest
    {
        [Test]
        public void GetDefault()
        {
            var connectionConfig = ConnectionConfig.Default;
            Assert.IsNotNull(connectionConfig);
            Assert.AreEqual("127.0.0.1", connectionConfig.Address);
            Assert.AreEqual(7777, connectionConfig.Port);
            Assert.AreEqual("DefaultKey", connectionConfig.Key);
        }

        [Test]
        public void GetDefaultsAreSame()
        {
            var connectionConfigA = ConnectionConfig.Default;
            var connectionConfigB = ConnectionConfig.Default;
            Assert.IsNotNull(connectionConfigA);
            Assert.AreSame(connectionConfigA, connectionConfigB);
        }
    }
}
