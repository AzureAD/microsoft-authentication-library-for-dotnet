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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;
using Test.ADAL.NET.Unit.Mocks;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class AcquireTokenSilentTests
    {
        [TestMethod]
        [TestCategory("AcquireTokenSilentTests")]
        public async Task AcquireTokenSilentServiceErrorTestAsync()
        {
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityCommonTenant, TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User, "unique_id", "displayable@id.com");
            cache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "something-invalid",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow)
            };

            AuthenticationContext context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, cache);

            try
            {
                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage()
                });
                await context.AcquireTokenSilentAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, new UserIdentifier("unique_id", UserIdentifierType.UniqueId));
                Assert.Fail("AdalSilentTokenAcquisitionException was expected");
            }
            catch (AdalSilentTokenAcquisitionException ex)
            {
                Assert.AreEqual(AdalError.FailedToAcquireTokenSilently, ex.ErrorCode);
                Assert.AreEqual(AdalErrorMessage.FailedToRefreshToken, ex.Message);
                Assert.IsNotNull(ex.InnerException);
                Assert.IsTrue(ex.InnerException is AdalException);
                Assert.AreEqual(((AdalException)ex.InnerException).ErrorCode, "invalid_grant");
            }
            catch
            {
                Assert.Fail("AdalSilentTokenAcquisitionException was expected");
            }

        }
    }
}
