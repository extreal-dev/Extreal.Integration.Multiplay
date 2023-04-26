using System;
using NUnit.Framework;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class NgoConfigTest
    {
        [Test]
        public void NewNgoConfigWithAddressNull()
            => Assert.That(() => _ = new NgoConfig(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contains("address"));
    }
}
