using System;
using NUnit.Framework;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class ConnectionConfigTest
    {
        [Test]
        public void NewConnectionConfigWithInvalidIpAddressForm()
            => Assert.That(() => _ = new ConnectionConfig("256:0:0:1"),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("The form of 'address' is invalid"));

        [Test]
        public void GetDefault()
        {
            var connectionConfig = ConnectionConfig.Default;
            Assert.IsNotNull(connectionConfig);
            Assert.AreEqual("127.0.0.1", connectionConfig.Address);
            Assert.AreEqual(7777, connectionConfig.Port);
            Assert.AreEqual("DefaultKey", connectionConfig.Key);
            Assert.IsNull(connectionConfig.ConnectionData);
            Assert.AreEqual(10, connectionConfig.TimeoutSeconds);
        }

        [Test]
        public void GetDefaultsAreSame()
        {
            var connectionConfigA = ConnectionConfig.Default;
            var connectionConfigB = ConnectionConfig.Default;
            Assert.IsNotNull(connectionConfigA);
            Assert.AreSame(connectionConfigA, connectionConfigB);
        }

        [Test]
        public void UpdateConfigSuccess()
        {
            var connectionConfig = new ConnectionConfig();

            const string address = "127.0.0.2";
            const ushort port = 7776;
            const string key = "NewKey";
            var connectionData = new byte[] { 1, 2, 3, 4 };
            const int timeoutSeconds = 1;
            connectionConfig.UpdateConfig(address, port, key, connectionData, timeoutSeconds);

            Assert.AreEqual(address, connectionConfig.Address);
            Assert.AreEqual(port, connectionConfig.Port);
            Assert.AreEqual(key, connectionConfig.Key);
            Assert.AreEqual(connectionData, connectionConfig.ConnectionData);
            Assert.AreEqual(timeoutSeconds, connectionConfig.TimeoutSeconds);
        }

        [Test]
        public void UpdateConfigWithInvalidIpAddressForm()
        {
            var connectionConfig = new ConnectionConfig();
            Assert.That(() => connectionConfig.UpdateConfig("256:0:0:1"),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("The form of 'address' is invalid"));
        }

        [Test]
        public void UpdateDefaultInstance()
            => Assert.That(() => ConnectionConfig.Default.UpdateConfig(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.EqualTo("Unable to update the config of the default instance"));
    }
}
