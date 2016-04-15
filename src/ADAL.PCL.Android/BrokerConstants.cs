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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class BrokerConstants
    {

        public const int BrokerRequestId = 1177;

        public const string BrokerRequest = "com.microsoft.aadbroker.adal.broker.request";

        public const string BrokerRequestResume = "com.microsoft.aadbroker.adal.broker.request.resume";
        
        /**
         * Account type string.
         */
        public const string BrokerAccountType = "com.microsoft.workaccount";

        public const string AccountInitialName = "aad";

        public const string BackgroundRequestMessage = "background.request";

        public const string AccountDefaultName = "Default";

        /**
         * Authtoken type string.
         */
        public const string AuthtokenType = "adal.authtoken.type";

        public const string BrokerFinalUrl = "adal.final.url";

        public const string AccountInitialRequest = "account.initial.request";

        public const string AccountClientIdKey = "account.clientid.key";

        public const string AccountClientSecretKey = "account.client.secret.key";

        public const string AccountCorrelationId = "account.correlationid";

        public const string AccountPrompt = "account.prompt";

        public const string AccountExtraQueryParam = "account.extra.query.param";

        public const string AccountLoginHint = "account.login.hint";

        public const string AccountResource = "account.resource";

        public const string AccountRedirect = "account.redirect";

        public const string AccountAuthority = "account.authority";

        public const string AccountRefreshToken = "account.refresh.token";

        public const string AccountAccessToken = "account.access.token";

        public const string AccountExpireDate = "account.expiredate";

        public const string AccountResult = "account.result";

        public const string AccountRemoveTokens = "account.remove.tokens";

        public const string AccountRemoveTokensValue = "account.remove.tokens.value";

        public const string MultiResourceToken = "account.multi.resource.token";

        public const string AccountName = "account.name";
        
        public const string AccountIdToken = "account.idtoken";

        public const string AccountUserInfoUserId = "account.userinfo.userid";

        public const string AccountUserInfoGivenName = "account.userinfo.given.name";

        public const string AccountUserInfoFamilyName = "account.userinfo.family.name";

        public const string AccountUserInfoIdentityProvider = "account.userinfo.identity.provider";

        public const string AccountUserInfoUserIdDisplayable = "account.userinfo.userid.displayable";

        public const string AccountUserInfoTenantId = "account.userinfo.tenantid";

        public const string AdalVersionKey = "adal.version.key";
        
        public const string AccountUidCaches = "account.uid.caches";

        public const string UserdataPrefix = "userdata.prefix";

        public const string UserdataUidKey = "calling.uid.key";

        public const string UserdataCallerCachekeys = "userdata.caller.cachekeys";

        public const string CallerCachekeyPrefix = "|";

        public const string ClientTlsNotSupported = " PKeyAuth/1.0";

        public const string ChallangeRequestHeader = "WWW-Authenticate";

        public const string ChallangeResponseHeader = "Authorization";

        public const string ChallangeResponseType = "PKeyAuth";

        public const string ChallangeResponseToken = "AuthToken";

        public const string ChallangeResponseContext = "Context";

        public const string ChallangeResponseVersion = "Version";

        public const string ResponseErrorCode = "com.microsoft.aad.adal:BrowserErrorCode";
        public const string ResponseErrorMessage = "com.microsoft.aad.adal:BrowserErrorMessage";


        /**
         * Certificate authorities are passed with delimiter.
         */
        public const string ChallangeRequestCertAuthDelimeter = ";";

        /**
         * Apk packagename that will install AD-Authenticator. It is used to
         * query if this app installed or not from package manager.
         */
        public const string PackageName = "com.microsoft.windowsintune.companyportal";

        /**
         * Signature info for Intune Company portal app that installs authenticator
         * component.
         */
        public const string Signature = "1L4Z9FJCgn5c0VLhyAxC5O9LdlE=";
        
        /**
         * Signature info for Azure authenticator app that installs authenticator
         * component.
         */
        public const string AzureAuthenticatorAppSignature = "ho040S3ffZkmxqtQrSwpTVOn9r0=";

        public const string AzureAuthenticatorAppPackageName = "com.azure.authenticator";

        public const string ClientTlsRedirect = "urn:http-auth:PKeyAuth";

        public const string ChallangeTlsIncapable = "x-ms-PKeyAuth";

        public const string ChallangeTlsIncapableVersion = "1.0";

        public const string RedirectPrefix = "msauth";

        //public const Object REDIRECT_DELIMETER_ENCODED = "%2C";
        
        public const string BrowserExtPrefix = "browser://";
        
        public const string BrowserExtInstallPrefix = "msauth://";

        public const string CallerInfoPackage = "caller.info.package";
    }
}
