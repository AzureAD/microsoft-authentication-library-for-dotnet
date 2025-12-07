// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Integration.NetFx.Infrastructure
{
    public enum Cloud
    {
        Public,
        Adfs,
        PPE,
        Arlington,
        PublicLegacy  // For regional tests that need original MSIDLAB4 configuration
    }

    public interface IConfidentialAppSettings
    {
        string ClientId { get; }
        string TenantId { get; }
        string Environment { get; }
        string[] AppScopes { get; }
        X509Certificate2 Certificate { get; }
        string Secret { get; }

        string Authority { get; }

        Cloud Cloud { get; }
        bool UseAppIdUri { get; set; }

        bool InstanceDiscoveryEndpoint { get; set; }
    }    

    public class ConfidentialAppSettings
    {
        public const string ID4SLab1TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";

        private class PublicCloudConfidentialAppSettings : IConfidentialAppSettings
        {
            // TODO: Tenant Migration - Migrated to new id4slab1 tenant for non-regional tests
            // Regional tests still use legacy configuration due to AADSTS100007 restrictions
            public string ClientId => UseAppIdUri? "api://54a2d933-8bf8-483b-a8f8-0a31924f3c1f" : "54a2d933-8bf8-483b-a8f8-0a31924f3c1f"; // MSAL-APP-AzureADMultipleOrgs in ID4SLAB1 tenant

            public string TenantId => ID4SLab1TenantId; 

            public string Environment => "login.microsoftonline.com";

            public string[] AppScopes => new[] { "https://vault.azure.net/.default" };

            public string Authority => $@"https://{Environment}/{TenantId}";

            public Cloud Cloud => Cloud.Public;

            public bool UseAppIdUri { get; set; }

            public bool InstanceDiscoveryEndpoint { get; set; } = true;

            public X509Certificate2 Certificate => GetCertificateLazy(TestConstants.AutomationTestCertName).Value;

            public string Secret => 
                GetSecretLazy(KeyVaultInstance.MsalTeam, "MSAL-APP-AzureADMultipleOrgs").Value;
        }

        private class AdfsConfidentialAppSettings : IConfidentialAppSettings
        {
            private const string AdfsCertName = "IDLABS-APP-Confidential-Client-Cert-OnPrem";
            private static readonly Lazy<LabResponse> s_adfsLabResponse = new Lazy<LabResponse>(() =>
            {
                return LabUserHelper.GetDefaultAdfsUserAsync().GetAwaiter().GetResult();
            });

            public string ClientId => s_adfsLabResponse.Value.App.AppId;

            public string TenantId => "";

            public string Environment => "fs.id4slab1.com/adfs";

            public string[] AppScopes => new[] { "openid", "profile" };

            public X509Certificate2 Certificate => s_certLazy.Value;

            private static Lazy<X509Certificate2> s_certLazy => new Lazy<X509Certificate2>(() =>
            {
                KeyVaultSecretsProvider kv = new KeyVaultSecretsProvider();
                return kv.GetCertificateWithPrivateMaterialAsync(AdfsCertName).GetAwaiter().GetResult();
            });

            public string Secret =>
                // Use the default app secret from the lab response
                GetSecretLazy(KeyVaultInstance.MsalTeam, "MSAL-App-Default").Value;

            public string Authority => $@"https://{Environment}";

            public Cloud Cloud => Cloud.Adfs;

            public bool UseAppIdUri { get; set; }

            public bool InstanceDiscoveryEndpoint { get; set; } = true;
        }

        private class PpeConfidentialAppSettings : IConfidentialAppSettings
        {
            public string ClientId => UseAppIdUri ? "api://microsoft.identity.9793041b-9078-4942-b1d2-babdc472cc0c" : "1e999007-0c4f-4242-9ca1-8e33397236a9";

            public string TenantId => "19eea2f8-e17a-470f-954d-d897c47f311c";

            public string Environment => "login.windows-ppe.net";

            public string[] AppScopes => new[] { $"{ClientId}/.default" };

            public X509Certificate2 Certificate => GetCertificateLazy(TestConstants.AutomationTestCertName).Value;

            public string Secret => throw new NotImplementedException();
            public string Authority => $@"https://{Environment}/{TenantId}";

            public Cloud Cloud => Cloud.PPE;

            public bool UseAppIdUri { get; set; }

            public bool InstanceDiscoveryEndpoint { get; set; } = true;
        }

        private class ArlingtonConfidentialAppSettings : IConfidentialAppSettings
        {
            public string ClientId => "c0555d2d-02f2-4838-802e-3463422e571d";

            public string TenantId => "45ff0c17-f8b5-489b-b7fd-2fedebbec0c4";

            public string Environment => "login.microsoftonline.us";

            public string[] AppScopes => new[] { "https://graph.microsoft.com/.default" };

            public X509Certificate2 Certificate => GetCertificateLazy(TestConstants.AutomationTestCertName).Value;

            public string Secret => GetSecretLazy(KeyVaultInstance.MSIDLab, TestConstants.MsalArlingtonCCAKeyVaultSecretName).Value;

            public string Authority => $@"https://{Environment}/{TenantId}";

            public Cloud Cloud => Cloud.Arlington;

            public bool UseAppIdUri { get; set; }

            public bool InstanceDiscoveryEndpoint { get; set; } = true;
        }   

        private class PublicLegacyCloudConfidentialAppSettings : IConfidentialAppSettings
        {
            // Legacy MSIDLAB4 configuration for regional tests only
            // Regional endpoints require original tenant due to AADSTS100007 restrictions
            public string ClientId => UseAppIdUri? "api://88f91eac-c606-4c67-a0e2-a5e8a186854f" : "88f91eac-c606-4c67-a0e2-a5e8a186854f"; // Legacy MSAL app in MSIDLAB4 tenant

            public string TenantId => "f645ad92-e38d-4d1a-b510-d1b09a74a8ca"; // MSIDLAB4 tenant (legacy)

            public string Environment => "login.microsoftonline.com";

            public string[] AppScopes => new[] { "https://vault.azure.net/.default" };

            public string Authority => $@"https://{Environment}/{TenantId}";

            public Cloud Cloud => Cloud.PublicLegacy;

            public bool UseAppIdUri { get; set; }

            public bool InstanceDiscoveryEndpoint { get; set; } = true;

            public X509Certificate2 Certificate => GetCertificateLazy(TestConstants.AutomationTestCertName).Value;

            public string Secret => GetSecretLazy(KeyVaultInstance.MSIDLab, TestConstants.MsalCCAKeyVaultSecretName).Value;
        }

        private static Lazy<IConfidentialAppSettings> s_publicCloudSettings =
            new Lazy<IConfidentialAppSettings>(() => new PublicCloudConfidentialAppSettings());
        
        private static Lazy<IConfidentialAppSettings> s_publicLegacyCloudSettings =
            new Lazy<IConfidentialAppSettings>(() => new PublicLegacyCloudConfidentialAppSettings());
        
        private static Lazy<IConfidentialAppSettings> s_ppeCloudSettings =
            new Lazy<IConfidentialAppSettings>(() => new PpeConfidentialAppSettings());
        
        private static Lazy<IConfidentialAppSettings> s_arlingtonCloudSettings =
            new Lazy<IConfidentialAppSettings>(() => new ArlingtonConfidentialAppSettings());
        
        private static Lazy<IConfidentialAppSettings> s_adfsCloudSettings =
          new Lazy<IConfidentialAppSettings>(() => new AdfsConfidentialAppSettings());

        public static IConfidentialAppSettings GetSettings(Cloud cloud)
        {
            switch (cloud)
            {
                case Cloud.Public:
                    return s_publicCloudSettings.Value;
                case Cloud.PublicLegacy:
                    return s_publicLegacyCloudSettings.Value;
                case Cloud.PPE:
                    return s_ppeCloudSettings.Value;
                case Cloud.Arlington:
                    return s_arlingtonCloudSettings.Value;
                case Cloud.Adfs:
                    return s_adfsCloudSettings.Value;
                default:
                    throw new NotImplementedException();
            }
        }

        private static Lazy<string> GetSecretLazy(string keyVaultInstance, string secretName) => new Lazy<string>(() =>
        {
            var keyVault = new KeyVaultSecretsProvider(keyVaultInstance);
            var secret = keyVault.GetSecretByName(secretName).Value;
            return secret;
        });

        public static Lazy<X509Certificate2> GetCertificateLazy(string certName) => new Lazy<X509Certificate2>(() =>
        {
            X509Certificate2 cert = CertificateHelper.FindCertificateByName(certName);
            if (cert == null)
            {
                throw new InvalidOperationException(
                    "Test setup error - cannot find a certificate in the My store for KeyVault. This is available for Microsoft employees only.");
            }

            return cert;
        });

    }
}
