using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.Utils;
using System.Text;

namespace Microsoft.Identity.Client.Azure
{
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms

    /// <summary>
    ///     ManagedIdentityProbe will look in environment variable to determine if the managed identity provider is
    ///     available. If the managed identity provider is available, the probe will allow for building a managed identity
    ///     credential provider to fetch AAD tokens using the IMDS endpoint.
    /// </summary>
    public class ManagedIdentityProbe : IProbe
    {

        private readonly HttpClient _httpClient;
        private readonly IManagedIdentityConfiguration _config;
        private readonly string _overrideClientId;

        internal ManagedIdentityProbe(HttpClient httpClient, IManagedIdentityConfiguration config = null, string overrideClientId = null)
        {
            _httpClient = httpClient;
            _config = config ?? new DefaultManagedIdentityConfiguration();
            _overrideClientId = overrideClientId;
        }

        /// <summary>
        ///     Create a Managed Identity probe with a specified client identity
        /// </summary>
        /// <param name="config">option configuration structure -- if not supplied, a default environmental configuration is used.</param>
        /// <param name="overrideClientId">override the client identity found in the config for use when querying the Azure IMDS endpoint</param>
        public ManagedIdentityProbe(IManagedIdentityConfiguration config = null, string overrideClientId = null)
            : this(null, config, overrideClientId) { }

        /// <summary>
        ///     Check if the probe is available for use in the current environment
        /// </summary>
        /// <returns>True if a credential provider can be built</returns>
        public async Task<bool> AvailableAsync()
        {
            // check App Service MSI
            if (IsAppService())
            {
                return true;
            }

#if net45  || NETSTANDARD2_0

            // Check if there is no service listening on VM IP
            //
            // This is a performance optimization to avoid retrying requests to the Managed Identity service

            if(!IsVMManagedIdentityListening())
            {
                return false;
            }
#endif

            try
            {
                // if service is listening on VM IP check if a token can be acquired
                var provider = BuildProvider(maxRetries: 2, httpClient: _httpClient);
                var token = await provider.GetTokenAsync(new List<string> { @"https://management.azure.com//.default" }).ConfigureAwait(false);
                return token != null;
            }
            catch (TooManyRetryAttemptsException)
            {
                return false;
            }
        }

        /// <summary>
        /// IsAppService tells us if we are executing within AppService with Managed Identities enabled
        /// </summary>
        /// <returns></returns>
        private bool IsAppService()
        {
            var vars = new List<string> { _config.ManagedIdentitySecret, _config.ManagedIdentityEndpoint };
            return vars.All(item => !string.IsNullOrWhiteSpace(item));
        }

#if net45 || NETSTANDARD2_0
        private static bool IsVMManagedIdentityListening()
        {
            var loopback = IPAddress.Parse(Constants.ManagedIdentityLoopbackAddress);
            foreach (var tcpEndpoint in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
            {
                if (tcpEndpoint.Address == loopback && tcpEndpoint.Port == 80)
                {
                    return true;
                }
            }
            return false;
        }
#endif

        /// <summary>
        ///     Create a managed identity credential provider from the information discovered by the probe
        /// </summary>
        /// <returns>A managed identity credential provider instance</returns>
        public async Task<ITokenProvider> ProviderAsync()
        {
            if (!await AvailableAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException("The required environment variables are not available.");
            }

            return BuildProvider(httpClient: _httpClient);
        }

        private ITokenProvider BuildProvider(int maxRetries = 5, HttpClient httpClient = null)
        {
            var endpoint = IsAppService() ? _config.ManagedIdentityEndpoint : Constants.ManagedIdentityTokenEndpoint;
            return new ManagedIdentityCredentialProvider(endpoint, httpClient: httpClient, secret: _config.ManagedIdentitySecret, clientId: ClientId, maxRetries: maxRetries);
        }

        private string ClientId
        {
            get { return string.IsNullOrWhiteSpace(_overrideClientId) ? _config.ClientId : _overrideClientId; }
        }
    }

    /// <summary>
    ///     ManagedIdentityCredentialProvider will fetch AAD JWT tokens from the IMDS endpoint for the default client id or
    ///     a specified client id.
    /// </summary>
    public class ManagedIdentityCredentialProvider : ITokenProvider
    {
        private static readonly HttpClient DefaultClient;

