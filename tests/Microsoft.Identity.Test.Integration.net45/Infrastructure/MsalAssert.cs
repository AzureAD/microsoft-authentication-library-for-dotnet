// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using System.Linq;
using System.Globalization;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    public static class MsalAssert
    {
        public static async Task<IAccount> AssertSingleAccountAsync(
            LabResponse labResponse,
            IPublicClientApplication pca,
            AuthenticationResult result)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.AccessToken));
            var account = (await pca.GetAccountsAsync().ConfigureAwait(false)).Single();
            Assert.IsTrue(
                    string.Equals(
                        labResponse.User.Upn,
                        result.Account.Username,
                        System.StringComparison.InvariantCultureIgnoreCase));

            return account;
        }

        public static void AssertAuthResult(AuthenticationResult result, LabUser user = null)
        {
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.AccessToken);

            if (user != null)
            {
                Assert.IsTrue(
                    string.Equals(
                        user.Upn, 
                        result.Account.Username, 
                        System.StringComparison.InvariantCultureIgnoreCase));
            }
        }
    }
}
