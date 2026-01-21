// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public static class LabResponseHelper
    {
        public static KeyVaultSecretsProvider KeyVaultSecretsProviderMsal { get; }
        public static KeyVaultSecretsProvider KeyVaultSecretsProviderMsid { get; }

        // Caches for configuration objects retrieved from Key Vault
         private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, UserConfig> s_userConfigCache = new System.Collections.Concurrent.ConcurrentDictionary<string, UserConfig>();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, AppConfig> s_appConfigCache = new System.Collections.Concurrent.ConcurrentDictionary<string, AppConfig>();

        static LabResponseHelper()
        {
            KeyVaultSecretsProviderMsal = new KeyVaultSecretsProvider(KeyVaultInstance.MsalTeam);
            KeyVaultSecretsProviderMsid = new KeyVaultSecretsProvider(KeyVaultInstance.MSIDLab);
        }

        /// <summary>
        /// Retrieves user configuration from Key Vault with caching.
        /// </summary>
        /// <param name="secret">The name of the Key Vault secret containing user configuration JSON.</param>
        /// <returns>A LabUser object deserialized from the Key Vault secret.</returns>
        public static async Task<UserConfig> GetUserConfigAsync(string secret)
        {
            // Check cache first
            if (s_userConfigCache.TryGetValue(secret, out UserConfig cachedConfig))
            {
                Debug.WriteLine($"UserConfig '{secret}' retrieved from cache");
                return cachedConfig;
            }

            try
            {
                var keyVaultSecret = await KeyVaultSecretsProviderMsal.GetSecretByNameAsync(secret).ConfigureAwait(false);
                string userData = keyVaultSecret.Value;

                if (string.IsNullOrEmpty(userData))
                {
                    Debug.WriteLine($"KeyVault secret '{secret}' empty");
                    throw new InvalidOperationException($"Found no content for secret '{secret}' in Key Vault.");
                }

                try
                {
                    // Parse as JObject to extract the 'user' property (case-insensitive)
                    var jsonObject = JObject.Parse(userData);
                    var userToken = jsonObject.GetValue("user", StringComparison.OrdinalIgnoreCase);
                    
                    if (userToken == null)
                    {
                        Debug.WriteLine($"KeyVault '{secret}': no 'user' property found in JSON");
                        throw new InvalidOperationException($"Key Vault secret '{secret}' does not contain a 'user' property.");
                    }

                    var userConfig = userToken.ToObject<UserConfig>() ?? throw new InvalidOperationException($"Failed to deserialize 'user' property from Key Vault secret '{secret}' to LabUser.");
                    Debug.WriteLine($"KeyVault '{secret}': {userConfig.Upn ?? "Unknown user"}");

                    // Cache the result
                    s_userConfigCache[secret] = userConfig;
                    return userConfig;
                }
                catch (JsonException jsonEx)
                {
                    Debug.WriteLine($"KeyVault '{secret}': invalid JSON ({userData.Length} chars) - {jsonEx.Message}");
                    throw new InvalidOperationException($"Key Vault secret '{secret}' contains invalid JSON for LabUser. {jsonEx.Message}", jsonEx);
                }
            }
            catch (Exception e) when (!(e is InvalidOperationException))
            {
                Debug.WriteLine($"KeyVault '{secret}' failed: {e.Message}");
                throw new InvalidOperationException($"Failed to retrieve or parse Key Vault secret '{secret}'. See inner exception.", e);
            }
        }

        /// <summary>
        /// Retrieves app configuration from Key Vault with caching.
        /// </summary>
        /// <param name="secret">The name of the Key Vault secret containing app configuration JSON.</param>
        /// <returns>An AppConfig object deserialized from the Key Vault secret.</returns>
        public static async Task<AppConfig> GetAppConfigAsync(string secret)
        {
            // Check cache first
            if (s_appConfigCache.TryGetValue(secret, out AppConfig cachedConfig))
            {
                Debug.WriteLine($"AppConfig '{secret}' retrieved from cache");
                return cachedConfig;
            }

            try
            {
                var keyVaultSecret = await KeyVaultSecretsProviderMsal.GetSecretByNameAsync(secret).ConfigureAwait(false);
                string appData = keyVaultSecret.Value;

                if (string.IsNullOrEmpty(appData))
                {
                    Debug.WriteLine($"KeyVault secret '{secret}' empty");
                    throw new InvalidOperationException($"Found no content for secret '{secret}' in Key Vault.");
                }

                try
                {
                    // Parse as JObject to extract the 'app' property (case-insensitive)
                    var jsonObject = JObject.Parse(appData);
                    var appToken = jsonObject.GetValue("app", StringComparison.OrdinalIgnoreCase);
                    
                    if (appToken == null)
                    {
                        Debug.WriteLine($"KeyVault '{secret}': no 'app' property found in JSON");
                        throw new InvalidOperationException($"Key Vault secret '{secret}' does not contain an 'app' property.");
                    }

                    var appConfig = appToken.ToObject<AppConfig>() ?? throw new InvalidOperationException($"Failed to deserialize 'app' property from Key Vault secret '{secret}' to AppConfig.");
                    Debug.WriteLine($"KeyVault '{secret}': {appConfig.AppId ?? "Unknown app"}");

                    // Cache the result
                    s_appConfigCache[secret] = appConfig;
                    return appConfig;
                }
                catch (JsonException jsonEx)
                {
                    Debug.WriteLine($"KeyVault '{secret}': invalid JSON ({appData.Length} chars) - {jsonEx.Message}");
                    throw new InvalidOperationException($"Key Vault secret '{secret}' contains invalid JSON for AppConfig. {jsonEx.Message}", jsonEx);
                }
            }
            catch (Exception e) when (!(e is InvalidOperationException))
            {
                Debug.WriteLine($"KeyVault '{secret}' failed: {e.Message}");
                throw new InvalidOperationException($"Failed to retrieve or parse Key Vault secret '{secret}'. See inner exception.", e);
            }
        }

        public static string FetchUserPassword(string userLabName)
        {
            // TODO: Implement caching to avoid repeated Key Vault calls
            if (string.IsNullOrWhiteSpace(userLabName))
            {
                Debug.WriteLine("Password fetch failed: empty lab name");
                throw new InvalidOperationException("Error: lab name is not set on user. Password retrieval failed.");
            }

            if (KeyVaultSecretsProviderMsid == null || KeyVaultSecretsProviderMsal == null)
            {
                Debug.WriteLine("Password fetch failed: KeyVault provider not initialized");
                throw new InvalidOperationException("Error: KeyVault secrets provider is not set");
            }

            try
            {
                var keyVaultSecret = KeyVaultSecretsProviderMsid.GetSecretByName(userLabName);
                string password = keyVaultSecret.Value;
                
                if (!string.IsNullOrEmpty(password))
                {
                    Debug.WriteLine($"Password retrieved for {userLabName} ({password.Length} chars)");
                    return password;
                }
                
                Debug.WriteLine($"Password empty for {userLabName}");
                throw new InvalidOperationException($"Password secret '{userLabName}' found but was empty in Key Vault.");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Password fetch failed for {userLabName}: {e.Message}");
                throw new InvalidOperationException($"Test setup: cannot get the user password from Key Vault secret '{userLabName}'. See inner exception.", e);
            }
        }

        /// <summary>
        /// Retrieves a secret string value from the specified Key Vault.
        /// </summary>
        /// <param name="secretName">The name of the secret to retrieve.</param>
        /// <param name="keyVault">The Key Vault instance to retrieve the secret from.</param>
        /// <returns>The secret value as a string.</returns>
        public static string FetchSecretString(string secretName, KeyVaultSecretsProvider keyVault)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                Debug.WriteLine("Secret fetch failed: empty secret name");
                throw new InvalidOperationException("Error: secret name cannot be empty.");
            }

            if (keyVault == null)
            {
                Debug.WriteLine("Secret fetch failed: KeyVault provider is null");
                throw new InvalidOperationException("Error: KeyVault secrets provider cannot be null.");
            }

            try
            {
                var keyVaultSecret = keyVault.GetSecretByName(secretName);
                string secretValue = keyVaultSecret.Value;
                
                if (!string.IsNullOrEmpty(secretValue))
                {
                    Debug.WriteLine($"Secret retrieved for {secretName} ({secretValue.Length} chars)");
                    return secretValue;
                }
                
                Debug.WriteLine($"Secret empty for {secretName}");
                throw new InvalidOperationException($"Secret '{secretName}' found but was empty in Key Vault.");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Secret fetch failed for {secretName}: {e.Message}");
                throw new InvalidOperationException($"Failed to retrieve Key Vault secret '{secretName}'. See inner exception.", e);
            }
        }
    }
}
