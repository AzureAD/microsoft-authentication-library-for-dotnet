// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SmileTestRunner
{
    public class SmileTestExecutor
    {
        private readonly string _testSource;
        private TestFileContent? _testContent;  // Nullable since it is initialized asynchronously and cannot be guaranteed to be non-null at the end of the constructor.

        private Dictionary<string, object> _variables = new Dictionary<string, object>();
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Creates a new instance of SmileTestExecutor using either a file path or URL
        /// </summary>
        /// <param name="testSource">Path to a local file or a URL pointing to YAML content</param>
        public SmileTestExecutor(string testSource)
        {
            _testSource = testSource;
        }

        /// <summary>
        /// Initializes the test content by loading from either a file or URL
        /// </summary>
        private async Task InitializeTestContentAsync()
        {
            string yamlContent = await GetYamlContentAsync().ConfigureAwait(false);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            _testContent = deserializer.Deserialize<TestFileContent>(yamlContent);
        }

        /// <summary>
        /// Gets the YAML content from either a file or URL
        /// </summary>
        /// <returns>The YAML content as a string</returns>
        private async Task<string> GetYamlContentAsync()
        {
            if (Uri.IsWellFormedUriString(_testSource, UriKind.Absolute))
            {
                // Get content from URL
                HttpResponseMessage response = await _httpClient.GetAsync(_testSource).ConfigureAwait(false);
                response.EnsureSuccessStatusCode(); // Throw if not successful
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                // Get content from local file
                if (!File.Exists(_testSource))
                {
                    throw new FileNotFoundException($"Test file '{_testSource}' not found");
                }

                return await File.ReadAllTextAsync(_testSource).ConfigureAwait(false);
            }
        }

        public async Task<List<bool>> RunTestAsync()
        {
            await InitializeTestContentAsync().ConfigureAwait(false);
            SetupEnvironment();
            CreateArrangeVariables();
            return await ExecuteStepsAsync().ConfigureAwait(false);
            // TODO: Do we need to handle unsetting env variables?
        }

        private void SetupEnvironment()
        {
            if (_testContent?.Env != null) // Use null conditional operator to ensure _testContent is not null
            {
                foreach (var entry in _testContent.Env)
                {
                    Environment.SetEnvironmentVariable(entry.Key, entry.Value);
                }
            }
        }

        private void CreateArrangeVariables()
        {
            if (_testContent?.Arrange == null)
                return;

            foreach (var entry in _testContent.Arrange)
            {
                string variableName = entry.Key;
                object variableValue = CreateObjectFromConfig(entry.Value);
                _variables[variableName] = variableValue;
            }
        }

        private object CreateObjectFromConfig(Dictionary<string, object> config)
        {
            var entry = config.First();
            return CreateMsalObject(entry.Key, JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(entry.Value)));
        }

        private object CreateMsalObject(string className, JsonElement parameters)
        {
            if (className == "ManagedIdentityClient")
            {
                if (parameters.TryGetProperty("managed_identity", out _))
                {
                    // TODO: Properly parse the parameters and set up SAMI or UAMI accordingly
                    var appBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned);
                    
                    //// Disabling shared cache options to avoid cross test pollution.
                    //appBuilder.Config.AccessorOptions = null;

                    // Use reflection to access the private or internal `Config` property
                    var configProperty = typeof(BaseAbstractApplicationBuilder<ManagedIdentityApplicationBuilder>)
                        .GetProperty("Config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (configProperty != null)
                    {
                        var appConfig = configProperty.GetValue(appBuilder); // as ApplicationConfiguration;

                        if (appConfig != null)
                        {
                            //appConfig.AccessorOptions = null; // Disabling shared cache options to avoid cross-test pollution

                            // Use reflection to access the 'AccessorOptions' property
                            var accessorOptionsProperty = appConfig.GetType()
                                .GetProperty("AccessorOptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                            if (accessorOptionsProperty != null)
                            {
                                accessorOptionsProperty.SetValue(appConfig, null); // Disabling shared cache options to avoid cross-test pollution
                            }
                        }
                    }

                    if (parameters.TryGetProperty("client_capabilities", out JsonElement clientCapabilitiesElement))
                    {
                        var clientCapabilities = JsonSerializer.Deserialize<string[]>(clientCapabilitiesElement.GetRawText());
                        appBuilder = appBuilder.WithClientCapabilities(clientCapabilities);
                    }
                    IManagedIdentityApplication managedIdentityApp = appBuilder.Build();
                    return managedIdentityApp;
                }
            }
            throw new NotImplementedException($"{className} is not implemented yet.");
        }

        private async Task<List<bool>> ExecuteStepsAsync()
        {
            var results = new List<bool>();

            if (_testContent?.Steps == null)
                return results;

            foreach (var step in _testContent.Steps)
            {
                if (step.Act == null)
                    continue;

                var actionResult = await ExecuteActionAsync(step.Act).ConfigureAwait(false);
                bool stepPassed = actionResult != null && (
                    step.Assert != null ? AreAssertionsPassed(actionResult, step.Assert) : true);
                results.Add(stepPassed);
            }

            return results;
        }

        private async Task<Dictionary<string, object>?> ExecuteActionAsync(Dictionary<string, object> actionConfig)
        {
            var actionEntry = actionConfig.First();
            string actionString = actionEntry.Key;
            var parameters = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(actionEntry.Value));

            string[] parts = actionString.Split('.');
            string variableName = parts[0];
            string methodName = parts[1];

            if (!_variables.TryGetValue(variableName, out object? obj))
                throw new InvalidOperationException($"Variable '{variableName}' not found");

            // Convert method parameters
            var methodParams = new Dictionary<string, object>();
            if (parameters.ValueKind == JsonValueKind.Object)
            {
                foreach (var param in parameters.EnumerateObject())
                {
                    methodParams[param.Name] = param.Value.Deserialize<object>()!;
                }
            }

            // Match obj via reflection
            if (obj is IManagedIdentityApplication managedIdentityApp)
            {
                if (methodName == "AcquireTokenForManagedIdentity")
                {
                    try
                    {
                        var acquireTokenBuilder = managedIdentityApp.AcquireTokenForManagedIdentity(
                           parameters.GetProperty("resource").GetString()
                        );

                        if (parameters.TryGetProperty("claims_challenge", out JsonElement claimsChallengeElement))
                        {
                            acquireTokenBuilder = acquireTokenBuilder.WithClaims(claimsChallengeElement.GetString());
                        }

                        var result = await acquireTokenBuilder
                           .ExecuteAsync()
                           .ConfigureAwait(false);
                        return new Dictionary<string, object>
                        {
                            ["access_token"] = result.AccessToken,
                            ["token_type"] = result.TokenType,
                            ["token_source"] = TokenSourceHelper.ToSmileString(result.AuthenticationResultMetadata.TokenSource),
                        };
                    }
                    catch (MsalException msalEx)
                    {
                        Console.WriteLine($"{variableName}.{methodName}: MSAL Exception: " +
                            $"\n - Message: {msalEx.Message}" +
                            $"\n - ErrorCode: {msalEx.ErrorCode}" +
                            $"\n - StackTrace: {msalEx.StackTrace}");
                        return null;
                    }
                }
                throw new NotImplementedException($"Method '{methodName}' not implemented for '{variableName}'");
            }
            throw new NotImplementedException($"Variable '{variableName}' not arranged");
        }

        private bool AreAssertionsPassed(Dictionary<string, object> result, Dictionary<string, object> assertions)
        {
            bool allAssertionsPassed = true;
            foreach (KeyValuePair<string, object> assertion in assertions)
            {
                if (!result.TryGetValue(assertion.Key, out object? actualValue) ||
                    !actualValue.Equals(assertion.Value))
                {
                    Console.WriteLine($"Assertion failed for {assertion.Key}. Expected: {assertion.Value}, Got: {actualValue}");
                    allAssertionsPassed = false;
                }
            }
            return allAssertionsPassed;
        }
    }

    public class TestFileContent
    {
        public required string Type { get; set; }
        public string? Ver { get; set; }
        public Dictionary<string, string>? Env { get; set; }
        public Dictionary<string, Dictionary<string, object>>? Arrange { get; set; }
        public required List<TestStep> Steps { get; set; }
    }

    public class TestStep
    {
        public required Dictionary<string, object> Act { get; set; }
        public Dictionary<string, object>? Assert { get; set; }
    }

    public static class TokenSourceHelper
    {
        public static string ToSmileString(TokenSource tokenSource)
        {
            return tokenSource switch
            {
                TokenSource.IdentityProvider => "identity_provider",
                TokenSource.Cache => "cache",
                TokenSource.Broker => "broker",
                _ => "unknown"
            };
        }
    }
}
