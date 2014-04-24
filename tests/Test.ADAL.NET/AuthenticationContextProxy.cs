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

namespace Test.ADAL.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;

    using Test.ADAL.NET.Friend;

    using Timer = System.Timers.Timer;
    using System.Timers;

    public enum PageType
    {
        Dashboard,
        DashboardResponse,
        Wab,
        WabError,
        Unknown
    }

    internal class AuthenticationContextProxy
    {
        private const string NotSpecified = "NotSpecified";

        private static string userName;
        private static string password;

        private readonly AuthenticationContext context;

        private const string FixedCorrelationId = "2ddbba59-1a04-43fb-b363-7fb0ae785030";

        public AuthenticationContextProxy(string authority)
        {
            this.context = new AuthenticationContext(authority);
            this.context.CorrelationId = new Guid(FixedCorrelationId);
        }

        public AuthenticationContextProxy(string authority, bool validateAuthority)
        {
            this.context = new AuthenticationContext(authority, validateAuthority);
            this.context.CorrelationId = new Guid(FixedCorrelationId);
        }

        public AuthenticationContextProxy(string authority, bool validateAuthority, TokenCacheStoreType tokenCacheStoreType)
        {
            IDictionary<TokenCacheKey, string> tokenCacheStore = null;
            if (tokenCacheStoreType == TokenCacheStoreType.InMemory)
            {
                tokenCacheStore = new Dictionary<TokenCacheKey, string>();
            }
            else if (tokenCacheStoreType == TokenCacheStoreType.ShortLived)
            {
                tokenCacheStore = new ShortLivedTokenCache();
            }

            this.context = new AuthenticationContext(authority, validateAuthority, tokenCacheStore);
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
            dummyContext.TokenCacheStore.Clear();
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

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, ClientCredentialProxy credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireToken(resource, (credential != null) ? credential.Credential : null));
            
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, (credential != null) ? credential.Credential : null));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, X509CertificateCredentialProxy credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireToken(resource, (credential != null) ? credential.Credential : null));
            
            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, (credential != null) ? credential.Credential : null));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, ClientAssertionProxy credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireToken(resource, (credential != null) ? credential.Credential : null));

            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, (credential != null) ? credential.Credential : null));
        }

// Disabled Non-Interactive Feature
#if false
        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string clientId, UserCredentialProxy credential)
        {
            return await RunTask(this.context.AcquireTokenAsync(resource, clientId, 
                (credential.Password == null) ? 
                new UserCredential(credential.UserId) :
                new UserCredential(credential.UserId, credential.Password)));
        }
