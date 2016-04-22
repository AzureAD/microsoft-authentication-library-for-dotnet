//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// 
    /// </summary>
    public static class BrokerConstants
    {
        /// <summary>
        /// 
        /// </summary>
        public const int BrokerRequestId = 1177;
        /// <summary>
        /// 
        /// </summary>
        public const string BrokerRequest = "com.microsoft.aadbroker.adal.broker.request";
        /// <summary>
        /// 
        /// </summary>
        public const string BrokerRequestResume = "com.microsoft.aadbroker.adal.broker.request.resume";

        /**
         * Account type string.
         */
        /// <summary>
        /// 
        /// </summary>
        public const string BrokerAccountType = "com.microsoft.workaccount";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountInitialName = "aad";
        /// <summary>
        /// 
        /// </summary>
        public const string BackgroundRequestMessage = "background.request";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountDefaultName = "Default";

        /**
         * Authtoken type string.
         */
        /// <summary>
        /// 
        /// </summary>
        public const string AuthtokenType = "adal.authtoken.type";

        /// <summary>
        /// 
        /// </summary>
        public const string BrokerFinalUrl = "adal.final.url";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountInitialRequest = "account.initial.request";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountClientIdKey = "account.clientid.key";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountClientSecretKey = "account.client.secret.key";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountCorrelationId = "account.correlationid";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountPrompt = "account.prompt";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountExtraQueryParam = "account.extra.query.param";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountLoginHint = "account.login.hint";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountResource = "account.resource";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountRedirect = "account.redirect";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountAuthority = "account.authority";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountRefreshToken = "account.refresh.token";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountAccessToken = "account.access.token";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountExpireDate = "account.expiredate";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountResult = "account.result";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountRemoveTokens = "account.remove.tokens";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountRemoveTokensValue = "account.remove.tokens.value";
        /// <summary>
        /// 
        /// </summary>
        public const string MultiResourceToken = "account.multi.resource.token";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountName = "account.name";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountIdToken = "account.idtoken";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountUserInfoUserId = "account.userinfo.userid";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountUserInfoGivenName = "account.userinfo.given.name";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountUserInfoFamilyName = "account.userinfo.family.name";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountUserInfoIdentityProvider = "account.userinfo.identity.provider";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountUserInfoUserIdDisplayable = "account.userinfo.userid.displayable";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountUserInfoTenantId = "account.userinfo.tenantid";
        /// <summary>
        /// 
        /// </summary>
        public const string AdalVersionKey = "adal.version.key";
        /// <summary>
        /// 
        /// </summary>
        public const string AccountUidCaches = "account.uid.caches";
        /// <summary>
        /// 
        /// </summary>
        public const string UserdataPrefix = "userdata.prefix";
        /// <summary>
        /// 
        /// </summary>
        public const string UserdataUidKey = "calling.uid.key";
        /// <summary>
        /// 
        /// </summary>
        public const string UserdataCallerCachekeys = "userdata.caller.cachekeys";
        /// <summary>
        /// 
        /// </summary>
        public const string CallerCachekeyPrefix = "|";
        /// <summary>
        /// 
        /// </summary>
        public const string ClientTlsNotSupported = " PKeyAuth/1.0";
        /// <summary>
        /// 
        /// </summary>
        public const string ChallangeRequestHeader = "WWW-Authenticate";
        /// <summary>
        /// 
        /// </summary>
        public const string ChallangeResponseHeader = "Authorization";
        /// <summary>
        /// 
        /// </summary>
        public const string ChallangeResponseType = "PKeyAuth";
        /// <summary>
        /// 
        /// </summary>
        public const string ChallangeResponseToken = "AuthToken";
        /// <summary>
        /// 
        /// </summary>
        public const string ChallangeResponseContext = "Context";
        /// <summary>
        /// 
        /// </summary>
        public const string ChallangeResponseVersion = "Version";

        /**
         * Certificate authorities are passed with delimiter.
         */
        /// <summary>
        /// 
        /// </summary>
        public const string ChallangeRequestCertAuthDelimeter = ";";

        /**
         * Apk packagename that will install AD-Authenticator. It is used to
         * query if this app installed or not from package manager.
         */
        /// <summary>
        /// 
        /// </summary>
        public const string PackageName = "com.microsoft.windowsintune.companyportal";

        /**
         * Signature info for Intune Company portal app that installs authenticator
         * component.
         */
        /// <summary>
        /// 
        /// </summary>
        public const string Signature = "1L4Z9FJCgn5c0VLhyAxC5O9LdlE=";

        /**
         * Signature info for Azure authenticator app that installs authenticator
         * component.
         */
        /// <summary>
        /// 
        /// </summary>
        public const string AzureAuthenticatorAppSignature = "ho040S3ffZkmxqtQrSwpTVOn9r0=";
        /// <summary>
        /// 
        /// </summary>
        public const string AzureAuthenticatorAppPackageName = "com.azure.authenticator";
        /// <summary>
        /// 
        /// </summary>
        public const string PKeyAuthRedirect = "urn:http-auth:PKeyAuth";
        /// <summary>
        /// 
        /// </summary>
        public const string ChallangeTlsIncapable = "x-ms-PKeyAuth";
        /// <summary>
        /// 
        /// </summary>
        public const string ChallangeTlsIncapableVersion = "1.0";
        /// <summary>
        /// 
        /// </summary>
        public const string RedirectPrefix = "msauth";

        //public const Object REDIRECT_DELIMETER_ENCODED = "%2C";
        /// <summary>
        /// 
        /// </summary>
        public const string BrowserExtPrefix = "browser://";
        /// <summary>
        /// 
        /// </summary>
        public const string BrowserExtInstallPrefix = "msauth://";
        /// <summary>
        /// 
        /// </summary>
        public const string CallerInfoPackage = "caller.info.package";
    }
}
