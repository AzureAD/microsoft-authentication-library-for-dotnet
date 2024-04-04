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
        Arlington
    }

    public interface IConfidentialAppSettings
    {
        string ClientId { get; }
        string TenantId { get; }
        string Environment { get; }
        string[] AppScopes { get; }
        X509Certificate2 GetCertificate();
        string GetSecret();

        string Authority { get; }

        Cloud Cloud { get; }
        bool UseAppIdUri { get; set; }

        bool InstanceDiscoveryEndpoint { get; set; }
    }    

    public class ConfidentialAppSettings
    {
        private class PublicCloudConfidentialAppSettings : IConfidentialAppSettings
        {
            public string ClientId => UseAppIdUri? "https://request.msidlab.com" : "f62c5ae3-bf3a-4af5-afa8-a68b800396e9";

            public string TenantId => "72f988bf-86f1-41af-91ab-2d7cd011db47";

            public string Environment => "login.microsoftonline.com";

            public string[] AppScopes => new[] { "https://vault.azure.net/.default" };

            public string Authority => $@"https://{Environment}/{TenantId}";

            public Cloud Cloud => Cloud.Public;

            public bool UseAppIdUri { get; set; }

            public bool InstanceDiscoveryEndpoint { get; set; } = true;

            public X509Certificate2 GetCertificate()
            {
                return GetCertificateLazy(TestConstants.AutomationTestCertName).Value;
            }

            public string GetSecret()
            {
                return GetSecretLazy(KeyVaultInstance.MsalTeam, TestConstants.MsalCCAKeyVaultSecretName).Value;
            }
        }

        private class AdfsConfidentialAppSettings : IConfidentialAppSettings
        {
            private const string AdfsCertName = "IDLABS-APP-Confidential-Client-Cert-OnPrem";

            public string ClientId => Adfs2019LabConstants.ConfidentialClientId;

            public string TenantId => "";

            public string Environment => "fs.msidlab8.com/adfs";

            public string[] AppScopes => new[] { "openid", "profile" };

            public X509Certificate2 GetCertificate()
            {
                return s_certLazy.Value;
            }

            private static Lazy<X509Certificate2> s_certLazy => new Lazy<X509Certificate2>(() =>
            {
                KeyVaultSecretsProvider kv = new KeyVaultSecretsProvider();
                return kv.GetCertificateWithPrivateMaterialAsync(AdfsCertName).GetAwaiter().GetResult();
            });

            public string GetSecret()
            {                
                return GetSecretLazy(KeyVaultInstance.MsalTeam, Adfs2019LabConstants.ADFS2019ClientSecretName).Value;
            }

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

            public X509Certificate2 GetCertificate()
            {
                return GetCertificateLazy(TestConstants.AutomationTestCertName).Value;
            }

            public string GetSecret()
            {
                throw new NotImplementedException();
            }
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

            public string[] AppScopes => new[] { "https://vault.azure.net/.default" };

            public X509Certificate2 GetCertificate()
            {
                return GetCertificateLazy(TestConstants.AutomationTestCertName).Value;
            }

            public string GetSecret()
            {
                return GetSecretLazy(KeyVaultInstance.MSIDLab, TestConstants.MsalArlingtonCCAKeyVaultSecretName).Value;
            }

            public string Authority => $@"https://{Environment}/{TenantId}";

            public Cloud Cloud => Cloud.Arlington;

            public bool UseAppIdUri { get; set; }

            public bool InstanceDiscoveryEndpoint { get; set; } = true;
        }   

        private static Lazy<IConfidentialAppSettings> s_publicCloudSettings =
            new Lazy<IConfidentialAppSettings>(() => new PublicCloudConfidentialAppSettings());
        
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
