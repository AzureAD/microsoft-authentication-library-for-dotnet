using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    internal class B2COpenIdConfigurationEndpointManager : IOpenIdConfigurationEndpointManager
    {
        private const string OpenIdConfigurationEndpoint = "v2.0/.well-known/openid-configuration";

        /// <inheritdoc />
        public Task<string> GetOpenIdConfigurationEndpointAsync(
            AuthorityInfo authorityInfo,
            string userPrincipalName,
            RequestContext requestContext)
        {
            string defaultEndpoint = string.Format(
                CultureInfo.InvariantCulture,
                new Uri(authorityInfo.CanonicalAuthority).AbsoluteUri + OpenIdConfigurationEndpoint);
            return Task.FromResult(defaultEndpoint);
        }
    }
}
