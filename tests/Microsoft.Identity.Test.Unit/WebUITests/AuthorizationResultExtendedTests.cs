// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class AuthorizationResultExtendedTests : TestBase
    {
        [TestMethod]
        public void FromUri_NullInput_ReturnsUnknownError()
        {
            var result = AuthorizationResult.FromUri(null);
            Assert.AreEqual(AuthorizationStatus.UnknownError, result.Status);
            Assert.AreEqual(MsalError.AuthenticationFailed, result.Error);
        }

        [TestMethod]
        public void FromUri_EmptyString_ReturnsUnknownError()
        {
            var result = AuthorizationResult.FromUri(string.Empty);
            Assert.AreEqual(AuthorizationStatus.UnknownError, result.Status);
        }

        [TestMethod]
        public void FromUri_WhitespaceString_ReturnsUnknownError()
        {
            var result = AuthorizationResult.FromUri("   ");
            Assert.AreEqual(AuthorizationStatus.UnknownError, result.Status);
        }

        [TestMethod]
        public void FromUri_NoQueryString_ReturnsUnknownError()
        {
            var result = AuthorizationResult.FromUri("https://example.com");
            Assert.AreEqual(AuthorizationStatus.UnknownError, result.Status);
            Assert.AreEqual(MsalError.AuthenticationFailed, result.Error);
        }

        [TestMethod]
        public void FromUri_ErrorWithSubcodeCancel_ReturnsUserCancel()
        {
            var result = AuthorizationResult.FromUri(
                "https://example.com?error=access_denied&error_subcode=cancel");
            Assert.AreEqual(AuthorizationStatus.UserCancel, result.Status);
        }

        [TestMethod]
        public void FromUri_ErrorWithDescription_ReturnsProtocolError()
        {
            var result = AuthorizationResult.FromUri(
                "https://example.com?error=invalid_request&error_description=bad+request");
            Assert.AreEqual(AuthorizationStatus.ProtocolError, result.Status);
            Assert.AreEqual("invalid_request", result.Error);
            Assert.AreEqual("bad request", result.ErrorDescription);
        }

        [TestMethod]
        public void FromUri_ErrorWithoutDescription_HasNullDescription()
        {
            var result = AuthorizationResult.FromUri(
                "https://example.com?error=server_error");
            Assert.AreEqual(AuthorizationStatus.ProtocolError, result.Status);
            Assert.AreEqual("server_error", result.Error);
            Assert.IsNull(result.ErrorDescription);
        }

        [TestMethod]
        public void FromUri_MsauthScheme_UsesFullUrlAsCode()
        {
            string msauthUrl = "msauth://com.example.app?state=somestate";
            var result = AuthorizationResult.FromUri(msauthUrl);
            Assert.AreEqual(AuthorizationStatus.Success, result.Status);
            Assert.AreEqual(msauthUrl, result.Code);
            Assert.AreEqual("somestate", result.State);
        }

        [TestMethod]
        public void FromUri_NoCode_NoMsauth_ReturnsUnknownError()
        {
            var result = AuthorizationResult.FromUri(
                "https://example.com?state=somestate&client_info=info");
            Assert.AreEqual(AuthorizationStatus.UnknownError, result.Status);
            Assert.AreEqual(MsalError.AuthenticationFailed, result.Error);
        }

        [TestMethod]
        public void FromUri_WithAllParameters_Success()
        {
            var result = AuthorizationResult.FromUri(
                "https://example.com?code=authcode&state=mystate&cloud_instance_host_name=login.microsoftonline.com&client_info=clientinfo");
            Assert.AreEqual(AuthorizationStatus.Success, result.Status);
            Assert.AreEqual("authcode", result.Code);
            Assert.AreEqual("mystate", result.State);
            Assert.AreEqual("login.microsoftonline.com", result.CloudInstanceHost);
            Assert.AreEqual("clientinfo", result.ClientInfo);
        }

        [TestMethod]
        public void FromPostData_Null_ReturnsUnknownError()
        {
            var result = AuthorizationResult.FromPostData(null);
            Assert.AreEqual(AuthorizationStatus.UnknownError, result.Status);
        }

        [TestMethod]
        public void FromPostData_WithCode_ReturnsSuccess()
        {
            var postData = System.Text.Encoding.UTF8.GetBytes("code=authcode&state=mystate");
            var result = AuthorizationResult.FromPostData(postData);
            Assert.AreEqual(AuthorizationStatus.Success, result.Status);
            Assert.AreEqual("authcode", result.Code);
            Assert.AreEqual("mystate", result.State);
        }

        [TestMethod]
        public void FromPostData_WithError_ReturnsProtocolError()
        {
            var postData = System.Text.Encoding.UTF8.GetBytes("error=invalid_grant&error_description=expired+token");
            var result = AuthorizationResult.FromPostData(postData);
            Assert.AreEqual(AuthorizationStatus.ProtocolError, result.Status);
            Assert.AreEqual("invalid_grant", result.Error);
        }

        [TestMethod]
        public void FromStatus_Success_Throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => AuthorizationResult.FromStatus(AuthorizationStatus.Success));
        }

        [TestMethod]
        public void FromStatus_UserCancel_SetsErrorFields()
        {
            var result = AuthorizationResult.FromStatus(AuthorizationStatus.UserCancel);
            Assert.AreEqual(AuthorizationStatus.UserCancel, result.Status);
            Assert.AreEqual(MsalError.AuthenticationCanceledError, result.Error);
            Assert.IsNotNull(result.ErrorDescription);
        }

        [TestMethod]
        public void FromStatus_UnknownError_SetsErrorFields()
        {
            var result = AuthorizationResult.FromStatus(AuthorizationStatus.UnknownError);
            Assert.AreEqual(AuthorizationStatus.UnknownError, result.Status);
            Assert.AreEqual(MsalError.UnknownError, result.Error);
        }

        [TestMethod]
        public void FromStatus_ErrorHttp_NoDefaultErrorFields()
        {
            var result = AuthorizationResult.FromStatus(AuthorizationStatus.ErrorHttp);
            Assert.AreEqual(AuthorizationStatus.ErrorHttp, result.Status);
            Assert.IsNull(result.Error);
        }

        [TestMethod]
        public void FromStatus_WithErrorAndDescription_SetsFields()
        {
            var result = AuthorizationResult.FromStatus(
                AuthorizationStatus.ProtocolError, "custom_error", "custom description");
            Assert.AreEqual(AuthorizationStatus.ProtocolError, result.Status);
            Assert.AreEqual("custom_error", result.Error);
            Assert.AreEqual("custom description", result.ErrorDescription);
        }
    }
}
