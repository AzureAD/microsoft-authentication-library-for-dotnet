using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class AuthorizationResultTests
    {
        private readonly Dictionary<string, string> _queryParams = new Dictionary<string, string>()
        {
            { OAuth2Parameter.State, "some_state" },
            { TokenResponseClaim.CloudInstanceHost, "cloud" },
            { TokenResponseClaim.ClientInfo, "some client info" },
            { TokenResponseClaim.Code, "some code" },
        };

        [TestMethod]
        public void UrlInErrorDescriptionTest()
        {
            Uri uri = new Uri("http://some_url.com?q=p");
            UriBuilder errorUri = new UriBuilder(TestConstants.AuthorityHomeTenant)
            {
                Query = string.Format(
                           CultureInfo.InvariantCulture,
                           "error={0}&error_description={1}",
                           MsalError.NonHttpsRedirectNotSupported,
                           MsalErrorMessage.NonHttpsRedirectNotSupported + " - " + CoreHelpers.UrlEncode(uri.AbsoluteUri))
            };

            var result = AuthorizationResult.FromUri(errorUri.Uri.AbsoluteUri);
            Assert.AreEqual(MsalErrorMessage.NonHttpsRedirectNotSupported + " - " + uri.AbsoluteUri, result.ErrorDescription);
            Assert.AreEqual(MsalError.NonHttpsRedirectNotSupported, result.Error);
        }

        [TestMethod]
        public void FromUriTest()
        {
            var authResult = AuthorizationResult.FromUri($"https://microsoft.com/auth?{_queryParams.ToQueryParameter()}");

            AssertAuthorizationResult(authResult);
        }

        [TestMethod]
        public void FromPostData()
        {
            var authResult = AuthorizationResult.FromPostData(_queryParams.ToQueryParameter().ToByteArray());

            AssertAuthorizationResult(authResult);
        }

        private void AssertAuthorizationResult(AuthorizationResult actualAuthResult)
        {
            Assert.AreEqual(_queryParams[OAuth2Parameter.State], actualAuthResult.State);
            Assert.AreEqual(_queryParams[TokenResponseClaim.ClientInfo], actualAuthResult.ClientInfo);
            Assert.AreEqual(_queryParams[TokenResponseClaim.CloudInstanceHost], actualAuthResult.CloudInstanceHost);
            Assert.AreEqual(_queryParams[TokenResponseClaim.Code], actualAuthResult.Code);
            Assert.AreEqual(AuthorizationStatus.Success, actualAuthResult.Status);
        }
    }
}
