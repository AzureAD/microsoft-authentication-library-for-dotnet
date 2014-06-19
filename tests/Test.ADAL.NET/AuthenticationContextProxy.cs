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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Test.ADAL.NET.Friend;
using Timer = System.Timers.Timer;

namespace Test.ADAL.Common
{
    public enum PageType
    {
        Dashboard,
        DashboardResponse,
        Wab,
        WabError,
        Unknown
    }

    internal partial class AuthenticationContextProxy
    {
        private const string NotSpecified = "NotSpecified";

        private readonly UserIdentifier NotSpecifiedUserId = new UserIdentifier(NotSpecified, UserIdentifierType.UniqueId);

        private static string userName;
        private static string password;
        private static SecureString securePassword;

        public AuthenticationContextProxy(string authority, bool validateAuthority, TokenCacheStoreType tokenCacheStoreType)
        {
            TokenCache tokenCache = null;
            if (tokenCacheStoreType == TokenCacheStoreType.InMemory)
            {
                tokenCache = new TokenCache();
            }

            this.context = new AuthenticationContext(authority, validateAuthority, tokenCache);
            this.context.CorrelationId = new Guid(FixedCorrelationId);
        }

        public static bool CallSync { get; set; }

        public static void InitializeTest()
        {
            ClearDefaultCache();
        }

        public static void ClearDefaultCache()
        {
            var dummyContext = new AuthenticationContext("https://dummy/dummy", false);
            dummyContext.TokenCache.Clear();
        }

        public static void SetEnvironmentVariable(string environmentVariable, string environmentVariableValue)
        {
            Environment.SetEnvironmentVariable(environmentVariable, environmentVariableValue);
        }

        public static void SetCredentials(string userNameIn, string passwordIn)
        {
            userName = userNameIn;
            password = passwordIn;
        }

        public static void SetSecureCredentials(string userNameIn, SecureString passwordIn)
        {
            userName = userNameIn;
            securePassword = passwordIn;
        }


        public static SecureString convertToSecureString(string strPassword)
        {
            var secureStr = new SecureString();
            if (strPassword.Length > 0)
            {
                foreach (var c in strPassword.ToCharArray()) secureStr.AppendChar(c);
            }
            return secureStr;
        }

        public static void Delay(int sleepMilliSeconds)
        {
            if (RecorderSettings.Mode == RecorderMode.Record || !RecorderSettings.Mock)
            {
                Thread.Sleep(sleepMilliSeconds);
            }
        }

