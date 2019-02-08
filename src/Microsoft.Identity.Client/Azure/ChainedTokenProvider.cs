using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Azure
{
    /// <summary>
    /// ChainedTokenProvider creates an ordered list of probes that will determine if the environment will allow it to build
    /// a ICredentialProvider. The ICredentialProvider is then used to generate authentication credentials for the
    /// consumer.
    /// </summary>
    public class ChainedTokenProvider : ITokenProvider
    {
        private readonly IList<IProbe> _probes;

        /// <summary>
        /// Create an instance of a ChainedTokenProvider providing a list of IProbes which will be executed in order to create a ICredentialProvider
        /// </summary>
        /// <param name="probes">probes to be executed in order to create a ICredentialProvider</param>
        /// <exception cref="ArgumentException">throws if no probes were provided or if no probe is able to build a ICredentialProvider</exception>
        public ChainedTokenProvider(IList<IProbe> probes)
        {
            if (probes == null || probes.Count() < 1)
            {
                throw new ArgumentException("must provide 1 or more IProbes");
            }
            _probes = probes;
        }

        /// <summary>
        ///     GetTokenAsync returns a token for a given set of scopes
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A token with expiration</returns>
        public async Task<IToken> GetTokenAsync(IEnumerable<string> scopes = null)
        {
            ITokenProvider provider = null;
            foreach (var probe in _probes)
            {
                if (await probe.AvailableAsync().ConfigureAwait(false))
                {
                    provider = await probe.ProviderAsync().ConfigureAwait(false);
                    break;
                }
            }

            if (provider == null)
            {
                throw new NoProbesAvailableException();
            }

            return await provider.GetTokenAsync(scopes).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// NoProbesAvailableException is thrown when the chain of providers doesn't contain any token providers able to fetch a token
    /// </summary>
    public class NoProbesAvailableException : MsalClientException
    {
        private const string Code = "no_probes_are_available";
        private const string ErrorMessage = "All of the IProbes provided were unable to find the variables needed to successfully create a credential provider.";

        /// <summary>
        /// Create a NoProbesAvailableException
        /// </summary>
        public NoProbesAvailableException() : base(Code, ErrorMessage) { }

        /// <summary>
        /// Create a NoProbesAvailableException with an error message
        /// </summary>
        public NoProbesAvailableException(string errorMessage) : base(Code, errorMessage) { }
    }
}
