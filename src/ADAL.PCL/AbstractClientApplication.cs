
using System;
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
        public string ClientId { get; set; }

        /// <summary>
        /// Redirect Uri configured in the portal. Will have a default value. Not required, if the developer is using the default client ID.
        /// </summary>
        public string RedirectUri { get; set; }

        public TokenCache TokenCache { get; set; }

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

        static AbstractClientApplication()
        {
            PlatformPlugin.Logger.Information(null, string.Format("ADAL {0} with assembly version '{1}', file version '{2}' and informational version '{3}' is running...",
                PlatformPlugin.PlatformInformation.GetProductName(), MsalIdHelper.GetMsalVersion(), MsalIdHelper.GetAssemblyFileVersion(), MsalIdHelper.GetAssemblyInformationalVersion()));
        }

        protected AbstractClientApplication(string authority, string clientId, string redirectUri)
        {
            this.Authority = authority;
            this.ClientId = clientId;
            this.RedirectUri = redirectUri;
        }
        
        internal async Task<AuthenticationResult> AcquireTokenSilentCommonAsync(string[] scope, ClientKey clientKey, UserIdentifier userId, IPlatformParameters parameters)
        {
            var handler = new AcquireTokenSilentHandler(this.Authenticator, this.TokenCache, scope, clientKey, userId, parameters);
            return await handler.RunAsync().ConfigureAwait(false);
        }


    }
}
