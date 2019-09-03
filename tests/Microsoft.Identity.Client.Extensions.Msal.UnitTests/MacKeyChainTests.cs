// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Identity.Client.Extensions.Msal.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit.Sdk;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    [TestClass]
    public class MacKeyChainTests
    {
        private const string ServiceName = "foo";
        private const string AccountName = "bar";

        [TestInitialize]
        public void TestInitialize()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                MacKeyChain.DeleteKey(ServiceName, AccountName);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                MacKeyChain.DeleteKey(ServiceName, AccountName);
            }
        }

        [RunOnOSX]
        public void TestWriteKey()
        {
            string data = "applesauce";

            MacKeyChain.WriteKey(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);
        }

        [RunOnOSX]
        public void TestWriteSameKeyTwice()
        {
            string data = "applesauce";

            MacKeyChain.WriteKey(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);

            MacKeyChain.WriteKey(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);
        }

        [RunOnOSX]
        public void TestWriteSameKeyTwiceWithDifferentData()
        {
            string data = "applesauce";
            MacKeyChain.WriteKey(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);

            data = "tomatosauce";
            MacKeyChain.WriteKey(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);
        }

        [RunOnOSX]
        public void TestRetrieveKey()
        {
            string data = "applesauce";

            MacKeyChain.WriteKey(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
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

            MacKeyChain.WriteKey(ServiceName, AccountName, Encoding.UTF8.GetBytes(data));
            VerifyKey(ServiceName, AccountName, expectedData: data);

            MacKeyChain.DeleteKey(ServiceName, AccountName);
            VerifyKeyIsNull(ServiceName, AccountName);
        }

        [RunOnOSX]
        public void TestDeleteNonExistingKey()
        {
            MacKeyChain.DeleteKey(ServiceName, AccountName);
        }

        private static void VerifyKey(string serviceName, string accountName, string expectedData)
        {
            string keychainData = Encoding.UTF8.GetString(MacKeyChain.RetrieveKey(serviceName, accountName));

            if (!keychainData.Equals(expectedData))
            {
#pragma warning disable CA2201 // Do not raise reserved exception types
                throw new Exception(string.Format(CultureInfo.CurrentCulture, "keychainData=\"{0}\" doesn't match expected data=\"{1}\"", keychainData, expectedData));
#pragma warning restore CA2201 // Do not raise reserved exception types
            }
        }

        private static void VerifyKeyIsNull(string serviceName, string accountName)
        {
            if (MacKeyChain.RetrieveKey(serviceName, accountName) != null)
            {
#pragma warning disable CA2201 // Do not raise reserved exception types
                throw new Exception(string.Format(CultureInfo.CurrentCulture, "key exists when it shouldn't be. keychainData=\"{0}\"", Encoding.UTF8.GetString(MacKeyChain.RetrieveKey(serviceName, accountName))));
#pragma warning restore CA2201 // Do not raise reserved exception types
            }
        }
    }
}