#endif

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri)
        {
            return RunTaskInteractive(resource, clientId, redirectUri);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, string userId)
        {
            return RunTaskInteractive(resource, clientId, redirectUri, PromptBehaviorProxy.NotSpecified, userId);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, string userId, string extraQueryParameters)
        {
            return RunTaskInteractive(resource, clientId, redirectUri, PromptBehaviorProxy.NotSpecified, userId, extraQueryParameters);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehaviorProxy promptBehavior)
        {
            return RunTaskInteractive(resource, clientId, redirectUri, promptBehavior);
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehaviorProxy promptBehavior, string userId)
        {
            return RunTaskInteractive(resource, clientId, redirectUri, promptBehavior, userId);
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

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientCredentialProxy credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByRefreshToken(refreshToken, clientId, (credential != null) ? credential.Credential : null));

            return await RunTaskAsync(this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId, (credential != null) ? credential.Credential : null));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, X509CertificateCredentialProxy credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByRefreshToken(refreshToken, clientId, (credential != null) ? credential.Credential : null));

            return await RunTaskAsync(this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId, (credential != null) ? credential.Credential : null));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, X509CertificateCredentialProxy credential, string resource)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByRefreshToken(refreshToken, clientId, (credential != null) ? credential.Credential : null, resource));

            return await RunTaskAsync(this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId, (credential != null) ? credential.Credential : null, resource));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, ClientAssertionProxy credential, string resource)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByRefreshToken(refreshToken, clientId, (credential != null) ? credential.Credential : null, resource));

            return await RunTaskAsync(this.context.AcquireTokenByRefreshTokenAsync(refreshToken, clientId, (credential != null) ? credential.Credential : null, resource));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientCredentialProxy credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByAuthorizationCode(authorizationCode, redirectUri, (credential != null) ? credential.Credential : null));

            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, (credential != null) ? credential.Credential : null));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, X509CertificateCredentialProxy credential)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByAuthorizationCode(authorizationCode, redirectUri, (credential != null) ? credential.Credential : null));

            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, (credential != null) ? credential.Credential : null));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, X509CertificateCredentialProxy credential, string resource)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByAuthorizationCode(authorizationCode, redirectUri, (credential != null) ? credential.Credential : null, resource));

            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, (credential != null) ? credential.Credential : null, resource));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByAuthorizationCodeAsync(string authorizationCode, Uri redirectUri, ClientAssertionProxy credential, string resource)
        {
            if (CallSync)
                return RunTask(() => this.context.AcquireTokenByAuthorizationCode(authorizationCode, redirectUri, (credential != null) ? credential.Credential : null, resource));

            return await RunTaskAsync(this.context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, redirectUri, (credential != null) ? credential.Credential : null, resource));
        }

        public string AcquireAccessCode(string resource, string clientId, Uri redirectUri, string userId)
        {
            return (RunTaskInteractive(resource, clientId, redirectUri, PromptBehaviorProxy.AccessCodeOnly, userId)).AccessToken;
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string userAssertion, ClientCredentialProxy clientCredential)
        {
            if (CallSync)
            {
                return RunTask(() => this.context.AcquireToken(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), (clientCredential == null) ? null : clientCredential.Credential));
            }

            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), (clientCredential == null) ? null : clientCredential.Credential));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string userAssertion, X509CertificateCredentialProxy clientCertificate)
        {
            if (CallSync)
            {
                return RunTask(() => this.context.AcquireToken(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), (clientCertificate == null) ? null : clientCertificate.Credential));                
            }

            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), (clientCertificate == null) ? null : clientCertificate.Credential));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string userAssertion, ClientAssertionProxy clientAssertion)
        {
            if (CallSync)
            {
                return RunTask(() => this.context.AcquireToken(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), (clientAssertion == null) ? null : clientAssertion.Credential));
            }

            return await RunTaskAsync(this.context.AcquireTokenAsync(resource, (userAssertion == null) ? null : new UserAssertion(userAssertion), (clientAssertion == null) ? null : clientAssertion.Credential));
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

        private AuthenticationResultProxy RunTaskInteractive(string resource, string clientId, Uri redirectUri, PromptBehaviorProxy promptBehaviorProxy = PromptBehaviorProxy.NotSpecified, string userId = NotSpecified, string extraQueryParameters = NotSpecified, int retryCount = 0)
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
                            string authorizationCode = AdalFriend.AcquireAccessCode(this.context, resource, clientId, redirectUri, userId);
                            return new AuthenticationResultProxy() { AccessToken = authorizationCode };
                        }

                        PromptBehavior promptBehavior = (promptBehaviorProxy == PromptBehaviorProxy.Always) ? PromptBehavior.Always :
                                        (promptBehaviorProxy == PromptBehaviorProxy.Never) ? PromptBehavior.Never : PromptBehavior.Auto;

                        if (userId == NotSpecified)
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
                                result = context.AcquireToken(resource, clientId, redirectUri, promptBehavior, extraQueryParameters);
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
                                    result = context.AcquireToken(resource, clientId, redirectUri, userId, extraQueryParameters);
                                }
                            }
                            else if (extraQueryParameters == NotSpecified)
                            {
                                result = context.AcquireToken(resource, clientId, redirectUri, userId, promptBehavior);
                            }
                            else
                            {
                                result = context.AcquireToken(resource, clientId, redirectUri, userId, promptBehavior, extraQueryParameters);
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
                if (resultProxy.ExceptionInnerStatusCode == 503 && retryCount < 5)
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
                Status = AuthenticationStatusProxy.Succeeded,
                AccessToken = result.AccessToken,
                AccessTokenType = result.AccessTokenType,
                ExpiresOn = result.ExpiresOn,
                IsMultipleResourceRefreshToken = result.IsMultipleResourceRefreshToken,
                RefreshToken = result.RefreshToken,
                TenantId = result.TenantId,
                UserInfo = (result.UserInfo != null) ? new UserInfoProxy
                {
                    FamilyName = result.UserInfo.FamilyName,
                    GivenName = result.UserInfo.GivenName,
                    IdentityProvider = result.UserInfo.IdentityProvider,
                    IsUserIdDisplayable = result.UserInfo.IsUserIdDisplayable,
                    UserId = result.UserInfo.UserId
                }
                    : null
            };
        }

        private static AuthenticationResultProxy GetAuthenticationResultProxy(Exception ex)
        {
            var output = new AuthenticationResultProxy
            {
                ErrorDescription = ex.Message,
            };

            if (ex is ArgumentNullException)
            {
                output.Error = ActiveDirectoryAuthenticationError.InvalidArgument;
            }
            else if (ex is ArgumentException)
            {
                output.Error = ActiveDirectoryAuthenticationError.InvalidArgument;
            }
            else if (ex is ActiveDirectoryAuthenticationException)
            {
                output.Error = ((ActiveDirectoryAuthenticationException)ex).ErrorCode;
                output.ExceptionInnerStatusCode = ((ActiveDirectoryAuthenticationException)ex).InnerStatusCode;
            }
            else
            {
                output.Error = ActiveDirectoryAuthenticationError.AuthenticationFailed;
            }

            output.Status = AuthenticationStatusProxy.Failed;
            output.Exception = ex;

            return output;
        }
    }
}
