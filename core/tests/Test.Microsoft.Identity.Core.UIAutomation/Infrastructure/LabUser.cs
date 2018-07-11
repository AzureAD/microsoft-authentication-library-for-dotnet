using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Test.Microsoft.Identity.Core.UIAutomation.infrastructure
{
    public class LabUser : IUser
    {
        private KeyVaultSecretsProvider keyVault;

        public LabUser()
        {
            keyVault = new KeyVaultSecretsProvider();
        }

        [JsonProperty("objectId")]
        public Guid ObjectId { get; set; }

        [JsonProperty("userType")]
        public UserType UserType { get; set; }

        [JsonProperty("upn")]
        public string Upn { get; set; }

        [JsonProperty("credentialVaultKeyName")]
        public string CredentialUrl { get; set; }

        public IUser HomeUser { get; set; }

        [JsonProperty("external")]
        public bool IsExternal { get; set; }

        [JsonProperty("mfa")]
        public bool IsMfa { get; set; }

        [JsonProperty("mam")]
        public bool IsMam { get; set; }

        [JsonProperty("licenses")]
        public ISet<string> Licenses { get; set; }

        [JsonProperty("isFederated")]
        public bool IsFederated { get; set; }

        [JsonProperty("federationProvider")]
        public FederationProvider FederationProvider { get; set; }

        [JsonProperty("tenantId")]
        public string CurrentTenantId { get; set; }

        [JsonProperty("hometenantId")]
        public string HomeTenantId { get; set; }

        [JsonProperty("homeUPN")]
        public string HomeUPN { get; set; }

        public void InitializeHomeUser()
        {
            HomeUser = new LabUser();
            var labHomeUser = (LabUser)HomeUser;

            labHomeUser.ObjectId = ObjectId;
            labHomeUser.UserType = UserType;
            labHomeUser.CredentialUrl = CredentialUrl;
            labHomeUser.HomeUser = labHomeUser;
            labHomeUser.IsExternal = IsExternal;
            labHomeUser.IsMfa = IsMfa;
            labHomeUser.IsMam = IsMam;
            labHomeUser.Licenses = Licenses;
            labHomeUser.IsFederated = IsFederated;
            labHomeUser.FederationProvider = FederationProvider;
            labHomeUser.HomeTenantId = HomeTenantId;
            labHomeUser.HomeUPN = HomeUPN;
            labHomeUser.CurrentTenantId = HomeTenantId;
            labHomeUser.Upn = HomeUPN;
        }

        /// <summary>
        /// Gets password from MSID Lab Keyvault
        /// </summary>
        /// <returns>password</returns>
        public string GetPassword()
        {
            if (String.IsNullOrWhiteSpace(CredentialUrl))
            {
                throw new Exception("Error: CredentialUrl is not set on user. Password retrieval failed.");
            }


            return keyVault.GetSecret(CredentialUrl).Value;
        }
    }

    public class LabResponse
    {
        [JsonProperty("Users")]
        public LabUser Users { get; set; }
    }
}
