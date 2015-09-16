//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    public static class BrokerConstants
    {

        public const int BROKER_REQUEST_ID = 1177;

        public const string BROKER_REQUEST = "com.microsoft.aadbroker.adal.broker.request";

        public const string BROKER_REQUEST_RESUME = "com.microsoft.aadbroker.adal.broker.request.resume";
        
        /**
         * Account type string.
         */
        public const string BROKER_ACCOUNT_TYPE = "com.microsoft.workaccount";

        public const string ACCOUNT_INITIAL_NAME = "aad";

        public const string BACKGROUND_REQUEST_MESSAGE = "background.request";

        public const string ACCOUNT_DEFAULT_NAME = "Default";

        /**
         * Authtoken type string.
         */
        public const string AUTHTOKEN_TYPE = "adal.authtoken.type";

        public const string BROKER_FINAL_URL = "adal.final.url";

        public const string ACCOUNT_INITIAL_REQUEST = "account.initial.request";

        public const string ACCOUNT_CLIENTID_KEY = "account.clientid.key";

        public const string ACCOUNT_CLIENT_SECRET_KEY = "account.client.secret.key";

        public const string ACCOUNT_CORRELATIONID = "account.correlationid";

        public const string ACCOUNT_PROMPT = "account.prompt";

        public const string ACCOUNT_EXTRA_QUERY_PARAM = "account.extra.query.param";

        public const string ACCOUNT_LOGIN_HINT = "account.login.hint";

        public const string ACCOUNT_RESOURCE = "account.resource";

        public const string ACCOUNT_REDIRECT = "account.redirect";

        public const string ACCOUNT_AUTHORITY = "account.authority";

        public const string ACCOUNT_REFRESH_TOKEN = "account.refresh.token";

        public const string ACCOUNT_ACCESS_TOKEN = "account.access.token";

        public const string ACCOUNT_EXPIREDATE = "account.expiredate";

        public const string ACCOUNT_RESULT = "account.result";

        public const string ACCOUNT_REMOVE_TOKENS = "account.remove.tokens";

        public const string ACCOUNT_REMOVE_TOKENS_VALUE = "account.remove.tokens.value";

        public const string MULTI_RESOURCE_TOKEN = "account.multi.resource.token";

        public const string ACCOUNT_NAME = "account.name";
        
        public const string ACCOUNT_IDTOKEN = "account.idtoken";

        public const string ACCOUNT_USERINFO_USERID = "account.userinfo.userid";

        public const string ACCOUNT_USERINFO_GIVEN_NAME = "account.userinfo.given.name";

        public const string ACCOUNT_USERINFO_FAMILY_NAME = "account.userinfo.family.name";

        public const string ACCOUNT_USERINFO_IDENTITY_PROVIDER = "account.userinfo.identity.provider";

        public const string ACCOUNT_USERINFO_USERID_DISPLAYABLE = "account.userinfo.userid.displayable";

        public const string ACCOUNT_USERINFO_TENANTID = "account.userinfo.tenantid";

        public const string ADAL_VERSION_KEY = "adal.version.key";
        
        public const string ACCOUNT_UID_CACHES = "account.uid.caches";

        public const string USERDATA_PREFIX = "userdata.prefix";

        public const string USERDATA_UID_KEY = "calling.uid.key";

        public const string USERDATA_CALLER_CACHEKEYS = "userdata.caller.cachekeys";

        public const string CALLER_CACHEKEY_PREFIX = "|";

        public const string CLIENT_TLS_NOT_SUPPORTED = " PKeyAuth/1.0";

        public const string CHALLANGE_REQUEST_HEADER = "WWW-Authenticate";

        public const string CHALLANGE_RESPONSE_HEADER = "Authorization";

        public const string CHALLANGE_RESPONSE_TYPE = "PKeyAuth";

        public const string CHALLANGE_RESPONSE_TOKEN = "AuthToken";

        public const string CHALLANGE_RESPONSE_CONTEXT = "Context";

        /**
         * Certificate authorities are passed with delimiter.
         */
        public const string CHALLANGE_REQUEST_CERT_AUTH_DELIMETER = ";";

        /**
         * Apk packagename that will install AD-Authenticator. It is used to
         * query if this app installed or not from package manager.
         */
        public const string PackageName = "com.microsoft.windowsintune.companyportal";

        /**
         * Signature info for Intune Company portal app that installs authenticator
         * component.
         */
        //TODO - revert to original signature
        public const string Signature = "IcB5PxIyvbLkbFVtBI/itkW/ejk=";//"1L4Z9FJCgn5c0VLhyAxC5O9LdlE=";
        
        /**
         * Signature info for Azure authenticator app that installs authenticator
         * component.
         */
        public const string AzureAuthenticatorAppSignature = "ho040S3ffZkmxqtQrSwpTVOn9r0=";

        public const string AzureAuthenticatorAppPackageName = "com.azure.authenticator";

        public const string CLIENT_TLS_REDIRECT = "urn:http-auth:PKeyAuth";

        public const string CHALLANGE_TLS_INCAPABLE = "x-ms-PKeyAuth";

        public const string CHALLANGE_TLS_INCAPABLE_VERSION = "1.0";

        public const string REDIRECT_PREFIX = "msauth";

        //public const Object REDIRECT_DELIMETER_ENCODED = "%2C";
        
        public const string BROWSER_EXT_PREFIX = "browser://";
        
        public const string BROWSER_EXT_INSTALL_PREFIX = "msauth://";

        public const string CALLER_INFO_PACKAGE = "caller.info.package";
    }
}