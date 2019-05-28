using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class VsTest
    {
        [TestMethod]
        public async Task TestCacheStuffAsync()
        {
            var clientID = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
            // var clientID = "d3590ed6-52b3-4102-aeff-aad2292ab01c";

            //var identityRoot = Path.Combine(MsalCacheHelper.UserRootDirectory, ".IdentityService");
            //var builder = new StorageCreationPropertiesBuilder(
            //    "msal.cache",
            //    identityRoot,
            //    clientID);
            //StorageCreationProperties creationProps = builder.Build();
            //var helper = await MsalCacheHelper.CreateAsync(creationProps);

            var publicClient = PublicClientApplicationBuilder.Create(clientID)
                .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            bool afterCalled = false;
            bool beforeCalled = false;

            byte[] msalCache = null;
            string cacheString = null;

            publicClient.UserTokenCache.SetAfterAccess(args => {
                afterCalled = true;
                msalCache = args.TokenCache.SerializeMsalV3();
                cacheString = Encoding.UTF8.GetString(msalCache);
            });

            publicClient.UserTokenCache.SetBeforeAccess(args => beforeCalled = true);

            //helper.RegisterCache(publicClient.UserTokenCache);

            var data = File.ReadAllBytes(Path.Combine(@"C:\Users\mzuber\Downloads", "unencryptedAdalCache.txt"));
            
            ((ITokenCacheSerializer)publicClient.UserTokenCache).DeserializeAdalV3(data);

            var accounts = await publicClient.GetAccountsAsync().ConfigureAwait(false);
            foreach (var account in accounts)
            {
                var scopes = new string[] { "https://graph.microsoft.com/.default" };
                var token = await publicClient.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);
                Console.WriteLine(token.AccessToken);
            }

            Assert.IsTrue(afterCalled);
            Assert.IsTrue(beforeCalled);

            Console.WriteLine();
            Console.WriteLine(cacheString);
            Console.WriteLine();

            var clientID2 = "1fec8e78-bce4-4aaf-ab1b-5451cc387264";
            var publicClient2 = PublicClientApplicationBuilder.Create(clientID2)
                .WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            ((ITokenCacheSerializer)publicClient2.UserTokenCache).DeserializeMsalV3(msalCache);

            var accounts2 = await publicClient2.GetAccountsAsync().ConfigureAwait(false);
            foreach (var account in accounts2)
            {
                var scopes = new string[] { "https://graph.microsoft.com/.default" };
                var token = await publicClient2.AcquireTokenSilent(scopes, account).ExecuteAsync().ConfigureAwait(false);
                Console.WriteLine(token.AccessToken);
            }

            Console.WriteLine();
        }
    }
}
