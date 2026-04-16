// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    /// <summary>
    /// Represents the configuration for a lab user, including properties such as ObjectId, UserType, UPN, HomeUPN, B2C provider, LabName, FederationProvider, TenantId, AppId, and AzureEnvironment.
    /// </summary>
    public class UserConfig
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        [JsonProperty("objectId")]
        public Guid ObjectId { get; set; }

        /// <summary>
        /// Gets or sets the type of the user.
        /// </summary>
        [JsonProperty("userType")]
        public string UserType { get; set; }

        /// <summary>
        /// Gets or sets the user principal name (UPN) of the user.
        /// </summary>
        [JsonProperty("upn")]
        public string Upn { get; set; }

        /// <summary>
        /// Gets or sets the home user principal name (UPN) of the user.
        /// </summary>
        [JsonProperty("homeupn")]
        public string HomeUPN { get; set; }

        /// <summary>
        /// Gets or sets the B2C provider for the user.
        /// </summary>
        [JsonProperty("b2cprovider")]
        public string B2cProvider { get; set; }

        /// <summary>
        /// Gets or sets the name of the lab.
        /// </summary>
        [JsonProperty("labname")]
        public string LabName { get; set; }

        /// <summary>
        /// Gets or sets the federation provider for the user.
        /// </summary>
        public string FederationProvider { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID for the user.
        /// </summary>
        public string TenantId { get; set; }

        private string _password = null;

        /// <summary>
        /// Gets or sets the application ID for the user.
        /// </summary>
        [JsonProperty("appid")]
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the Azure environment for the user.
        /// </summary>
        [JsonProperty("azureenvironment")]
        public string AzureEnvironment { get; set; }

        /// <summary>
        /// Gets or fetches the password for the user.
        /// </summary>
        public string GetOrFetchPassword()
        {
            if (_password == null)
            {
                _password = LabResponseHelper.FetchUserPassword(LabName);
            }

            return _password;
        }
    }
}
