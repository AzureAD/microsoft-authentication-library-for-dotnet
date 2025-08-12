using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Test.Unit.TestUtils
{
    /// <summary>
    /// Service to retrieve and access test configuration from an external test service.
    /// This allows tests to be configured externally and supports simulation of various
    /// authentication scenarios.
    /// </summary>
    public static class ConfigService
    {
        private static JsonDocument _config;

        /// <summary>
        /// The production service endpoint URL (for future use with real msidlab.com service)
        /// </summary>
        private const string ProductionServiceEndpoint = "https://msidlab.com";

        /// <summary>
        /// The localhost service endpoint URL for development and testing
        /// </summary>
        private const string LocalhostServiceEndpoint = "https://localhost:5001";

        /// <summary>
        /// Flag to determine if the service should use localhost instead of the production endpoint
        /// </summary>
        private static bool _useLocalhost = false;

        /// <summary>
        /// The base endpoint URL for the service
        /// </summary>
        public static string ServiceEndpoint => _useLocalhost ? LocalhostServiceEndpoint : ProductionServiceEndpoint;

        /// <summary>
        /// Gets the IMsalHttpClientFactory used by ConfigService, which ignores SSL certificate validation
        /// when using localhost
        /// </summary>
        public static IMsalHttpClientFactory HttpClientFactory { get; private set; }

        /// <summary>
        /// Initialize the configuration service and fetch configuration for the specified scenario
        /// </summary>
        /// <param name="scenario">Name of the scenario to fetch configuration for, or "all" for everything</param>
        /// <param name="useLocalhost">Whether to use localhost instead of production endpoint</param>
        /// <returns>Task that completes when configuration is loaded</returns>
        public static async Task InitializeAsync(string scenario = "all", bool useLocalhost = false)
        {
            _useLocalhost = useLocalhost;
            
            // Create an HttpClient that ignores SSL certificate validation errors when using localhost
            if (useLocalhost)
            {
                HttpClientFactory = new InsecureHttpClientFactory();
            }
            else
            {
                // Will be expanded to use the actual msidlab.com endpoint in the future,
                // for now only localhost is possible.
            }

            try
            {
                string configUrl = $"{ServiceEndpoint}/api/getConfig/config?scenario={scenario}";
                
                var response = await HttpClientFactory.GetHttpClient().GetStringAsync(configUrl).ConfigureAwait(false);
                _config = JsonDocument.Parse(response);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to connect to the test service. Make sure the service is running at {ServiceEndpoint}.",
                    ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Invalid JSON response from the test service.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration '{scenario}'. Error: {ex.Message}",
                    ex);
            }
        }

        /// <summary>
        /// Navigate to a section in the configuration using a path with dot notation
        /// </summary>
        /// <param name="path">Path to the section, e.g. "authorities.generic"</param>
        /// <returns>JsonElement for the section</returns>
        private static JsonElement NavigateToSection(string path)
        {
            var element = _config.RootElement;
            var parts = path.Split('.');

            foreach (var part in parts)
            {
                if (!element.TryGetProperty(part, out element))
                {
                    throw new InvalidOperationException(
                        $"Configuration section '{part}' not found in path '{path}'.");
                }
            }

            return element;
        }

        /// <summary>
        /// Get a string value from the configuration using a path with dot notation
        /// </summary>
        /// <param name="path">Path to the value, e.g. "authorities.generic.authority"</param>
        /// <returns>The string value at the path</returns>
        /// <exception cref="ArgumentException">If the path is null or empty</exception>
        /// <exception cref="InvalidOperationException">If the value is not a string or the path doesn't exist</exception>
        public static string GetString(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            var element = NavigateToSection(path);
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }

            throw new InvalidOperationException(
                $"Configuration value at '{path}' is not a string but is {element.ValueKind}.");
        }

        /// <summary>
        /// Get the authority URL for a given authority key
        /// </summary>
        /// <param name="authorityKey">Key of the authority, e.g. "generic", "entra", "ciam"</param>
        /// <returns>The authority URL</returns>
        public static string GetAuthority(string authorityKey)
        {
            if (string.IsNullOrWhiteSpace(authorityKey))
            {
                throw new ArgumentException("Authority key cannot be null or empty.", nameof(authorityKey));
            }

            return GetString($"authorities.{authorityKey}.authority");
        }

        /// <summary>
        /// Get the issuer URL for a given authority key
        /// </summary>
        /// <param name="authorityKey">Key of the authority, e.g. "generic", "entra", "ciam"</param>
        /// <returns>The issuer URL</returns>
        public static string GetIssuer(string authorityKey)
        {
            if (string.IsNullOrWhiteSpace(authorityKey))
            {
                throw new ArgumentException("Authority key cannot be null or empty.", nameof(authorityKey));
            }

            return GetString($"authorities.{authorityKey}.issuer");
        }

        /// <summary>
        /// Get the client ID from the configuration
        /// </summary>
        /// <returns>The client ID</returns>
        public static string GetClientId() => GetString("clientCredentials.client_id");

        /// <summary>
        /// Get the client secret from the configuration
        /// </summary>
        /// <returns>The client secret</returns>
        public static string GetClientSecret() => GetString("clientCredentials.client_secret");

        /// <summary>
        /// Get the URI to the test service with the specified path appended
        /// </summary>
        /// <param name="path">Path to append</param>
        /// <returns>Full URI to the test service</returns>
        public static string GetServiceUri(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return ServiceEndpoint;
            }

            // Ensure path starts with a forward slash
            string normalizedPath = path.StartsWith("/") ? path : $"/{path}";

            return $"{ServiceEndpoint}{normalizedPath}";
        }

        /// <summary>
        /// Get the response type URI from the configuration
        /// </summary>
        /// <param name="responseTypeKey">Key of the response type, e.g. "oidc_response_successful"</param>
        /// <returns>The response type URI</returns>
        public static string GetResponseTypeUri(string responseTypeKey)
        {
            if (string.IsNullOrWhiteSpace(responseTypeKey))
            {
                throw new ArgumentException("Response type key cannot be null or empty.", nameof(responseTypeKey));
            }

            return GetString($"responseTypes.{responseTypeKey}");
        }

        /// <summary>
        /// Get a test scenario directly by its name
        /// </summary>
        /// <param name="scenarioName">The name of the scenario (e.g. "serviceFabricRevocation" or "nonMatchingIssuer")</param>
        /// <returns>A test scenario configuration object</returns>
        public static TestScenario GetScenario(string scenarioName)
        {
            if (string.IsNullOrWhiteSpace(scenarioName))
            {
                throw new ArgumentException("Scenario name cannot be null or empty.", nameof(scenarioName));
            }

            string basePath = $"testScenarios.{scenarioName}";
            return new TestScenario(basePath);
        }

        /// <summary>
        /// Class representing a test scenario with its configuration values and helper methods
        /// </summary>
        public class TestScenario
        {
            private readonly string _basePath;

            /// <summary>
            /// Creates a new test scenario with the given base path
            /// </summary>
            /// <param name="basePath">The base path to the scenario in the configuration</param>
            public TestScenario(string basePath)
            {
                _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            }

            /// <summary>
            /// Get a value from this test scenario
            /// </summary>
            /// <param name="key">The key of the value to retrieve</param>
            /// <returns>The value as a string</returns>
            public string GetValue(string key) => GetString($"{_basePath}.{key}");

            /// <summary>
            /// The authority key for this scenario, used to look up authority and issuer URLs
            /// </summary>
            public string AuthorityKey => GetValue("authorityKey");

            /// <summary>
            /// The response type key for this scenario, used to determine which endpoint to call
            /// </summary>
            public string ResponseType => GetValue("responseType");

            /// <summary>
            /// The resource value for token acquisition scenarios
            /// </summary>
            public string Resource => GetValue("resource");

            /// <summary>
            /// Create a service URI for this scenario's response type
            /// </summary>
            /// <param name="additionalPath">Optional additional path to append</param>
            /// <returns>The full service URI</returns>
            public string CreateServiceUri(string additionalPath = null)
            {
                string path = GetResponseTypeUri(ResponseType);
                
                if (string.IsNullOrEmpty(additionalPath))
                {
                    return GetServiceUri(path);
                }
                
                return GetServiceUri($"{path}/{additionalPath}");
            }

            /// <summary>
            /// Create an OIDC authority URI with encoded issuer
            /// </summary>
            /// <returns>The full OIDC authority URI</returns>
            public string CreateOidcAuthorityUri()
            {
                var issuerUrl = GetIssuer(AuthorityKey);
                var encodedIssuer = System.Net.WebUtility.UrlEncode(issuerUrl);
                return CreateServiceUri(encodedIssuer);
            }

            /// <summary>
            /// Create an identity provider URI for token revocation scenarios
            /// </summary>
            /// <returns>The identity provider URI</returns>
            public string CreateIdentityProviderUri()
            {
                string servicePath = GetValue("servicePath");
                string tokenResponsePath = GetResponseTypeUri("token_successful");
                return GetServiceUri($"{servicePath}{tokenResponsePath}");
            }

            /// <summary>
            /// Create a revocation endpoint URI for token revocation scenarios
            /// </summary>
            /// <returns>The revocation endpoint URI</returns>
            public string CreateRevocationEndpointUri()
            {
                string revocationPath = GetValue("revocationEndpoint");
                return GetServiceUri(revocationPath);
            }
        }
        
        /// <summary>
        /// HttpClientFactory that ignores SSL certificate validation for test purposes
        /// </summary>
        public class InsecureHttpClientFactory : IMsalHttpClientFactory
        {
            /// <summary>
            /// Get an HttpClient with SSL certificate validation disabled
            /// </summary>
            /// <returns>HttpClient instance</returns>
            public HttpClient GetHttpClient()
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
            
                return new HttpClient(handler);
            }
        }
    }
}
