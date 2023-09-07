// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheExtension
{
    [TestClass]
    public class MacKeyChainTests
    {
        private const string ServiceName = "foo";
        private const string AccountName = "bar";
        private const string TestNamespace = "msal-test";

        MacOSKeychain _macOSKeychain;
        [TestInitialize]
        public void TestInitialize()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _macOSKeychain = new MacOSKeychain();
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _macOSKeychain.Remove(ServiceName, AccountName);
            }
        }

        [RunOnOSX]
        public void TestWriteKey()
        {
            string data = "applesauce";

            _macOSKeychain.AddOrUpdate(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);
        }

        [RunOnOSX]
        public void TestWriteSameKeyTwice()
        {
            string data = "applesauce";

            _macOSKeychain.AddOrUpdate(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);

            _macOSKeychain.AddOrUpdate(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);
        }

        [RunOnOSX]
        public void TestWriteSameKeyTwiceWithDifferentData()
        {
            string data = "applesauce";
            _macOSKeychain.AddOrUpdate(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);

            data = "tomatosauce";
            _macOSKeychain.AddOrUpdate(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);
        }

        [RunOnOSX]
        public void TestRetrieveKey()
        {
            string data = "applesauce";

            _macOSKeychain.AddOrUpdate(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);
        }

        [RunOnOSX]
        public void TestRetrieveNonExistingKey()
        {
            VerifyKeyIsNull(ServiceName, AccountName);
        }

        [RunOnOSX]
        public void TestDeleteKey()
        {
            string data = "applesauce";

            _macOSKeychain.AddOrUpdate(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);

            _macOSKeychain.Remove(ServiceName, AccountName);
            VerifyKeyIsNull(ServiceName, AccountName);
        }

        [RunOnOSX]
        public void TestDeleteNonExistingKey()
        {
            _macOSKeychain.Remove(ServiceName, AccountName);
        }

        [RunOnOSX]
        public void MacOSKeychain_Get_NotFound_ReturnsNull()
        {
            var keychain = new MacOSKeychain(TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            var credential = keychain.Get(service, account: null);
            Assert.IsNull(credential);
        }

        [RunOnOSX]
        public void MacOSKeychain_ReadWriteDelete()
        {
            var keychain = new MacOSKeychain(TestNamespace);

            // Create a service that is guaranteed to be unique
            string service = $"https://example.com/{Guid.NewGuid():N}";
            const string account = "john.doe";
            const string password = "letmein123"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInline", Justification="Fake credential")]

            try
            {
                // Write
                keychain.AddOrUpdate(service, account, Encoding.UTF8.GetBytes(password));

                // Read
                var outCredential = keychain.Get(service, account);
                var stringPassword = Encoding.UTF8.GetString(outCredential.Password);

                Assert.IsNotNull(outCredential);
                Assert.AreEqual(account, outCredential.Account);
                Assert.AreEqual(password, stringPassword);
            }
            finally
            {
                // Ensure we clean up after ourselves even in case of 'get' failures
                keychain.Remove(service, account);
            }
        }

        [RunOnOSX]
        public void MacOSKeychain_Remove_NotFound_ReturnsFalse()
        {
            var keychain = new MacOSKeychain(TestNamespace);

            // Unique service; guaranteed not to exist!
            string service = $"https://example.com/{Guid.NewGuid():N}";

            bool result = keychain.Remove(service, account: null);
            Assert.IsFalse(result);
        }

        private void VerifyKey(string serviceName, string accountName, string expectedData)
        {
            var entry  = _macOSKeychain.Get(serviceName, accountName);
            Assert.AreEqual(expectedData, Encoding.UTF8.GetString(entry.Password));
        }

        private void VerifyKeyIsNull(string serviceName, string accountName)
        {
            if (_macOSKeychain.Get(serviceName, accountName) != null)
            {
                Assert.Fail(
                    "key exists when it shouldn't be. keychainData=\"{0}\"",
                    _macOSKeychain.Get(serviceName, accountName).Password);
            }
        }
    }
}
