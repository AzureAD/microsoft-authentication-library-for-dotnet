using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace NetStandard
{
    public class NetStandardAuthenticator
    {
        // This app has http://localhost redirect uri registered
        private static readonly string s_clientIdForPublicApp = "1d18b3b0-251b-4714-a02a-9956cec86c2d";

        private static readonly IEnumerable<string> s_scopes = new[] { "user.read" }; // used for WIA and U/P, can be empty

        private const string GraphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

        private readonly LogCallback _logCallback;
        private readonly string _cacheFilePath;

        public NetStandardAuthenticator(LogCallback logCallback, string cacheFilePath )
        {
            _logCallback = logCallback ?? throw new ArgumentNullException(nameof(logCallback));
            _cacheFilePath = cacheFilePath ?? throw new ArgumentNullException(nameof(logCallback));
        }

        public Task<AuthenticationResult> GetTokenInteractiveAsync(CancellationToken cts)
        {
            var pca = CreatePca();

            return pca.AcquireTokenInteractive(s_scopes).ExecuteAsync(cts);
        }

        private IPublicClientApplication CreatePca()
        {
            IPublicClientApplication pca = PublicClientApplicationBuilder
                            .Create(s_clientIdForPublicApp)
                            .WithLogging(_logCallback)
                            .WithRedirectUri("http://localhost") // required for DefaultOsBrowser
                            .Build();

            pca.UserTokenCache.SetBeforeAccess(notificationArgs =>
            {
                notificationArgs.TokenCache.DeserializeMsalV3(File.Exists(_cacheFilePath)
                    ? File.ReadAllBytes(_cacheFilePath)
                    : null);
            });
            pca.UserTokenCache.SetAfterAccess(notificationArgs =>
            {
                // if the access operation resulted in a cache update
                if (notificationArgs.HasStateChanged)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(_cacheFilePath, notificationArgs.TokenCache.SerializeMsalV3());
                }
            });
            return pca;
        }


    }
}