        public void SetCorrelationId(Guid correlationId)
        {
            this.context.CorrelationId = correlationId;
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, ClientCredential credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireToken(resource, credential));
            
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, credential));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, ClientAssertionCertificateProxy credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireToken(resource, (credential != null) ? credential.Certificate : null));
            
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, (credential != null) ? credential.Certificate : null));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, ClientAssertion credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireToken(resource, credential));

            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, credential));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string clientId, UserCredentialProxy credential)
        {
            if (CallSync)
            { 
                return RunTask(() => this.context.AcquireToken(resource, clientId, 
                    (credential.Password == null) ? 
                    new UserCredential(credential.UserId) :
                    new UserCredential(credential.UserId, credential.Password)));
            }

            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, clientId,
                (credential.Password == null) ?
                new UserCredential(credential.UserId) :
                new UserCredential(credential.UserId, credential.Password)));
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri)
        {
            return RunTaskInteractive(resource, clientId, redirectUri, PromptBehaviorProxy.NotSpecified, NotSpecifiedUserId, NotSpecified);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, UserIdentifier userId)
        {
            return RunTaskInteractive(resource, clientId, redirectUri, PromptBehaviorProxy.NotSpecified, userId, NotSpecified);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, UserIdentifier userId, string extraQueryParameters)
        {
            return RunTaskInteractive(resource, clientId, redirectUri, PromptBehaviorProxy.NotSpecified, userId, extraQueryParameters);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehaviorProxy promptBehavior)
        {
            return RunTaskInteractive(resource, clientId, redirectUri, promptBehavior, NotSpecifiedUserId, NotSpecified);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, UserIdentifier userId, PromptBehaviorProxy promptBehavior)
        {
            return RunTaskInteractive(resource, clientId, redirectUri, promptBehavior, userId, NotSpecified);
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByRefreshToken(refreshToken, clientId));

            return await RunTaskAsync(this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, string resource)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByRefreshToken(refreshToken, clientId, resource));

            return await RunTaskAsync(this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId, resource));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientCredential credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByRefreshToken(refreshToken, clientId, credential));

            return await RunTaskAsync(this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId, credential));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientAssertionCertificateProxy credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByRefreshToken(refreshToken, clientId, (credential != null) ? credential.Certificate : null));

            return await RunTaskAsync(this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId, (credential != null) ? credential.Certificate : null));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientAssertionCertificateProxy credential, string resource)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByRefreshToken(refreshToken, clientId, (credential != null) ? credential.Certificate : null, resource));

            return await RunTaskAsync(this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId, (credential != null) ? credential.Certificate : null, resource));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientAssertion credential, string resource)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByRefreshToken(refreshToken, clientId, credential, resource));

            return await RunTaskAsync(this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId, credential, resource));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientCredential credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByAuthorizationCode(authorizationCode, redirectUri, credential));

            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, credential));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertionCertificateProxy credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByAuthorizationCode(authorizationCode, redirectUri, (credential != null) ? credential.Certificate : null));

            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, (credential != null) ? credential.Certificate : null));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertionCertificateProxy credential, string resource)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByAuthorizationCode(authorizationCode, redirectUri, (credential != null) ? credential.Certificate : null, resource));

            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, (credential != null) ? credential.Certificate : null, resource));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertion credential, string resource)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByAuthorizationCode(authorizationCode, redirectUri, credential, resource));

            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, credential, resource));
        }

        public string AcquireAccessCode(string resource, string clientId, Uri redirectUri, UserIdentifier userId)
        {
            return (RunTaskInteractive(resource, clientId, redirectUri, PromptBehaviorProxy.AccessCodeOnly, userId, NotSpecified)).AccessToken;
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string userAssertion, ClientCredential clientCredential)
        {
            if (CallSync)
            {
                return RunTask(() => this.context.AcquireToken(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), clientCredential));
            }

            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), clientCredential));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string userAssertion, ClientAssertionCertificateProxy clientCertificate)
        {
            if (CallSync)
            {
                return RunTask(() => this.context.AcquireToken(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), (clientCertificate == null) ? null : clientCertificate.Certificate));                
            }

            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), (clientCertificate == null) ? null : clientCertificate.Certificate));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string userAssertion, ClientAssertion clientAssertion)
        {
            if (CallSync)
            {
                return RunTask(() => this.context.AcquireToken(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), clientAssertion));
            }

            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), clientAssertion));
        }

        private async Task<AuthenticationResultProxy> RunTaskAsync(Task<AuthenticationResult> task)
        {
            AuthenticationResultProxy resultProxy;

            try
            {
                AuthenticationResult result = await task;
                resultProxy = GetAuthenticationResultProxy(result);
            }
            catch (Exception ex)
            {
                resultProxy = GetAuthenticationResultProxy(ex);            
            }

            return resultProxy;
        }

        private AuthenticationResultProxy RunTask(Func<AuthenticationResult> func)
        {
            AuthenticationResultProxy resultProxy;

            try
            {
                AuthenticationResult result = func();
                resultProxy = GetAuthenticationResultProxy(result);
            }
            catch (Exception ex)
            {
                resultProxy = GetAuthenticationResultProxy(ex);
            }

            return resultProxy;
        }

        private AuthenticationResultProxy RunTaskInteractive(string resource, string clientId, Uri redirectUri, PromptBehaviorProxy promptBehaviorProxy, UserIdentifier userId, string extraQueryParameters, int retryCount = 0)
        {
            AuthenticationResultProxy resultProxy;

            try
            {
                AuthenticationResult result = null;
                using (Timer abortTest = new Timer(10 * 1000)) // 10 seconds for test execution
                {
                    using (Timer uiSupply = new Timer(250))
                    {
                        if (userName != null || password != null)
                        {
                            uiSupply.Elapsed += UiSupplyEventHandler;
                        }

                        abortTest.Elapsed += (sender, e) => UiAbortEventHandler(sender, e, uiSupply);

                        uiSupply.Start();
                        abortTest.Start();

                        if (promptBehaviorProxy == PromptBehaviorProxy.AccessCodeOnly)
                        {
                            string authorizationCode = AdalFriend.AcquireAccessCode(this.context, resource, clientId,
                                redirectUri, userId);
                            return new AuthenticationResultProxy() {AccessToken = authorizationCode};
                        }

                        PromptBehavior promptBehavior = (promptBehaviorProxy == PromptBehaviorProxy.RefreshSession)
                            ? PromptBehavior.RefreshSession
                            : (promptBehaviorProxy == PromptBehaviorProxy.Always)
                                ? PromptBehavior.Always
                                : (promptBehaviorProxy == PromptBehaviorProxy.Never)
                                    ? PromptBehavior.Never
                                    : PromptBehavior.Auto;

                        if (userId != null && userId.Id == NotSpecified)
                        {
                            if (promptBehaviorProxy == PromptBehaviorProxy.NotSpecified)
                            {
                                result = context.AcquireToken(resource, clientId, redirectUri);
                            }
                            else if (extraQueryParameters == NotSpecified)
                            {
                                result = context.AcquireToken(resource, clientId, redirectUri, promptBehavior);
                            }
                            else
                            {
                                result = context.AcquireToken(resource, clientId, redirectUri, promptBehavior,
                                    extraQueryParameters);
                            }
                        }
                        else
                        {
                            if (promptBehaviorProxy == PromptBehaviorProxy.NotSpecified)
                            {
                                if (extraQueryParameters == NotSpecified)
                                {
                                    result = context.AcquireToken(resource, clientId, redirectUri, userId);
                                }
                                else
                                {
                                    result = context.AcquireToken(resource, clientId, redirectUri, userId,
                                        extraQueryParameters);
                                }
                            }
                            else if (extraQueryParameters == NotSpecified)
                            {
                                result = context.AcquireToken(resource, clientId, redirectUri, userId, promptBehavior);
                            }
                            else
                            {
                                result = context.AcquireToken(resource, clientId, redirectUri, userId, promptBehavior,
                                    extraQueryParameters);
                            }
                        }

                        abortTest.Stop();
                        uiSupply.Stop();
                    }
                }

                resultProxy = GetAuthenticationResultProxy(result);
            }
            catch (Exception ex)
            {
                resultProxy = GetAuthenticationResultProxy(ex);
                if (resultProxy.ExceptionStatusCode == 503 && retryCount < 5)
                {
                    Thread.Sleep(3000);
                    Log.Comment(string.Format("Retry #{0}...", retryCount + 1));
                    return RunTaskInteractive(resource, clientId, redirectUri, promptBehaviorProxy, userId, extraQueryParameters, retryCount + 1);
                }
            }

            return resultProxy;
        }

        public delegate void UiSupplyDelegate(WindowsFormsWebAuthenticationDialog dialog);

        private void UiSupplyEventHandler(object sender, ElapsedEventArgs e)
        {
            WindowsFormsWebAuthenticationDialog webAuthenticationDialog = this.GetWebAuthenticationDialog(5000);
            webAuthenticationDialog.BeginInvoke(new UiSupplyDelegate(UiSupply), webAuthenticationDialog);
        }

        private void UiSupply(WindowsFormsWebAuthenticationDialog webAuthenticationDialog)
        {
            if (webAuthenticationDialog != null)
            {
                WebBrowser webBrowser = ((WindowsFormsWebAuthenticationDialog)webAuthenticationDialog).WebBrowser;
                DialogHandler handler = new DialogHandler();
                handler.EnterInput(userName, password);

                UISupplier supplier = new UISupplier();
                UISupplier.Results result = supplier.SupplyUIStep(webBrowser, userName, password);
                if (result == UISupplier.Results.Error)
                {
                    ((Form)webBrowser.Parent.Parent).Close();
                }
            }
        }

        private void UiAbortEventHandler(object sender, ElapsedEventArgs e, Timer uiSupply)
        {
            Log.Comment("Test execution timeout");
            WindowsFormsWebAuthenticationDialog webAuthenticationDialog = this.GetWebAuthenticationDialog(1000);
            if (webAuthenticationDialog != null)
            {
                ((Form)((WindowsFormsWebAuthenticationDialog)webAuthenticationDialog).WebBrowser.Parent.Parent).Close();
            }

            uiSupply.Stop();
        }

        private WindowsFormsWebAuthenticationDialog GetWebAuthenticationDialog(int totalWaitMilliseconds)
        {
            WindowsFormsWebAuthenticationDialog webAuthenticationDialog = null;
            const int EachWaitMilliseconds = 200;
            do
            {
                try
                {
                    webAuthenticationDialog = Enumerable.OfType<WindowsFormsWebAuthenticationDialog>(Application.OpenForms).Single();
                }
                catch (InvalidOperationException)
                {
                    Verify.Fail("Unable to find auth dialog");
                }

                if (webAuthenticationDialog == null)
                {
                    Thread.Sleep(EachWaitMilliseconds);
                    totalWaitMilliseconds -= EachWaitMilliseconds;
                }
            }
            while (totalWaitMilliseconds > 0 && webAuthenticationDialog == null);

            return webAuthenticationDialog;
        }

        private static AuthenticationResultProxy GetAuthenticationResultProxy(AuthenticationResult result)
        {
            return new AuthenticationResultProxy
            {
                Status = AuthenticationStatusProxy.Success,
                AccessToken = result.AccessToken,
                AccessTokenType = result.AccessTokenType,
                ExpiresOn = result.ExpiresOn,
                IsMultipleResourceRefreshToken = result.IsMultipleResourceRefreshToken,
                RefreshToken = result.RefreshToken,
                IdToken = result.IdToken,
                TenantId = result.TenantId,
                UserInfo = result.UserInfo
            };
        }

        private static AuthenticationResultProxy GetAuthenticationResultProxy(Exception ex)
        {
            var output = new AuthenticationResultProxy
            {
                ErrorDescription = ex.Message,
            };

            output.Status = AuthenticationStatusProxy.ClientError;
            if (ex is ArgumentNullException)
            {
                output.Error = AdalError.InvalidArgument;
            }
            else if (ex is ArgumentException)
            {
                output.Error = AdalError.InvalidArgument;
            }
            else if (ex is AdalServiceException)
            {
                output.Error = ((AdalServiceException)ex).ErrorCode;
                output.ExceptionStatusCode = ((AdalServiceException)ex).StatusCode;
                output.Status = AuthenticationStatusProxy.ServiceError;
            }
            else if (ex is AdalException)
            {
                output.Error = ((AdalException)ex).ErrorCode;
            }
            else
            {
                output.Error = AdalError.AuthenticationFailed;
            }

            output.Exception = ex;

            return output;
        }
    }
}
