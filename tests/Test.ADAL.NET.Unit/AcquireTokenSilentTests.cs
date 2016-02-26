using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class AcquireTokenSilentTests
    {
        [TestMethod]
        [TestCategory("AcquireTokenSilentTests")]
        public async Task AcquireTokenSilentServiceErrorTestAsync()
        {
            Sts sts = new AadSts();
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(sts.Authority, sts.ValidResource, sts.ValidClientId, TokenSubjectType.User, "unique_id", "displayable@id.com");
            cache.tokenCacheDictionary[key] = new AuthenticationResultEx
            {
                RefreshToken = "something-invalid",
                ResourceInResponse = sts.ValidResource,
                Result = new AuthenticationResult("Bearer", "some-access-token", DateTimeOffset.UtcNow)
            };

            AuthenticationContext context = new AuthenticationContext(sts.Authority, sts.ValidateAuthority, cache);

            try
            {
                await context.AcquireTokenSilentAsync(sts.ValidResource, sts.ValidClientId, new UserIdentifier("unique_id", UserIdentifierType.UniqueId));
                Verify.Fail("AdalSilentTokenAcquisitionException was expected");
            }
            catch (AdalSilentTokenAcquisitionException ex)
            {
                Verify.AreEqual(AdalError.FailedToAcquireTokenSilently, ex.ErrorCode);
                Verify.AreEqual(AdalErrorMessage.FailedToRefreshToken, ex.Message);
                Verify.IsNotNull(ex.InnerException);
                Verify.IsTrue(ex.InnerException is AdalException);
                Verify.AreEqual(((AdalException)ex.InnerException).ErrorCode, "invalid_grant");
            }
            catch
            {
                Verify.Fail("AdalSilentTokenAcquisitionException was expected");
            }

        }
    }
}