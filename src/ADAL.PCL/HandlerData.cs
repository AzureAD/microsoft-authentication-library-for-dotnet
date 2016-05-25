
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class HandlerData
    {
        private string clientId;

        /* Authenticator authenticator, TokenCache tokenCache, string resource,
ClientKey clientKey, TokenSubjectType subjectType, bool extendedLifeTimeEnabled */
        public Authenticator authenticator { get; set; }

        public TokenCache tokenCache { get; set; }

        public string resource { get; set; }

        public ClientKey clientKey { get; set; }

        public TokenSubjectType subjectType { get; set; }

        public bool extendedLifeTimeEnabled { get; set; }  

        public HandlerData(Authenticator authenticator, TokenCache tokenCache, string resource, ClientKey clientKey, bool extendedLifeTimeEnabled)
        {
            this.authenticator = authenticator;
            this.tokenCache = tokenCache;
            this.resource = resource;
            this.clientKey = clientKey;
            this.extendedLifeTimeEnabled = extendedLifeTimeEnabled;
        }

        public HandlerData(Authenticator authenticator, TokenCache tokenCache, bool extendedLifeTimeEnabled)
        {
            this.authenticator = authenticator;
            this.tokenCache = tokenCache;
            this.extendedLifeTimeEnabled = extendedLifeTimeEnabled;
        }

        public HandlerData(Authenticator authenticator, TokenCache tokenCache, string resource, bool extendedLifeTimeEnabled)
        {
            this.authenticator = authenticator;
            this.tokenCache = tokenCache;
            this.resource = resource;
            this.extendedLifeTimeEnabled = extendedLifeTimeEnabled;
        }

        public HandlerData(Authenticator authenticator, string resource, string clientId, bool extendedLifeTimeEnabled)
        {
            this.authenticator = authenticator;
            this.resource = resource;
            this.clientId = clientId;
            this.extendedLifeTimeEnabled = extendedLifeTimeEnabled;
        }

        public HandlerData(Authenticator authenticator, TokenCache tokenCache, string resource, string clientId, bool extendedLifeTimeEnabled)
        {
            this.authenticator = authenticator;
            this.tokenCache = tokenCache;
            this.resource = resource;
            this.clientId = clientId;
            this.extendedLifeTimeEnabled = extendedLifeTimeEnabled;
        }
    }
}
