
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Handlers;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    public abstract class AbstractClientApplication
    {
        protected const string DEFAULT_AUTHORTIY = "https://login.microsoftonline.com/common";

        /// <summary>
        /// default false.
        /// </summary>
        public bool RestrictToSingleUser { get; set; }

        public string Authority { get; private set; }

        /// <summary>
        /// Will be a default value. Can be overriden by the developer.
        /// </summary>
        public string ClientId { get;  set; }

        /// <summary>
        /// Redirect Uri configured in the portal. Will have a default value. Not required, if the developer is using the default client ID.
        /// </summary>
        public string RedirectUri { get; set; }

        public TokenCache UserTokenCache { get; set; }

        /// <summary>
        /// Gets or sets correlation Id which would be sent to the service with the next request. 
        /// Correlation Id is to be used for diagnostics purposes. 
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// Gets a value indicating whether address validation is ON or OFF.
        /// </summary>
        public bool ValidateAuthority { get; set; }


        /// <summary>
        /// Returns a User centric view over the cache that provides a list of all the signed in users.
        /// </summary>
        public IEnumerable<User> Users
        {
            get
            {
                if (this.UserTokenCache == null || this.UserTokenCache.Count == 0)
                {
                    PlatformPlugin.Logger.Information(null, "AccessToken cache is null or empty");
                    return new List<User>();
                }

                return this.UserTokenCache.GetUsers(this.ClientId);
            }
        }

        static AbstractClientApplication()
        {
            PlatformPlugin.Logger.Information(null, string.Format("MSAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}' is running...",
                PlatformPlugin.PlatformInformation.GetProductName(), MsalIdHelper.GetMsalVersion(), MsalIdHelper.GetAssemblyFileVersion(), MsalIdHelper.GetAssemblyInformationalVersion()));
        }

        protected AbstractClientApplication(string authority, string clientId, string redirectUri, bool validateAuthority)
        {
            this.Authority = authority;
            this.ClientId = clientId;
            this.RedirectUri = redirectUri;
            this.ValidateAuthority = validateAuthority;
        }

        internal async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(Authenticator authenticator, string[] scope, ClientKey clientKey, string userId, IPlatformParameters parameters, string policy)
        {
            if (parameters == null)
            {
                parameters = PlatformPlugin.DefaultPlatformParameters;
            }

            var handler = new AcquireTokenSilentHandler(authenticator, this.UserTokenCache, scope, clientKey, userId,  parameters, policy, this.RestrictToSingleUser);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(Authenticator authenticator, string[] scope, ClientKey clientKey, User user, IPlatformParameters parameters, string policy)
        {
            if (parameters == null)
            {
                parameters = PlatformPlugin.DefaultPlatformParameters;
            }

            var handler = new AcquireTokenSilentHandler(authenticator, this.UserTokenCache, scope, clientKey, user, parameters, policy, this.RestrictToSingleUser);
            return await handler.RunAsync().ConfigureAwait(false);
        }
    }
}
