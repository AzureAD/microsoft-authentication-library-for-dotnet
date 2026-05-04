// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Handlers;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    public class AuthorityRegistryTests
    {
        #region DetectFromUri

        [TestMethod]
        public void DetectFromUri_AadAuthority_ReturnsAadHandler()
        {
            var uri = new Uri("https://login.microsoftonline.com/common/");
            var handler = AuthorityRegistry.DetectFromUri(uri);
            Assert.AreEqual(AuthorityType.Aad, handler.AuthorityType);
            Assert.IsInstanceOfType(handler, typeof(AadAuthorityHandler));
        }

        [TestMethod]
        public void DetectFromUri_AdfsAuthority_ReturnsAdfsHandler()
        {
            var uri = new Uri("https://someAdfs.com/adfs/");
            var handler = AuthorityRegistry.DetectFromUri(uri);
            Assert.AreEqual(AuthorityType.Adfs, handler.AuthorityType);
            Assert.IsInstanceOfType(handler, typeof(AdfsAuthorityHandler));
        }

        [TestMethod]
        public void DetectFromUri_AdfsAuthority_CaseInsensitive()
        {
            var uri = new Uri("https://someAdfs.com/ADFS/");
            var handler = AuthorityRegistry.DetectFromUri(uri);
            Assert.AreEqual(AuthorityType.Adfs, handler.AuthorityType);
        }

        [TestMethod]
        public void DetectFromUri_B2CAuthority_ReturnsB2CHandler()
        {
            var uri = new Uri("https://mytenant.b2clogin.com/tfp/mytenant.onmicrosoft.com/policy/");
            var handler = AuthorityRegistry.DetectFromUri(uri);
            Assert.AreEqual(AuthorityType.B2C, handler.AuthorityType);
            Assert.IsInstanceOfType(handler, typeof(B2CAuthorityHandler));
        }

        [TestMethod]
        public void DetectFromUri_B2CAuthority_CaseInsensitive()
        {
            var uri = new Uri("https://mytenant.b2clogin.com/TFP/mytenant.onmicrosoft.com/policy/");
            var handler = AuthorityRegistry.DetectFromUri(uri);
            Assert.AreEqual(AuthorityType.B2C, handler.AuthorityType);
        }

        [TestMethod]
        public void DetectFromUri_CiamAuthority_ReturnsCiamHandler()
        {
            var uri = new Uri("https://mytenant.ciamlogin.com/mytenant.onmicrosoft.com/");
            var handler = AuthorityRegistry.DetectFromUri(uri);
            Assert.AreEqual(AuthorityType.Ciam, handler.AuthorityType);
            Assert.IsInstanceOfType(handler, typeof(CiamAuthorityHandler));
        }

        [TestMethod]
        public void DetectFromUri_CiamAuthority_CaseInsensitive()
        {
            var uri = new Uri("https://mytenant.CIAMLOGIN.COM/mytenant.onmicrosoft.com/");
            var handler = AuthorityRegistry.DetectFromUri(uri);
            Assert.AreEqual(AuthorityType.Ciam, handler.AuthorityType);
        }

        [TestMethod]
        public void DetectFromUri_CiamAuthority_SubdomainVariation()
        {
            var uri = new Uri("https://contoso.ciamlogin.com/contoso.onmicrosoft.com/");
            var handler = AuthorityRegistry.DetectFromUri(uri);
            Assert.AreEqual(AuthorityType.Ciam, handler.AuthorityType);
        }

        [TestMethod]
        public void DetectFromUri_DstsAuthority_ReturnsDstsHandler()
        {
            var uri = new Uri("https://dsts.core.azure.net/dstsv2/tenant/");
            var handler = AuthorityRegistry.DetectFromUri(uri);
            Assert.AreEqual(AuthorityType.Dsts, handler.AuthorityType);
            Assert.IsInstanceOfType(handler, typeof(DstsAuthorityHandler));
        }

        [TestMethod]
        public void DetectFromUri_DstsAuthority_CaseInsensitive()
        {
            var uri = new Uri("https://dsts.core.azure.net/DSTSV2/tenant/");
            var handler = AuthorityRegistry.DetectFromUri(uri);
            Assert.AreEqual(AuthorityType.Dsts, handler.AuthorityType);
        }

        #endregion

        #region DetectFromUri ordering — CIAM before AAD

        [TestMethod]
        public void DetectFromUri_CiamUrl_NotDetectedAsAad()
        {
            // CIAM must be detected BEFORE AAD, since AAD is the catch-all
            var ciamUri = new Uri("https://mytenant.ciamlogin.com/mytenant.onmicrosoft.com/");
            var handler = AuthorityRegistry.DetectFromUri(ciamUri);
            Assert.AreNotEqual(AuthorityType.Aad, handler.AuthorityType,
                "A .ciamlogin.com URL must be detected as CIAM, not AAD.");
            Assert.AreEqual(AuthorityType.Ciam, handler.AuthorityType);
        }

        [TestMethod]
        public void DetectFromUri_StandardAadUrl_DetectedAsAad()
        {
            // A normal AAD URL (no special path or host) should be the catch-all AAD
            var aadUri = new Uri("https://login.microsoftonline.com/tenantid/");
            var handler = AuthorityRegistry.DetectFromUri(aadUri);
            Assert.AreEqual(AuthorityType.Aad, handler.AuthorityType);
        }

        #endregion

        #region GetByType

        [TestMethod]
        public void GetByType_Aad_ReturnsAadHandler()
        {
            var handler = AuthorityRegistry.GetByType(AuthorityType.Aad);
            Assert.AreEqual(AuthorityType.Aad, handler.AuthorityType);
            Assert.IsInstanceOfType(handler, typeof(AadAuthorityHandler));
        }

        [TestMethod]
        public void GetByType_Adfs_ReturnsAdfsHandler()
        {
            var handler = AuthorityRegistry.GetByType(AuthorityType.Adfs);
            Assert.AreEqual(AuthorityType.Adfs, handler.AuthorityType);
            Assert.IsInstanceOfType(handler, typeof(AdfsAuthorityHandler));
        }

        [TestMethod]
        public void GetByType_B2C_ReturnsB2CHandler()
        {
            var handler = AuthorityRegistry.GetByType(AuthorityType.B2C);
            Assert.AreEqual(AuthorityType.B2C, handler.AuthorityType);
            Assert.IsInstanceOfType(handler, typeof(B2CAuthorityHandler));
        }

        [TestMethod]
        public void GetByType_Ciam_ReturnsCiamHandler()
        {
            var handler = AuthorityRegistry.GetByType(AuthorityType.Ciam);
            Assert.AreEqual(AuthorityType.Ciam, handler.AuthorityType);
            Assert.IsInstanceOfType(handler, typeof(CiamAuthorityHandler));
        }

        [TestMethod]
        public void GetByType_Dsts_ReturnsDstsHandler()
        {
            var handler = AuthorityRegistry.GetByType(AuthorityType.Dsts);
            Assert.AreEqual(AuthorityType.Dsts, handler.AuthorityType);
            Assert.IsInstanceOfType(handler, typeof(DstsAuthorityHandler));
        }

        [TestMethod]
        public void GetByType_Generic_ReturnsGenericHandler()
        {
            var handler = AuthorityRegistry.GetByType(AuthorityType.Generic);
            Assert.AreEqual(AuthorityType.Generic, handler.AuthorityType);
            Assert.IsInstanceOfType(handler, typeof(GenericAuthorityHandler));
        }

        #endregion

        #region Create — correct Authority subclass

        [TestMethod]
        public void Create_AadAuthorityInfo_ReturnsAadAuthority()
        {
            var info = AuthorityInfo.FromAuthorityUri("https://login.microsoftonline.com/common/", false);
            var authority = AuthorityRegistry.Create(info);
            Assert.IsInstanceOfType(authority, typeof(AadAuthority));
        }

        [TestMethod]
        public void Create_AdfsAuthorityInfo_ReturnsAdfsAuthority()
        {
            var info = AuthorityInfo.FromAdfsAuthority("https://someAdfs.com/adfs/", false);
            var authority = AuthorityRegistry.Create(info);
            Assert.IsInstanceOfType(authority, typeof(AdfsAuthority));
        }

        [TestMethod]
        public void Create_B2CAuthorityInfo_ReturnsB2CAuthority()
        {
            var info = AuthorityInfo.FromB2CAuthority("https://mytenant.b2clogin.com/tfp/mytenant.onmicrosoft.com/policy/");
            var authority = AuthorityRegistry.Create(info);
            Assert.IsInstanceOfType(authority, typeof(B2CAuthority));
        }

        [TestMethod]
        public void Create_CiamAuthorityInfo_ReturnsCiamAuthority()
        {
            var info = AuthorityInfo.FromAuthorityUri("https://mytenant.ciamlogin.com/mytenant.onmicrosoft.com/", false);
            var authority = AuthorityRegistry.Create(info);
            Assert.IsInstanceOfType(authority, typeof(CiamAuthority));
        }

        [TestMethod]
        public void Create_DstsAuthorityInfo_ReturnsDstsAuthority()
        {
            var info = new AuthorityInfo(AuthorityType.Dsts, "https://dsts.core.azure.net/dstsv2/tenant/", false);
            var authority = AuthorityRegistry.Create(info);
            Assert.IsInstanceOfType(authority, typeof(DstsAuthority));
        }

        [TestMethod]
        public void Create_GenericAuthorityInfo_ReturnsGenericAuthority()
        {
            var info = AuthorityInfo.FromGenericAuthority("https://somegenericidp.com/");
            var authority = AuthorityRegistry.Create(info);
            Assert.IsInstanceOfType(authority, typeof(GenericAuthority));
        }

        #endregion

        #region CreateValidator — correct validator type

        [TestMethod]
        public void CreateValidator_AadAuthority_ReturnsAadAuthorityValidator()
        {
            var info = AuthorityInfo.FromAuthorityUri("https://login.microsoftonline.com/common/", false);
            var validator = AuthorityRegistry.CreateValidator(info, null);
            Assert.IsInstanceOfType(validator, typeof(AadAuthorityValidator));
        }

        [TestMethod]
        public void CreateValidator_AdfsAuthority_ReturnsAdfsAuthorityValidator()
        {
            var info = AuthorityInfo.FromAdfsAuthority("https://someAdfs.com/adfs/", false);
            var validator = AuthorityRegistry.CreateValidator(info, null);
            Assert.IsInstanceOfType(validator, typeof(AdfsAuthorityValidator));
        }

        [TestMethod]
        public void CreateValidator_B2CAuthority_ReturnsNullAuthorityValidator()
        {
            var info = AuthorityInfo.FromB2CAuthority("https://mytenant.b2clogin.com/tfp/mytenant.onmicrosoft.com/policy/");
            var validator = AuthorityRegistry.CreateValidator(info, null);
            Assert.IsInstanceOfType(validator, typeof(NullAuthorityValidator));
        }

        [TestMethod]
        public void CreateValidator_CiamAuthority_ReturnsNullAuthorityValidator()
        {
            var info = AuthorityInfo.FromAuthorityUri("https://mytenant.ciamlogin.com/mytenant.onmicrosoft.com/", false);
            var validator = AuthorityRegistry.CreateValidator(info, null);
            Assert.IsInstanceOfType(validator, typeof(NullAuthorityValidator));
        }

        [TestMethod]
        public void CreateValidator_DstsAuthority_ReturnsNullAuthorityValidator()
        {
            var info = new AuthorityInfo(AuthorityType.Dsts, "https://dsts.core.azure.net/dstsv2/tenant/", false);
            var validator = AuthorityRegistry.CreateValidator(info, null);
            Assert.IsInstanceOfType(validator, typeof(NullAuthorityValidator));
        }

        [TestMethod]
        public void CreateValidator_GenericAuthority_ReturnsNullAuthorityValidator()
        {
            var info = AuthorityInfo.FromGenericAuthority("https://somegenericidp.com/");
            var validator = AuthorityRegistry.CreateValidator(info, null);
            Assert.IsInstanceOfType(validator, typeof(NullAuthorityValidator));
        }

        #endregion

        #region GenericAuthorityHandler — never URI-detected

        [TestMethod]
        public void DetectFromUri_GenericNotAutoDetected_FallsBackToAad()
        {
            // Generic authorities are never auto-detected from URI; any unknown URL falls through to AAD catch-all
            var uri = new Uri("https://somegenericidp.com/");
            var handler = AuthorityRegistry.DetectFromUri(uri);
            Assert.AreEqual(AuthorityType.Aad, handler.AuthorityType,
                "A generic URL should fall through to the AAD catch-all handler.");
        }

        [TestMethod]
        public void GenericHandler_CanHandle_AlwaysReturnsFalse()
        {
            var handler = AuthorityRegistry.GetByType(AuthorityType.Generic);
            Assert.IsFalse(handler.CanHandle(new Uri("https://somegenericidp.com/"), "somegenericidp.com", null));
            Assert.IsFalse(handler.CanHandle(new Uri("https://login.microsoftonline.com/common/"), "login.microsoftonline.com", "common"));
            Assert.IsFalse(handler.CanHandle(new Uri("https://mytenant.ciamlogin.com/"), "mytenant.ciamlogin.com", null));
        }

        #endregion
    }

    [TestClass]
    public class AuthorityHandlerResolveTests : TestBase
    {
        // Test: simple handlers return requestAuthorityInfo when provided
        [TestMethod]
        public async Task AdfsHandler_WithRequestAuthority_ReturnsRequestAuthority()
        {
            using var harness = CreateTestHarness();
            var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
            var configInfo = AuthorityInfo.FromAdfsAuthority("https://fs.contoso.com/adfs/", false);
            requestContext.ServiceBundle.Config.Authority = new AdfsAuthority(configInfo);

            var requestInfo = AuthorityInfo.FromAdfsAuthority("https://fs.contoso.com/adfs/", false);
            var configAuthority = Authority.CreateAuthority(configInfo);
            var handler = new AdfsAuthorityHandler();

            var result = await handler.ResolveForRequestAsync(configAuthority, requestInfo, null, requestContext).ConfigureAwait(false);

            Assert.IsInstanceOfType(result, typeof(AdfsAuthority));
            Assert.AreEqual(configInfo.CanonicalAuthority, result.AuthorityInfo.CanonicalAuthority);
        }

        // Test: simple handlers fall back to config when no request authority
        [TestMethod]
        public async Task AdfsHandler_WithNoRequestAuthority_ReturnsConfigAuthority()
        {
            using var harness = CreateTestHarness();
            var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
            var configInfo = AuthorityInfo.FromAdfsAuthority("https://fs.contoso.com/adfs/", false);
            requestContext.ServiceBundle.Config.Authority = new AdfsAuthority(configInfo);

            var configAuthority = Authority.CreateAuthority(configInfo);
            var handler = new AdfsAuthorityHandler();

            var result = await handler.ResolveForRequestAsync(configAuthority, null, null, requestContext).ConfigureAwait(false);

            Assert.IsInstanceOfType(result, typeof(AdfsAuthority));
            Assert.AreEqual(configInfo.CanonicalAuthority, result.AuthorityInfo.CanonicalAuthority);
        }

        // Test: AAD handler falls back to config when no request authority and no account
        [TestMethod]
        public async Task AadHandler_WithNoRequestAuthority_NoAccount_ReturnsConfigAuthority()
        {
            using var harness = CreateTestHarness();
            var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
            var configAuthority = Authority.CreateAuthority("https://login.microsoftonline.com/mytenant/");
            requestContext.ServiceBundle.Config.Authority = configAuthority;
            var handler = new AadAuthorityHandler();

            var result = await handler.ResolveForRequestAsync(configAuthority, null, null, requestContext).ConfigureAwait(false);

            Assert.IsInstanceOfType(result, typeof(AadAuthority));
        }

        // Test: B2C handler returns config authority when no request authority
        [TestMethod]
        public async Task B2CHandler_WithNoRequestAuthority_ReturnsConfigAuthority()
        {
            using var harness = CreateTestHarness();
            var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
            var configInfo = AuthorityInfo.FromB2CAuthority("https://mytenant.b2clogin.com/tfp/mytenant.onmicrosoft.com/policy/");
            requestContext.ServiceBundle.Config.Authority = new B2CAuthority(configInfo);

            var configAuthority = Authority.CreateAuthority(configInfo);
            var handler = new B2CAuthorityHandler();

            var result = await handler.ResolveForRequestAsync(configAuthority, null, null, requestContext).ConfigureAwait(false);

            Assert.IsInstanceOfType(result, typeof(B2CAuthority));
            Assert.AreEqual(configInfo.CanonicalAuthority, result.AuthorityInfo.CanonicalAuthority);
        }

        // Test: registry dispatches ResolveForRequestAsync through the correct handler
        [TestMethod]
        public async Task AuthorityRegistry_ResolveForRequestAsync_DispatchesToCorrectHandler()
        {
            using var harness = CreateTestHarness();
            var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
            var configInfo = AuthorityInfo.FromAdfsAuthority("https://fs.contoso.com/adfs/", false);
            requestContext.ServiceBundle.Config.Authority = new AdfsAuthority(configInfo);
            var configAuthority = requestContext.ServiceBundle.Config.Authority;

            var result = await AuthorityRegistry.ResolveForRequestAsync(
                configAuthority, null, null, requestContext).ConfigureAwait(false);

            Assert.IsInstanceOfType(result, typeof(AdfsAuthority));
        }

        // Test: generic handler falls back to config when no request authority
        [TestMethod]
        public async Task GenericHandler_WithNoRequestAuthority_ReturnsConfigAuthority()
        {
            using var harness = CreateTestHarness();
            var requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid(), null);
            var configInfo = AuthorityInfo.FromGenericAuthority("https://somegenericidp.com/");
            requestContext.ServiceBundle.Config.Authority = new GenericAuthority(configInfo);

            var configAuthority = Authority.CreateAuthority(configInfo);
            var handler = new GenericAuthorityHandler();

            var result = await handler.ResolveForRequestAsync(configAuthority, null, null, requestContext).ConfigureAwait(false);

            Assert.IsInstanceOfType(result, typeof(GenericAuthority));
        }
    }
}
