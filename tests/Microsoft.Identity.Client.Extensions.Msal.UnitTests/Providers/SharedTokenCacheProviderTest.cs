// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
    [TestClass]
    public class SharedTokenCacheProviderTest
    {
        private StorageCreationPropertiesBuilder _builder;

        [TestInitialize]
        public void TestInitialize()
        {
            var tmpFilePath = Path.GetTempFileName();
            _builder = new StorageCreationPropertiesBuilder(
                    Path.GetFileName(tmpFilePath),
                    Path.GetDirectoryName(tmpFilePath),
                    "1234")
                .WithMacKeyChain(serviceName: "testFoo", accountName: "accountName")
                .WithLinuxKeyring(
                    schemaName: "msal.cache",
                    collection: "default",
                    secretLabel: "MSALCache",
                    attribute1: new KeyValuePair<string, string>("MsalClientID", "testFoo"),
                    attribute2: new KeyValuePair<string, string>("MsalClientVersion", "1.0.0.0"));
        }

        [TestMethod]
        [TestCategory("SharedTokenCacheProviderTests")]
        public async Task ShouldNotBeAvailableWithoutIdentitiesAsync()
        {
            var provider = new SharedTokenCacheProvider(_builder);
            Assert.IsFalse(await provider.IsAvailableAsync().ConfigureAwait(false));
        }
    }
}
