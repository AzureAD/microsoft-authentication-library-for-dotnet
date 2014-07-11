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
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    enum TestType
    {
        DotNet,
        WinRT
    }

    internal partial class AdalTests
    {
        private const string SecondCallExtraQueryParameter = "secondcall";
        private const string ThirdCallExtraQueryParameter = "thirdcall";

        public static TestType TestType { get; set; }

        public static void InitializeTest()
        {
            AuthenticationContextProxy.InitializeTest();
        }


        public static void AcquireTokenPositiveTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);
        }

        public static void AcquireTokenPositiveWithoutRedirectUriOrUserIdTest(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri);
            VerifySuccessResult(sts, result);

            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "userId");
            VerifyErrorResult(result, Sts.InvalidArgumentError, "UserIdentifier.AnyUser");

            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);
        }

        public static async Task AcquireTokenPositiveByRefreshTokenTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.ValidClientId, (string)null);
            VerifySuccessResult(sts, result, true, false);

            AuthenticationResultProxy result2 = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken + "x", sts.ValidClientId, (string)null);
            
            // TODO: Update status code to 400 once AAD returns it.
            VerifyErrorResult(result2, "invalid_grant", "Refresh Token", (sts.Type == StsType.ADFS) ? 400 : 401);

            result = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.ValidClientId, sts.ValidResource);
            if (sts.Type == StsType.ADFS)
            {
                VerifyErrorResult(result, Sts.InvalidArgumentError, "multiple resource");                
            }
            else
            {
                VerifySuccessResult(sts, result, true, false);
            }
        }

        public static void AuthenticationContextAuthorityValidationTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.InvalidAuthority, true);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            if (sts.Type == StsType.ADFS)
            {
                VerifyErrorResult(result, Sts.InvalidArgumentError, "validateAuthority");
            }
            else
            {
                VerifyErrorResult(result, Sts.AuthorityNotInValidList, "authority");                
            }

            context = new AuthenticationContextProxy(sts.InvalidAuthority, false);
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationUiFailedError, "authentication dialog");
            context = new AuthenticationContextProxy(sts.Authority, false);
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            context = new AuthenticationContextProxy(sts.Authority, true);
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            if (sts.Type == StsType.ADFS)
            {
                VerifyErrorResult(result, Sts.InvalidArgumentError, "validateAuthority");
            }
            else                
            {
                VerifySuccessResult(sts, result);                 
            }                

            context = new AuthenticationContextProxy(sts.Authority);
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            if (sts.Type == StsType.ADFS)
            {
                VerifyErrorResult(result, Sts.InvalidArgumentError, "validateAuthority");
            }
            else
            {
                VerifySuccessResult(sts, result);
            }

            context = new AuthenticationContextProxy(sts.Authority + "/extraPath1/extraPath2", sts.ValidateAuthority);
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);            
        }

        public static void AcquireTokenWithRedirectUriTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.InvalidExistingRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);

            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.InvalidNonExistingRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);

            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, new Uri(sts.ValidNonExistingRedirectUri.AbsoluteUri + "#fragment"), PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");
            VerifyErrorResult(result, Sts.InvalidArgumentError, "fragment");

            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, null, PromptBehaviorProxy.Auto, sts.ValidUserId);
            if (TestType != TestType.WinRT)
            {
                VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");
            }
            else
            {
                // Winrt can send null redirecturi
                VerifySuccessResult(sts, result);
            }

            AuthenticationContextProxy.ClearDefaultCache();
            EndBrowserDialogSession();
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientIdWithExistingRedirectUri, sts.ValidExistingRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContextProxy.ClearDefaultCache();

            result = context.AcquireToken(sts.ValidResource, sts.ValidNonExistentRedirectUriClientId, sts.ValidNonExistingRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);            
        }

        public static void AcquireTokenWithInvalidAuthorityTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy("https://www.live.com/login", false);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);

            context = new AuthenticationContextProxy(sts.InvalidAuthority, false);
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationUiFailedError, null);

            if (sts.Type != StsType.ADFS)
            {
                Uri uri = new Uri(sts.Authority);
                context = new AuthenticationContextProxy(string.Format("{0}://{1}/non_existing_tenant", uri.Scheme, uri.Authority));
                result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
                VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);
            }
        }

        public static void AcquireTokenWithInvalidResourceTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.InvalidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifyErrorResult(result, Sts.InvalidResourceError, "resource");

            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            result = context.AcquireToken(sts.ValidResource.ToUpper(), sts.ValidClientId.ToUpper(), sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, 
                (sts.Type == StsType.AAD) ? new UserIdentifier(sts.ValidUserName, UserIdentifierType.RequiredDisplayableId) : UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);

            result = context.AcquireToken(sts.ValidResource.ToUpper(), sts.ValidClientId.ToUpper(), sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto,
                (result.UserInfo != null) ? new UserIdentifier(result.UserInfo.UniqueId, UserIdentifierType.UniqueId) : UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);
        }

        public static void AcquireTokenWithInvalidClientIdTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.InvalidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);
        }

        public static void AcquireTokenWithIncorrectUserCredentialTest(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(sts.InvalidUserName, "invalid_password");
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, UserIdentifier.AnyUser, "incorrect_user");
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, "canceled");
        }

        public static void AcquireTokenWithAuthenticationCanceledTest(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(null, null);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, new UserIdentifier("cancel_authentication@test.com", UserIdentifierType.OptionalDisplayableId));
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, "canceled");
        }

        public static void AcquireTokenPositiveWithDefaultCacheTest(Sts sts)
        {
            AuthenticationContextProxy.ClearDefaultCache();

            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);          
            List<AuthenticationResultProxy> results = AcquireTokenPositiveWithCache(sts, context);
            VerifyExpiresOnAreEqual(results[0], results[1]);

            EndBrowserDialogSession();
            Log.Comment("Waiting 2 seconds before next token request...");
            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay
            AuthenticationResultProxy resultWithoutUser = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, UserIdentifier.AnyUser, SecondCallExtraQueryParameter);
            VerifyExpiresOnAreEqual(results[0], resultWithoutUser);

            context.VerifySingleItemInCache(results[0], sts.Type);
        }

        public static void AcquireTokenPositiveWithNullCacheTest(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            var context = new AuthenticationContextProxy(
                sts.Authority,
                sts.ValidateAuthority,
                TokenCacheStoreType.Null);
            List<AuthenticationResultProxy> results = AcquireTokenPositiveWithCache(sts, context);
            VerifyExpiresOnAreNotEqual(results[0], results[1]);
        }

        public static void AcquireTokenPositiveWithInMemoryCacheTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheStoreType.InMemory);
            List<AuthenticationResultProxy> results = AcquireTokenPositiveWithCacheExpectingEqualResults(sts, context);
            VerifyExpiresOnAreEqual(results[0], results[1]);
        }

        public static void UserInfoTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationResultProxy result2;
            if (sts.Type == StsType.AAD)
            {
                Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);
                Verify.IsNotNull(result.UserInfo.UniqueId);
                Verify.IsNotNull(result.UserInfo.GivenName);
                Verify.IsNotNull(result.UserInfo.FamilyName);

                EndBrowserDialogSession();
                Log.Comment("Waiting 2 seconds before next token request...");
                AuthenticationContextProxy.Delay(2000);   // 2 seconds delay
                AuthenticationContextProxy.SetCredentials(null, null);
                result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto,
                    new UserIdentifier(result.UserInfo.DisplayableId, UserIdentifierType.OptionalDisplayableId), 
                    SecondCallExtraQueryParameter);
                ValidateAuthenticationResultsAreEqual(result, result2);
            }

            AuthenticationContextProxy.SetCredentials(null, null);
            result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri);
            Verify.AreEqual(result.AccessToken, result2.AccessToken);

            SetCredential(sts);
            result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId, ThirdCallExtraQueryParameter);
            VerifySuccessResult(sts, result2);
            if (result.UserInfo != null)
            {
                ValidateAuthenticationResultsAreEqual(result, result2);
            }
            else
            {
                VerifyExpiresOnAreNotEqual(result, result2);
            }

            EndBrowserDialogSession();
            Log.Comment("Waiting 2 seconds before next token request...");
            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.InvalidRequiredUserId, SecondCallExtraQueryParameter);
            VerifyErrorResult(result2, "user_mismatch", null);
        }

        public static async Task MultiResourceRefreshTokenTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationResultProxy result2 = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.ValidClientId, sts.ValidResource2);
            if (sts.Type == StsType.AAD)
            {
                VerifySuccessResult(sts, result2, true, false);
                Verify.IsTrue(result.IsMultipleResourceRefreshToken);
                Verify.IsTrue(result2.IsMultipleResourceRefreshToken);
            }

            result2 = context.AcquireToken(sts.ValidResource2, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result2);
            if (sts.Type == StsType.ADFS)
            {
                Verify.IsFalse(result.IsMultipleResourceRefreshToken);
            }
            else
            {
                Verify.IsTrue(result.IsMultipleResourceRefreshToken);                
            }

            if (sts.Type == StsType.AAD)
            {
                result2 = context.AcquireToken(sts.ValidResource3, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
                VerifySuccessResult(sts, result2);
                Verify.IsTrue(result.IsMultipleResourceRefreshToken);
            }
        }

        public static void TenantlessTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.TenantlessAuthority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);
            Verify.IsNotNull(result.TenantId);

            AuthenticationContextProxy.SetCredentials(null, null);
            AuthenticationResultProxy result2 = context.AcquireToken(
                sts.ValidResource, 
                sts.ValidClientId,
                sts.ValidDefaultRedirectUri,
                PromptBehaviorProxy.Auto, 
                sts.ValidUserId);

            ValidateAuthenticationResultsAreEqual(result, result2);

            SetCredential(sts);
            context = new AuthenticationContextProxy(sts.TenantlessAuthority.Replace("Common", result.TenantId), sts.ValidateAuthority, TokenCacheStoreType.Null);
            result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result2);
        }

        public static async Task InstanceDiscoveryTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContextProxy.SetEnvironmentVariable("ExtraQueryParameter", string.Empty);

            // PROD discovery endpoint knows about PPE as well, so this passes discovery and fails later as refresh token is invalid for PPE.
            context = new AuthenticationContextProxy(sts.Authority.Replace("windows.net", "windows-ppe.net"), sts.ValidateAuthority);
            result = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.ValidClientId, sts.ValidResource);
            VerifyErrorResult(result, "invalid_grant", "Refresh Token");

            context = new AuthenticationContextProxy(sts.Authority.Replace("windows.net", "windows.unknown"), sts.ValidateAuthority);
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifyErrorResult(result, "authority_not_in_valid_list", "invalid_instance");
        }

        public static void ForcePromptTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContextProxy.SetCredentials(null, null);
            AuthenticationResultProxy result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, 
                (sts.Type == StsType.ADFS) ? null : sts.ValidUserId);
            VerifySuccessResult(sts, result2);
            Verify.AreEqual(result2.AccessToken, result.AccessToken);

            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Always);
            VerifySuccessResult(sts, result);
            Verify.AreNotEqual(result2.AccessToken, result.AccessToken);
        }

        public static void AcquireTokenPositiveWithFederatedTenantTest(Sts sts)
        {
            var userId = sts.ValidUserId;

            AuthenticationContextProxy.SetCredentials(userId.Id, sts.ValidPassword);
            var context = new AuthenticationContextProxy(sts.Authority, false, TokenCacheStoreType.Null);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, userId);
            VerifySuccessResult(sts, result);

            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result);
        }

        public static async Task AcquireTokenNonInteractivePositiveTestAsync(Sts sts)
        {
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            UserCredentialProxy credential = new UserCredentialProxy(sts.ValidUserName, sts.ValidPassword);
            AuthenticationResultProxy result = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId, credential);
            VerifySuccessResult(sts, result);
            Verify.IsNotNull(result.UserInfo);
            Verify.IsNotNull(result.UserInfo.UniqueId);
            Verify.IsNotNull(result.UserInfo.DisplayableId);

            AuthenticationContextProxy.Delay(2000);

            // Test token cache
            AuthenticationResultProxy result2 = await context.AcquireTokenAsync(sts.ValidResource, sts.ValidClientId, credential);
            VerifySuccessResult(sts, result2);
            VerifyExpiresOnAreEqual(result, result2);
        }

        public static async Task WebExceptionAccessTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);
            result = await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, sts.InvalidClientId);
            VerifyErrorResult(result, "invalid_request", "AADSTS90011");
            Verify.IsNotNull(result.Exception);
            Verify.IsNotNull(result.Exception.InnerException);
            Verify.IsTrue(result.Exception.InnerException is WebException);
            using (StreamReader sr = new StreamReader(((WebException)(result.Exception.InnerException)).Response.GetResponseStream()))
            {
                string streamBody = sr.ReadToEnd();
                Verify.IsTrue(streamBody.Contains("AADSTS90011"));
            }
        }

        public static void ExtraQueryParametersTest(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheStoreType.Null);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId, null);
            VerifySuccessResult(sts, result);

            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId, "redirect_uri=123");
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);   // AADSTS90004: The request is not properly formatted. The parameter 'redirect_uri' is duplicated.  

            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId, "resource=123");
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);   // AADSTS90004: The request is not properly formatted. The parameter 'resource' is duplicated.

            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId, "client_id=123");
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);   // AADSTS90004: The request is not properly formatted. The parameter 'client_id' is duplicated.  

            EndBrowserDialogSession();
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId, "login_hint=123");
            
            // AAD error: AADSTS90004: The request is not properly formatted. The parameter 'login_hint' is duplicated.              
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, null);                

            EndBrowserDialogSession();
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId, "login_hintx=123");
            VerifySuccessResult(sts, result);

            EndBrowserDialogSession();
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, UserIdentifier.AnyUser, "login_hint=" + sts.ValidUserName);
            VerifySuccessResult(sts, result);
        }

        internal static void AcquireTokenWithPromptBehaviorNeverTest(Sts sts)
        {
            // Should not be able to get a token silently on first try.
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Never);
            VerifyErrorResult(result, Sts.UserInteractionRequired, null);

            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            // Obtain a token interactively.
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContextProxy.SetCredentials(null, null);
            // Now there should be a token available in the cache so token should be available silently.
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Never);
            VerifySuccessResult(sts, result);

            // Clear the cache and silent auth should work via session cookies.
            AuthenticationContextProxy.ClearDefaultCache();
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Never);
            VerifySuccessResult(sts, result);

            // Clear the cache and cookies and silent auth should fail.
            AuthenticationContextProxy.ClearDefaultCache();
            EndBrowserDialogSession();
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Never);
            VerifyErrorResult(result, Sts.UserInteractionRequired, null);                
        }

        internal static void MultiUserCacheTest(Sts sts)
        {
            Log.Comment("Acquire token for user1 interactively");
            AuthenticationContextProxy.SetCredentials(null, sts.ValidPassword);            
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user1 returning cached token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Clear cookie and acquire token for user2 interactively");
            EndBrowserDialogSession();
            AuthenticationContextProxy.SetCredentials(null, sts.ValidPassword2);
            AuthenticationResultProxy result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user1 returning cached token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 returning cached token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user1 and resource2 using cached multi resource refresh token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result = context.AcquireToken(sts.ValidResource2, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 and resource2 using cached multi resource refresh token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result2 = context.AcquireToken(sts.ValidResource2, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);
        }

        internal static void SwitchUserTest(Sts sts)
        {
            Log.Comment("Acquire token for user1 interactively");
            AuthenticationContextProxy.SetCredentials(null, sts.ValidPassword);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token via cookie for user1 without user");
            AuthenticationResultProxy result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 via force prompt and user");
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName2, sts.ValidPassword2);
            result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Always, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 via force prompt");
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName2, sts.ValidPassword2);
            result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Always);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Fail to acquire token without user while tokens for two users in the cache");
            result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri);
            VerifyErrorResult(result2, "multiple_matching_tokens_detected", null);
        }

        public static void VerifyExpiresOnAreEqual(AuthenticationResultProxy result, AuthenticationResultProxy result2)
        {
            bool equal = AreDateTimeOffsetsEqual(result.ExpiresOn, result2.ExpiresOn);

            if (!equal)
            {
                Log.Comment(result.ExpiresOn.ToString("R") + " <> " + result2.ExpiresOn.ToString("R"));
            }

            Verify.IsTrue(equal, "AuthenticationResult.ExpiresOn");
        }

        public static void VerifyExpiresOnAreNotEqual(AuthenticationResultProxy result, AuthenticationResultProxy result2)
        {
            bool equal = AreDateTimeOffsetsEqual(result.ExpiresOn, result2.ExpiresOn);

            if (equal)
            {
                Log.Comment(result.ExpiresOn.ToString("R") + " <> " + result2.ExpiresOn.ToString("R"));
            }

            Verify.IsFalse(equal, "AuthenticationResult.ExpiresOn");
        }

        public static bool AreDateTimeOffsetsEqual(DateTimeOffset time1, DateTimeOffset time2)
        {
            bool equal = (time1.Ticks / 10000 == time2.Ticks / 10000);
            if (!equal)
            {
                Log.Comment("DateTimeOffsets with ticks {0} and {1} are not equal", time1.Ticks, time2.Ticks);
            }

            return equal;
        }

        public static List<AuthenticationResultProxy> AcquireTokenPositiveWithCache(Sts sts, AuthenticationContextProxy context)
        {
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            Log.Comment("Waiting 2 seconds before next token request...");
            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay

            AuthenticationResultProxy result2;
            if (result.UserInfo != null)
                result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, new UserIdentifier(result.UserInfo.DisplayableId, UserIdentifierType.OptionalDisplayableId), SecondCallExtraQueryParameter);
            else
                result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri);

            VerifySuccessResult(sts, result2);

            return new List<AuthenticationResultProxy> { result, result2 };
        }

        public static void EndBrowserDialogSession()
        {
            const int INTERNET_OPTION_END_BROWSER_SESSION = 42;
            NativeMethods.InternetSetOption(IntPtr.Zero, INTERNET_OPTION_END_BROWSER_SESSION, IntPtr.Zero, 0);
        }

        public static void VerifySuccessResult(Sts sts, AuthenticationResultProxy result, bool supportRefreshToken = true, bool supportUserInfo = true)
        {
            Log.Comment("Verifying success result...");
            if (result.Status != AuthenticationStatusProxy.Success)
            {
                Log.Comment(string.Format("Unexpected '{0}' error from service: {1}", result.Error, result.ErrorDescription));
            }

            Verify.AreEqual(AuthenticationStatusProxy.Success, result.Status, "AuthenticationResult.Status");
            Verify.IsNotNull(result.AccessToken, "AuthenticationResult.AccessToken");
            if (supportRefreshToken)
            {
                Verify.IsNotNull(result.RefreshToken, "AuthenticationResult.RefreshToken");
            }
            else
            {
                Verify.IsNull(result.RefreshToken, "AuthenticationResult.RefreshToken");
            }

            Verify.IsNull(result.Error, "AuthenticationResult.Error");
            Verify.IsNull(result.ErrorDescription, "AuthenticationResult.ErrorDescription");

            if (sts.Type != StsType.ADFS && supportUserInfo)
            {
                Action<string, string, bool> ValidateUserInfo = (string field, string caption, bool required) =>
                {
                    if (string.IsNullOrEmpty(field))
                    {
                        if (required)
                        {
                            Log.Error("No " + caption);
                        }
                        else
                        {
                            Log.Warning("No " + caption);                            
                        }
                    }
                    else
                    {
                        Log.Comment(field, caption);
                    }
                };

                ValidateUserInfo(result.TenantId, "tenant id", true);
                ValidateUserInfo(result.UserInfo.UniqueId, "user unique id", true);
                ValidateUserInfo(result.UserInfo.DisplayableId, "user displayable id", true);
                ValidateUserInfo(result.UserInfo.GivenName, "given name", false);
                ValidateUserInfo(result.UserInfo.FamilyName, "family name", false);
            }

            long expiresIn = (long)(result.ExpiresOn - DateTime.UtcNow).TotalSeconds;
            Log.Comment("Verifying token expiration...");
            Verify.IsGreaterThanOrEqual(expiresIn, (long)0, "Token ExpiresOn");
        }

        public static void VerifyErrorResult(AuthenticationResultProxy result, string error, string errorDescriptionKeyword, int statusCode = 0)
        {
            Log.Comment(string.Format("Verifying error result '{0}':'{1}'...", result.Error, result.ErrorDescription));
            Verify.AreNotEqual(AuthenticationStatusProxy.Success, result.Status);
            Verify.IsNull(result.AccessToken);
            Verify.IsNotNull(result.Error);
            Verify.IsNotNull(result.ErrorDescription);
            Verify.IsFalse(result.ErrorDescription.Contains("+"), "Error description should not be in URL form encoding!");
            Verify.IsFalse(result.ErrorDescription.Contains("%2"), "Error description should not be in URL encoding!");

            if (error != null)
            {
                Verify.AreEqual(error, result.Error);
            }

            if (errorDescriptionKeyword != null)
            {
                VerifyErrorDescriptionContains(result.ErrorDescription, errorDescriptionKeyword);
            }

            if (statusCode != 0)
            {
                Verify.AreEqual(statusCode, result.ExceptionStatusCode);
            }
        }

        private static List<AuthenticationResultProxy> AcquireTokenPositiveWithCacheExpectingEqualResults(Sts sts, AuthenticationContextProxy context)
        {
            List<AuthenticationResultProxy> results = AcquireTokenPositiveWithCache(sts, context);

            Verify.AreEqual(results[0].AccessToken, results[1].AccessToken, "AuthenticationResult.AccessToken");
            Log.Comment(string.Format("First ExpiresOn: {0}", results[0].ExpiresOn));
            Log.Comment(string.Format("Second ExpiresOn: {0}", results[1].ExpiresOn));
            return results;
        }

        private static void VerifyErrorDescriptionContains(string errorDescription, string keyword)
        {
            Log.Comment(string.Format("Verifying error description '{0}'...", errorDescription));
            Verify.IsGreaterThanOrEqual(errorDescription.IndexOf(keyword, StringComparison.OrdinalIgnoreCase), 0);
        }

        private static void ValidateAuthenticationResultsAreEqual(AuthenticationResultProxy result, AuthenticationResultProxy result2)
        {
            Verify.AreEqual(result.AccessToken, result2.AccessToken, "AuthenticationResult.AccessToken");
            Verify.AreEqual(result.RefreshToken, result2.RefreshToken, "AuthenticationResult.RefreshToken");
            Verify.AreEqual(result.UserInfo.UniqueId, result2.UserInfo.UniqueId);
            Verify.AreEqual(result.UserInfo.DisplayableId, result2.UserInfo.DisplayableId);
            Verify.AreEqual(result.UserInfo.GivenName, result2.UserInfo.GivenName);
            Verify.AreEqual(result.UserInfo.FamilyName, result2.UserInfo.FamilyName);
            Verify.AreEqual(result.TenantId, result2.TenantId);
        }
        
        private static void SetCredential(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(sts.Type == StsType.ADFS ? sts.ValidUserName : null, sts.ValidPassword);            
        }

        private static class NativeMethods
        {
            [DllImport("wininet.dll", SetLastError = true)]
            public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);
        }

        public static void AcquireTokenAndRefreshSessionTest(Sts sts)
        {
            var userId = sts.ValidUserId;

            AuthenticationContextProxy.SetCredentials(userId.Id, sts.ValidPassword);
            var context = new AuthenticationContextProxy(sts.Authority, false, TokenCacheStoreType.InMemory);
            AuthenticationResultProxy result = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.Auto, userId);
            VerifySuccessResult(sts, result);
            //Thread.Sleep(TimeSpan.FromSeconds(2));
            AuthenticationResultProxy result2 = context.AcquireToken(sts.ValidResource, sts.ValidClientId, sts.ValidDefaultRedirectUri, PromptBehaviorProxy.RefreshSession, userId);
            VerifySuccessResult(sts, result2);
            Verify.AreNotEqual(result.AccessToken, result2.AccessToken);
        }
    }
}
