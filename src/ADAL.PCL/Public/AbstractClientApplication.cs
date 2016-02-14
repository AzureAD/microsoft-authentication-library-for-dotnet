
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    public abstract class AbstractClientApplication
    {
        internal Authenticator Authenticator;
        protected const string DEFAULT_AUTHORTIY = "https://login.microsoftonline.com/common";

        /// <summary>
        /// default false. TODO - Why would anyone build a single user, single tenant app. Consider removal.
        /// </summary>
        public string RestrictToSingleUser { get; set; }

        public string Authority { get; private set; }

        /// <summary>
        /// Will be a default value. Can be overriden by the developer.
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// Redirect Uri configured in the portal. Will have a default value. Not required, if the developer is using the default client ID.
        /// </summary>
        public string RedirectUri { get; set; }

        public TokenCache UserTokenCache { get; set; }

        /// <summary>
        /// Gets or sets correlation Id which would be sent to the service with the next request. 
        /// Correlation Id is to be used for diagnostics purposes. 
        /// </summary>
        public Guid CorrelationId
        {
            get
            {
                return this.Authenticator.CorrelationId;
            }

            set
            {
                this.Authenticator.CorrelationId = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether address validation is ON or OFF.
        /// </summary>
        public bool ValidateAuthority
        {
            get
            {
                return this.Authenticator.ValidateAuthority;
            }

            set { this.Authenticator.ValidateAuthority = value; }
        }


        /// <summary>
        /// Returns a User centric view over the cache that provides a list of all the signed in users.
        /// </summary>
        public IEnumerable<User> GetUsers()
        {
            List <User> users = new List<User>();
            if (this.UserTokenCache == null || this.UserTokenCache.Count == 0)
            {
                PlatformPlugin.Logger.Information(null, "AccessToken cache is null or empty");
                return users;
            }
            IEnumerable<TokenCacheItem> allItems = this.UserTokenCache.ReadItems(this.ClientId);
            IEnumerable<string> uniqueIds = allItems.Select(item => item.UniqueId).Distinct();
            foreach(string uniqueId in uniqueIds)
            {
                users.Add(allItems.Where(item => !string.IsNullOrEmpty(item.UniqueId) && item.UniqueId.Equals(uniqueId)).First().User);
            }

            return users;
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
            // If authorityType is not provided (via first constructor), we validate by default (except for ASG and Office tenants).
            this.Authenticator = new Authenticator(authority, validateAuthority);
        }

        internal async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(Authenticator authenticator, string[] scope, ClientKey clientKey, string userId, IPlatformParameters parameters, string policy)
        {
            var handler = new AcquireTokenSilentHandler(authenticator, this.UserTokenCache, scope, clientKey, userId,  parameters, policy);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        internal async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(Authenticator authenticator, string[] scope, ClientKey clientKey, User user, IPlatformParameters parameters, string policy)
        {
            var handler = new AcquireTokenSilentHandler(authenticator, this.UserTokenCache, scope, clientKey, user, parameters, policy);
            return await handler.RunAsync().ConfigureAwait(false);
        }

        private bool isUserIdDisplayable(string userId)
        {
            return false;
        }
    }
}
