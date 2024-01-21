// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Microsoft.Identity.Test.Unit.CacheExtension
{
    /// <summary>
    /// These tests mock the cache accessors, i.e. do not write anything to disk / key chain / key ring etc.
    /// </summary>
    [TestClass]
    public class MsalCacheStorageTests
    {
        public static readonly string CacheFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
        private readonly TraceSource _logger = new TraceSource("TestSource", SourceLevels.All);
        private static StorageCreationProperties s_storageCreationProperties;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            var builder = new StorageCreationPropertiesBuilder(
                Path.GetFileName(CacheFilePath),
                Path.GetDirectoryName(CacheFilePath));
            builder = builder.WithMacKeyChain(serviceName: "Microsoft.Developer.IdentityService", accountName: "MSALCache");
            builder = builder.WithLinuxKeyring(
                schemaName: "msal.cache",
                collection: "default",
                secretLabel: "MSALCache",
                attribute1: new KeyValuePair<string, string>("MsalClientID", "Microsoft.Developer.IdentityService"),
                attribute2: new KeyValuePair<string, string>("MsalClientVersion", "1.0.0.0"));
            s_storageCreationProperties = builder.Build();
        }

        [TestMethod]
        public void ReadCanThrowExceptions()
        {
            // Arrange
            var actualLogger = new TraceSourceLogger(_logger);
            var cacheAccessor = NSubstitute.Substitute.For<ICacheAccessor>();
            cacheAccessor.Read().Throws(new InvalidOperationException());
            var storage = new Storage(s_storageCreationProperties, cacheAccessor, actualLogger);

            // Assert
            AssertException.Throws<InvalidOperationException>(
                () => storage.ReadData());
        }

        [TestMethod]
        public void WriteCanThrowExceptions()
        {
            // Arrange
            var actualLogger = new TraceSourceLogger(_logger);
            var cacheAccessor = NSubstitute.Substitute.For<ICacheAccessor>();
            cacheAccessor.WhenForAnyArgs(c => c.Write(null)).Throw(new InvalidOperationException());
            var storage = new Storage(s_storageCreationProperties, cacheAccessor, actualLogger);

            // Assert
            AssertException.Throws<InvalidOperationException>(
                () => storage.WriteData(new byte[0]));
        }

        [TestMethod]
        public void ClearCanThrowExceptions()
        {
            // Arrange
            var actualLogger = new TraceSourceLogger(_logger);
            var cacheAccessor = NSubstitute.Substitute.For<ICacheAccessor>();
            cacheAccessor.WhenForAnyArgs(c => c.Clear()).Throw(new InvalidOperationException());
            var storage = new Storage(s_storageCreationProperties, cacheAccessor, actualLogger);

            // Act
            storage.Clear(ignoreExceptions: true);

            // Assert
            AssertException.Throws<InvalidOperationException>(
                () => storage.Clear(ignoreExceptions: false));
        }

        [TestMethod]
        public void CacheStorageReadCanHandleReadingNull()
        {
            // Arrange
            var cacheAccessor = NSubstitute.Substitute.For<ICacheAccessor>();
            cacheAccessor.Read().Returns((byte[])null);

            var actualLogger = new TraceSourceLogger(_logger);
            var storage = new Storage(s_storageCreationProperties, cacheAccessor, actualLogger);

            // Act
            byte[] result = storage.ReadData();

            // Assert
            Assert.AreEqual(0, result.Length);
        }

      
        // Regression https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/issues/56
        [TestMethod]
        public void CacheStorageCanHandleMultipleExceptionsWhenReading()
        {
            // Arrange
            var stringListener = new TraceStringListener();
            var cacheAccessor = Substitute.For<ICacheAccessor>();
            var exception = new InvalidOperationException("some error");
            cacheAccessor.Read().Throws(exception);
            cacheAccessor.When((x) => x.Clear()).Do(_ => throw exception);
            _logger.Listeners.Add(stringListener);
            var actualLogger = new TraceSourceLogger(_logger);
            var storage = new Storage(s_storageCreationProperties, cacheAccessor, actualLogger);

            // Act
            byte[] result = null;
            try
            {
                 result = storage.ReadData();
            }
            catch (Exception ex)
            {
                // ignore 
                Assert.IsTrue(ex is InvalidOperationException);
            }

            // Assert            
            Assert.IsTrue(stringListener.CurrentLog.Contains("TestSource Error"));
            Assert.IsTrue(stringListener.CurrentLog.Contains("InvalidOperationException"));
            Assert.IsTrue(stringListener.CurrentLog.Contains("some error"));
        }

        [TestMethod]
        public void VerifyPersistenceThrowsInnerExceptions()
        {
            // Arrange
            var actualLogger = new TraceSourceLogger(_logger);
            var cacheAccessor = Substitute.For<ICacheAccessor>();
            cacheAccessor.CreateForPersistenceValidation().Returns(cacheAccessor);
            var exception = new InvalidOperationException("some error");
            var storage = new Storage(s_storageCreationProperties, cacheAccessor, actualLogger);

            cacheAccessor.Read().Throws(exception);

            // Act
            var ex = AssertException.Throws<MsalCachePersistenceException>(
                () => storage.VerifyPersistence());

            // Assert
            Assert.AreEqual(ex.InnerException, exception);
        }

        [TestMethod]
        public void VerifyPersistenceThrowsIfDataReadIsEmpty()
        {
            // Arrange
            var actualLogger = new TraceSourceLogger(_logger);
            var cacheAccessor = Substitute.For<ICacheAccessor>();
            cacheAccessor.CreateForPersistenceValidation().Returns(cacheAccessor);
            var storage = new Storage(s_storageCreationProperties, cacheAccessor, actualLogger);

            // Act
            var ex = AssertException.Throws<MsalCachePersistenceException>(
                () => storage.VerifyPersistence());

            // Assert
            Assert.IsNull(ex.InnerException); // no more details available
        }

        [TestMethod]
        public void VerifyPersistenceThrowsIfDataReadIsDiffrentFromDataWritten()
        {
            // Arrange
            var actualLogger = new TraceSourceLogger(_logger);
            var cacheAccessor = Substitute.For<ICacheAccessor>();
            cacheAccessor.CreateForPersistenceValidation().Returns(cacheAccessor);
            var storage = new Storage(s_storageCreationProperties, cacheAccessor, actualLogger);
            cacheAccessor.Read().Returns(Encoding.UTF8.GetBytes("other_dummy_data"));

            // Act
            var ex = AssertException.Throws<MsalCachePersistenceException>(
                () => storage.VerifyPersistence());

            // Assert
            Assert.IsNull(ex.InnerException); // no more details available
        }

        [TestMethod]
        public void VerifyPersistenceHappyPath()
        {
            // Arrange
            byte[] dummyData = Encoding.UTF8.GetBytes(Storage.PersistenceValidationDummyData);
            var actualLogger = new TraceSourceLogger(_logger);
            var cacheAccessor = Substitute.For<ICacheAccessor>();
            cacheAccessor.CreateForPersistenceValidation().Returns(cacheAccessor);
            var storage = new Storage(s_storageCreationProperties, cacheAccessor, actualLogger);
            cacheAccessor.Read().Returns(dummyData);

            // Act
            storage.VerifyPersistence();

            // Assert
            Received.InOrder(() => {
                cacheAccessor.CreateForPersistenceValidation();
                cacheAccessor.Write(Arg.Any<byte[]>());
                cacheAccessor.Read();
                cacheAccessor.Clear();
            });
        }

        [TestMethod]
        public void UnprotectedOptionMutuallyExclusiveWithOtherOptions()
        {
            var builder = new StorageCreationPropertiesBuilder(
               Path.GetFileName(CacheFilePath),
               Path.GetDirectoryName(CacheFilePath));
            builder = builder.WithMacKeyChain(serviceName: "Microsoft.Developer.IdentityService", accountName: "MSALCache");
            builder.WithUnprotectedFile();

            AssertException.Throws<ArgumentException>(() => builder.Build());

            builder = new StorageCreationPropertiesBuilder(
               Path.GetFileName(CacheFilePath),
               Path.GetDirectoryName(CacheFilePath));
            
            builder = builder.WithLinuxKeyring(
                schemaName: "msal.cache",
                collection: "default",
                secretLabel: "MSALCache",
                attribute1: new KeyValuePair<string, string>("MsalClientID", "Microsoft.Developer.IdentityService"),
                attribute2: new KeyValuePair<string, string>("MsalClientVersion", "1.0.0.0"));
            builder.WithUnprotectedFile();
            AssertException.Throws<ArgumentException>(() => builder.Build());

            builder = new StorageCreationPropertiesBuilder(
              Path.GetFileName(CacheFilePath),
              Path.GetDirectoryName(CacheFilePath));
            builder.WithLinuxUnprotectedFile();
            builder.WithUnprotectedFile();
            
            AssertException.Throws<ArgumentException>(() => builder.Build());

        }
    }
}
