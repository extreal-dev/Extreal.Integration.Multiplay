using System;
using NUnit.Framework;

namespace Extreal.Integration.Multiplay.NGO.Test
{
    public class NgoConfigTest
    {
        [Test]
        public void NewNgoConfigWithInvalidIpAddressForm()
            => Assert.That(() => _ = new NgoConfig("256:0:0:1"),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("The form of 'address' is invalid"));
    }
}
