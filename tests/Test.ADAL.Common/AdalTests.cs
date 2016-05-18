//----------------------------------------------------------------------
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        public static IPlatformParameters PlatformParameters { get; set; }
        

        public static async Task AcquireTokenWithAuthenticationCanceledTestAsync(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(null, null);
            var context = new AuthenticationContextProxy(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResultProxy result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, new UserIdentifier("cancel_authentication@test.com", UserIdentifierType.OptionalDisplayableId));
            VerifyErrorResult(result, Sts.AuthenticationCanceledError, "canceled");
        }

        public static async Task AcquireTokenPositiveWithDefaultCacheTestAsync(Sts sts)
        {
            AuthenticationContextProxy.ClearDefaultCache();

            SetCredential(sts);
            var context = new AuthenticationContextProxy(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            List<AuthenticationResultProxy> results = await AcquireTokenPositiveWithCacheAsync(sts, context);
            VerifyExpiresOnAreEqual(results[0], results[1]);

            EndBrowserDialogSession();
            Log.Comment("Waiting 2 seconds before next token request...");
            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay
            AuthenticationResultProxy resultWithoutUser = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, UserIdentifier.AnyUser, SecondCallExtraQueryParameter);
            VerifyExpiresOnAreEqual(results[0], resultWithoutUser);

            context.VerifySingleItemInCache(results[0], sts.Type);
        }

        public static async Task AcquireTokenPositiveWithNullCacheTestAsync(Sts sts)
        {
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            var context = new AuthenticationContextProxy(
                TestConstants.DefaultAuthorityCommonTenant,
                sts.ValidateAuthority,
                TokenCacheType.Null);
            List<AuthenticationResultProxy> results = await AcquireTokenPositiveWithCacheAsync(sts, context);
            VerifyExpiresOnAreNotEqual(results[0], results[1]);
        }

        public static async Task AcquireTokenPositiveWithInMemoryCacheTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority, TokenCacheType.InMemory);
            List<AuthenticationResultProxy> results = await AcquireTokenPositiveWithCacheExpectingEqualResultsAsync(sts, context);
            VerifyExpiresOnAreEqual(results[0], results[1]);
        }

        public static async Task UserInfoTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResultProxy result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationResultProxy result2;
            if (sts.Type == StsType.AAD)
            {
                Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);
                Verify.IsNotNullOrEmptyString(result.UserInfo.UniqueId);
                Verify.IsNotNullOrEmptyString(result.UserInfo.GivenName);
                Verify.IsNotNullOrEmptyString(result.UserInfo.FamilyName);

                EndBrowserDialogSession();
                Log.Comment("Waiting 2 seconds before next token request...");
                AuthenticationContextProxy.Delay(2000);   // 2 seconds delay
                AuthenticationContextProxy.SetCredentials(null, null);
                result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters,
                    new UserIdentifier(result.UserInfo.DisplayableId, UserIdentifierType.OptionalDisplayableId), 
                    SecondCallExtraQueryParameter);
                ValidateAuthenticationResultsAreEqual(result, result2);
            }

            AuthenticationContextProxy.SetCredentials(null, null);
            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters);
            Verify.AreEqual(result.AccessToken, result2.AccessToken);

            SetCredential(sts);
            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidUserId, ThirdCallExtraQueryParameter);
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
            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.InvalidRequiredUserId, SecondCallExtraQueryParameter);
            VerifyErrorResult(result2, "user_mismatch", null);
        }

        public static async Task MultiResourceRefreshTokenTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResultProxy result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationResultProxy result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource2, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result2);

            if (sts.Type == StsType.AAD)
            {
                result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource3, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidUserId);
                VerifySuccessResult(sts, result2);
            }
        }
        


        internal static async Task MultiUserCacheTestAsync(Sts sts)
        {
            Log.Comment("Acquire token for user1 interactively");
            AuthenticationContextProxy.SetCredentials(null, sts.ValidPassword);            
            var context = new AuthenticationContextProxy(TestConstants.DefaultAuthorityCommonTenant, sts.ValidateAuthority);
            AuthenticationResultProxy result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user1 returning cached token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Clear cookie and acquire token for user2 interactively");
            EndBrowserDialogSession();
            AuthenticationContextProxy.SetCredentials(null, sts.ValidPassword2);
            AuthenticationResultProxy result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user1 returning cached token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 returning cached token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user1 and resource2 using cached multi resource refresh token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource2, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 and resource2 using cached multi resource refresh token");
            AuthenticationContextProxy.SetCredentials(null, null);
            result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource2, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);
        }
        

        public static async Task<List<AuthenticationResultProxy>> AcquireTokenPositiveWithCacheAsync(Sts sts, AuthenticationContextProxy context)
        {
            AuthenticationResultProxy result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            Log.Comment("Waiting 2 seconds before next token request...");
            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay

            AuthenticationResultProxy result2;
            if (result.UserInfo != null)
                result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters, new UserIdentifier(result.UserInfo.DisplayableId, UserIdentifierType.OptionalDisplayableId), SecondCallExtraQueryParameter);
            else
                result2 = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultResource, PlatformParameters);

            VerifySuccessResult(sts, result2);

            return new List<AuthenticationResultProxy> { result, result2 };
        }

        public static void EndBrowserDialogSession()
        {
            const int INTERNET_OPTION_END_BROWSER_SESSION = 42;
            NativeMethods.InternetSetOption(IntPtr.Zero, INTERNET_OPTION_END_BROWSER_SESSION, IntPtr.Zero, 0);
        }

        private static async Task<List<AuthenticationResultProxy>> AcquireTokenPositiveWithCacheExpectingEqualResultsAsync(Sts sts, AuthenticationContextProxy context)
        {
            List<AuthenticationResultProxy> results = await AcquireTokenPositiveWithCacheAsync(sts, context);

            Verify.AreEqual(results[0].AccessToken, results[1].AccessToken, "AuthenticationResult.AccessToken");
            Log.Comment(string.Format(CultureInfo.CurrentCulture, " First ExpiresOn: {0}", results[0].ExpiresOn));
            Log.Comment(string.Format(CultureInfo.CurrentCulture, " Second ExpiresOn: {0}", results[1].ExpiresOn));
            return results;
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
    }
}
