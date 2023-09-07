// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Microsoft.Identity.Test.Unit.CacheExtension
{
    [TestClass]
    public class ImportExportTest 
    {
        public static readonly string CacheFilePath = Path.Combine(
            Path.GetTempPath(),
            Path.GetTempFileName());
        private readonly TraceSource _logger = new TraceSource("TestSource");
        public const string ClientId = "1d18b3b0-251b-4714-a02a-9956cec86c2d";

        private MsalCacheHelper _cacheHelper;

        [TestInitialize]
        public void TestInitialize()
        {
            var storageBuilder = new StorageCreationPropertiesBuilder(
                Path.GetFileName(CacheFilePath),
                Path.GetDirectoryName(CacheFilePath));
            storageBuilder = storageBuilder.WithMacKeyChain(
                serviceName: "Microsoft.Developer.IdentityService.Test",
                accountName: "MSALCacheTest");

            // unit tests run on Linux boxes without LibSecret 
            storageBuilder.WithLinuxUnprotectedFile();

            // 1. Use MSAL to create an instance of the Public Client Application
            var app = PublicClientApplicationBuilder.Create(ClientId)
                .Build();

            // 3. Create the high level MsalCacheHelper based on properties and a logger
            _cacheHelper = MsalCacheHelper.CreateAsync(
                    storageBuilder.Build(),
                    new TraceSource("MSAL.CacheExtension.Test"))
                .GetAwaiter().GetResult();
            
            // 4. Let the cache helper handle MSAL's cache
            _cacheHelper.RegisterCache(app.UserTokenCache);
        }

        [TestMethod]
        public void ImportExport()
        {
            var storageBuilder = new StorageCreationPropertiesBuilder(
                Path.GetFileName(CacheFilePath),
                Path.GetDirectoryName(CacheFilePath));

            storageBuilder = storageBuilder.WithMacKeyChain(
                serviceName: "Microsoft.Developer.IdentityService.Test",
                accountName: "MSALCacheTest");

            // unit tests run on Linux boxes without LibSecret 
            storageBuilder.WithLinuxUnprotectedFile();

            // 1. Use MSAL to create an instance of the Public Client Application
            var app = PublicClientApplicationBuilder.Create(ClientId)
                .Build();

            // 3. Create the high level MsalCacheHelper based on properties and a logger
            _cacheHelper = MsalCacheHelper.CreateAsync(
                    storageBuilder.Build(),
                    new TraceSource("MSAL.CacheExtension.Test"))
                .GetAwaiter().GetResult();

            // 4. Let the cache helper handle MSAL's cache
            _cacheHelper.RegisterCache(app.UserTokenCache);

            // Act
            string dataString = "Hello World";
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataString);
            var result = _cacheHelper.LoadUnencryptedTokenCache();
            Assert.AreEqual(0, result.Length);

            _cacheHelper.SaveUnencryptedTokenCache(dataBytes);
            byte[] actualData = _cacheHelper.LoadUnencryptedTokenCache();

            Assert.AreEqual(dataString, Encoding.UTF8.GetString(actualData));
        }
    }
}
