// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Unit;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Azure.Identity;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public static class LabAuthenticationHelper
    {
        private const string LabAccessConfidentialClientId = "f62c5ae3-bf3a-4af5-afa8-a68b800396e9";
        private const string MsftTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        private const string ServiceConnectionResourceId = "6eeeb73d-37aa-4d78-83b7-728101b8bddd";

        public static async Task<AccessToken> GetAccessTokenForLabAPIAsync(string labAccessClientId)
        {
            string[] scopes = new string[] { "https://msidlab.com/.default" };

            return await GetLabAccessTokenAsync(
                "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/", 
                scopes,  
                labAccessClientId).ConfigureAwait(false);
        }

        public static async Task<AccessToken> GetLabAccessTokenAsync(string authority, string[] scopes)
        {
            return await GetLabAccessTokenAsync(
                authority,
                scopes,
                string.Empty).ConfigureAwait(false);
        }

        public static async Task<AccessToken> GetLabAccessTokenAsync(string authority, string[] scopes, string clientId)
        {
            var clientIdForAuth = string.IsNullOrEmpty(clientId) ? LabAccessConfidentialClientId : clientId;
            var credential = new AzurePipelinesCredential(MsftTenantId, clientIdForAuth, ServiceConnectionResourceId);
            var tokenRequestContext = new TokenRequestContext(scopes);

            try
            {
                AccessToken accessToken = await credential.GetTokenAsync(tokenRequestContext, default).ConfigureAwait(false);
                return accessToken;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to acquire an Azure AD token", ex);
            }
        }
    }

    public enum LabAccessAuthenticationType
    {
        ClientCertificate,
        ClientSecret,
        UserCredential
    }
}
