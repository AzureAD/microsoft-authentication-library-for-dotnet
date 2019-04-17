// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.CacheV2.Impl;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheV2Tests
{
    [TestClass]
    public class FileSystemCredentialPathManagerTests
    {
        private FileSystemCredentialPathManager _credentialPathManager;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            _credentialPathManager = new FileSystemCredentialPathManager(TestCommon.CreateDefaultServiceBundle().PlatformProxy.CryptographyManager);
        }

        [TestMethod]
        public void ToSafeFilename()
        {
            Assert.AreEqual("98JPIEIUEFT7FFJK", _credentialPathManager.ToSafeFilename("!@#$%^&*()-+"));
            Assert.AreEqual("SEOC8GKOVGE196NR", _credentialPathManager.ToSafeFilename(""));
            Assert.AreEqual("82E183VGAG9CFOF4", _credentialPathManager.ToSafeFilename("=^^="));
            Assert.AreEqual("EOE7CM5P6N5I6EAS", _credentialPathManager.ToSafeFilename("alreadySafeButStill"));
            Assert.AreEqual("EOE7CM5P6N5I6EAS", _credentialPathManager.ToSafeFilename("AlReAdYsAfEbUtStIlL"));
            Assert.AreEqual(
                "EPGP81EH0BA8BLKC",
                _credentialPathManager.ToSafeFilename("================================================"));
        }
    }
}