        private readonly ManagedIdentityClient _client;

        static ManagedIdentityCredentialProvider()
        {
            DefaultClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(100) // 100 milliseconds -- make sure there is an extremely short timeout to ensure we fail fast
            };
        }

        internal ManagedIdentityCredentialProvider(string endpoint, HttpClient httpClient = null, string secret = null, string clientId = null, int maxRetries = 5)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                _client = new ManagedIdentityVMClient(endpoint, httpClient ?? DefaultClient, clientId: clientId, maxRetries: maxRetries);
            }
            else
            {
                _client = new ManagedIdentityAppServiceClient(endpoint, secret, httpClient ?? DefaultClient, maxRetries: maxRetries);
            }
        }

        /// <summary>
        /// Create an instance of a ManagedIdentityCredentialProvider which will talk to either AppService or VM managed identity token endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="secret"></param>
        /// <param name="clientId"></param>
        /// <param name="maxRetries"></param>
        public ManagedIdentityCredentialProvider(string endpoint, string secret = null, string clientId = null, int maxRetries = 5)
            : this(endpoint, httpClient: DefaultClient, secret: secret, clientId: clientId, maxRetries: maxRetries) { }

        /// <summary>
        ///     GetTokenAsync returns a token for a given set of scopes
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>A token with expiration</returns>
        public async Task<IToken> GetTokenAsync(IEnumerable<string> scopes = null)
        {
            var resourceUriInScopes = scopes?.FirstOrDefault(i => i.EndsWith(@"/.default", StringComparison.OrdinalIgnoreCase));
            if (resourceUriInScopes == null)
            {
                throw new NoResourceUriInScopesException();
            }

            return await _client.FetchTokenWithRetryAsync(resourceUriInScopes).ConfigureAwait(false);
        }
    }


    /// <summary>
    /// NoResourceUriInScopesException is thrown when the managed identity token provider does not find a .default
    /// scope for a resource in the enumeration of scopes.
    /// </summary>
    public class NoResourceUriInScopesException : MsalClientException
    {
        private const string Code = "no_resource_uri_with_slash_.default_in_scopes";
        private const string ErrorMessage = "The scopes provided is either empty or none that end in `/.default`.";

        /// <summary>
        /// Create a TooManyRetryAttemptsException
        /// </summary>
        public NoResourceUriInScopesException() : base(Code, ErrorMessage) { }

        /// <summary>
        /// Create a TooManyRetryAttemptsException with an error message
        /// </summary>
        public NoResourceUriInScopesException(string errorMessage) : base(Code, errorMessage) { }
    }

    internal abstract class ManagedIdentityClient
    {
        protected const string FailedParseOfManagedIdentityExpiration = "failed_parse_of_managed_identity_token_expiry";

        protected readonly string _endpoint;
        protected readonly int _maxRetries;
        protected readonly HttpClient _client;

        internal ManagedIdentityClient(string endpoint, HttpClient client, int maxRetries = 5)
        {
            _endpoint = endpoint;
            _client = client;
            _maxRetries = maxRetries;
        }

        protected abstract HttpRequestMessage BuildTokenRequest(string resourceUri);

        public async Task<IToken> FetchTokenWithRetryAsync(string resourceUri)
        {
            var strategy = new RetryWithExponentialBackoff(_maxRetries, 50, 60000);
            HttpResponseMessage res = null;
            await strategy.RunAsync(async () =>
            {
                var req = BuildTokenRequest(resourceUri);
                res = await _client.SendAsync(req).ConfigureAwait(false);

                var intCode = (int)res.StatusCode;
                switch (intCode) {
                    case 404:
                    case 429:
                    case var _ when intCode >= 500:
                        throw new TransientManagedIdentityException($"encountered transient managed identity service error with status code {intCode}");
                    case 400:
                        throw new BadRequestManagedIdentityException();
                }
            }).ConfigureAwait(false);

            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            if(string.IsNullOrEmpty(json))
            {
                return null;
            }

            var tokenRes = TokenResponse.Parse(json);
            var startOfUnixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            if (!double.TryParse(tokenRes.ExpiresOn, out var seconds))
            {
                throw new MsalException(FailedParseOfManagedIdentityExpiration, $"unable to parse the expires on token into an int: {tokenRes.ExpiresOn}");
            }
            return new AccessTokenWithExpiration { ExpiresOn = startOfUnixTime.AddSeconds(seconds), AccessToken = tokenRes.AccessToken };
        }
    }

    internal class ManagedIdentityVMClient : ManagedIdentityClient
    {
        private readonly string _clientId;

        public ManagedIdentityVMClient(string endpoint, HttpClient client, string clientId = null, int maxRetries = 10) : base(endpoint, client, maxRetries)
        {
            _clientId = clientId;
        }

        protected override HttpRequestMessage BuildTokenRequest(string resourceUri)
        {
            string clientIdParameter = string.IsNullOrWhiteSpace(_clientId)
                    ? string.Empty :
                    $"&client_id={_clientId}";

            var requestUri = $"{_endpoint}?resource={resourceUri}{clientIdParameter}&api-version={Constants.ManagedIdentityVMApiVersion}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("Metadata", "true");
            return request;
        }
    }

    internal class ManagedIdentityAppServiceClient : ManagedIdentityClient
    {
        private readonly string _secret;

        public ManagedIdentityAppServiceClient(string endpoint, string secret, HttpClient client, int maxRetries = 5) : base(endpoint, client, maxRetries)
        {
            _secret = secret;
        }

        protected override HttpRequestMessage BuildTokenRequest(string resourceUri)
        {
            var requestUri = $"{_endpoint}?resource={resourceUri}&api-version={Constants.ManagedIdentityAppServiceApiVersion}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("Secret", _secret);
            return request;
        }
    }

    /// <summary>
    /// Used to hold the deserialized token response.
    /// </summary>
    [DataContract]
    internal class TokenResponse
    {
        private const string TokenResponseFormatExceptionMessage = "Token response is not in the expected format.";

        internal enum DateFormat
        {
            Unix,
            DateTimeString
        };

        // MSI endpoint return access_token
        [DataMember(Name = "access_token", IsRequired = false)]
        public string AccessToken { get; private set; }

        // MSI endpoint return expires_on
        [DataMember(Name = "expires_on", IsRequired = false)]
        public string ExpiresOn { get; private set; }

        [DataMember(Name = "error_description", IsRequired = false)]
        public string ErrorDescription { get; private set; }

        // MSI endpoint return token_type
        [DataMember(Name = "token_type", IsRequired = false)]
        public string TokenType { get; private set; }

        /// <summary>
        /// Parse token response returned from OAuth provider.
        /// While more fields are returned, we only need the access token.
        /// </summary>
        /// <param name="tokenResponse">This is the response received from OAuth endpoint that has the access token in it.</param>
        /// <returns></returns>
        public static TokenResponse Parse(string tokenResponse)
        {
            try
            {
                return JsonHelper.DeserializeFromJson<TokenResponse>(Encoding.UTF8.GetBytes(tokenResponse));
            }
            catch (Exception exp)
            {
                throw new FormatException($"{TokenResponseFormatExceptionMessage} Exception Message: {exp.Message}");
            }
        }
    }

    /// <summary>
    /// IManagedIdentityConfiguration provides the configurable properties for the ManagedIdentityProbe
    /// </summary>
    public interface IManagedIdentityConfiguration
    {
        /// <summary>
        /// ManagedIdentitySecret is the secret for use in Azure AppService
        /// </summary>
        string ManagedIdentitySecret { get; }

        /// <summary>
        /// ManagedIdentityEndpoint is the AppService endpoint
        /// </summary>
        string ManagedIdentityEndpoint { get; }

        /// <summary>
        /// VMManagedIdentityEndpoint is the VM's default managed identity endpoint
        /// </summary>
        string VMManagedIdentityEndpoint { get; }

        /// <summary>
        /// ClientId is the user assigned managed identity for use in VM managed identity
        /// </summary>
        string ClientId { get; }
    }

    internal class DefaultManagedIdentityConfiguration : IManagedIdentityConfiguration
    {
        public string ManagedIdentitySecret => Env.ManagedIdentitySecret;

        public string ManagedIdentityEndpoint => Env.ManagedIdentityEndpoint;

        public string ClientId => Env.ClientId;

        public string VMManagedIdentityEndpoint => Constants.ManagedIdentityTokenEndpoint;
    }

#endif
}
