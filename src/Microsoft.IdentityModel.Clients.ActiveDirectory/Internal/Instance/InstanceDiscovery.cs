//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [DataContract]
    internal sealed class InstanceDiscoveryResponse
    {
        [DataMember(Name = "tenant_discovery_endpoint")]
        public string TenantDiscoveryEndpoint { get; set; }

        [DataMember(Name = "metadata")]
        public InstanceDiscoveryMetadataEntry[] Metadata { get; set; }
    }

    [DataContract]
    internal sealed class InstanceDiscoveryMetadataEntry
    {
        [DataMember(Name = "preferred_network")]
        public string PreferredNetwork { get; set; }

        [DataMember(Name = "preferred_cache")]
        public string PreferredCache { get; set; }

        [DataMember(Name = "aliases")]
        public string[] Aliases { get; set; }
    }

    internal static class InstanceDiscovery
    {
        public const string DefaultTrustedAuthority = "login.microsoftonline.com";

        private static HashSet<string> WhitelistedAuthorities = new HashSet<string>(new[]
        {
            "login.windows.net", // Microsoft Azure Worldwide - Used in validation scenarios where host is not this list 
            "login.chinacloudapi.cn", // Microsoft Azure China
            "login.microsoftonline.de", // Microsoft Azure Blackforest
            "login-us.microsoftonline.com", // Microsoft Azure US Government - Legacy
            "login.microsoftonline.us", // Microsoft Azure US Government
            "login.microsoftonline.com" // Microsoft Azure Worldwide
        });

        public const string AuthorizeEndpointTemplate = "https://{host}/{tenant}/oauth2/authorize";

        // The following cache could be private, but we keep it public so that internal unit test can take a peek into it
        public static readonly ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry> InstanceCache =
            new ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry>(); // Keys are host strings

        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public static async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntry(string host, bool validateAuthority,
            CallState callState)
        {
            InstanceDiscoveryMetadataEntry entry = null;
            if (!InstanceCache.TryGetValue(host, out entry))
            {
                await semaphore.WaitAsync(); // SemaphoreSlim.WaitAsync() will not block current thread
                try
                {
                    if (!InstanceCache.TryGetValue(host, out entry))
                    {
                        await DiscoverAsync(host, validateAuthority, callState);
                        InstanceCache.TryGetValue(host, out entry);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
            return entry;
        }

        private static async Task DiscoverAsync(string host, bool validateAuthority,
            CallState callState) // No return value. Modifies InstanceCache directly.
        {
            string tentativeAuthorizeEndpoint = AuthorizeEndpointTemplate.Replace("{host}", host);
            string instanceDiscoveryEndpoint =
                ("https://{host}/common/discovery/instance?api-version=1.1&authorization_endpoint=" +
                 tentativeAuthorizeEndpoint)
                .Replace("{host}", WhitelistedAuthorities.Contains(host) ? host : DefaultTrustedAuthority);
            var client = new AdalHttpClient(instanceDiscoveryEndpoint, callState);
            InstanceDiscoveryResponse discoveryResponse = null;
            try
            {
                discoveryResponse = await client.GetResponseAsync<InstanceDiscoveryResponse>().ConfigureAwait(false);
                if (validateAuthority && discoveryResponse.TenantDiscoveryEndpoint == null)
                {
                    // hard stop here
                    throw new AdalException(AdalError.AuthorityNotInValidList);
                }
            }
            catch (AdalServiceException ex)
            {
                if (validateAuthority)
                {
                    // hard stop here
                    throw new AdalException(
                        (ex.ErrorCode == "invalid_instance")
                            ? AdalError.AuthorityNotInValidList
                            : AdalError.AuthorityValidationFailed, ex);
                }
            }

            foreach (var entry in discoveryResponse?.Metadata ?? Enumerable.Empty<InstanceDiscoveryMetadataEntry>())
            {
                foreach (var alias in entry?.Aliases ?? Enumerable.Empty<string>())
                {
                    InstanceCache.TryAdd(alias, entry);
                }
            }
            InstanceCache.TryAdd(host, new InstanceDiscoveryMetadataEntry
            {
                PreferredNetwork = host,
                PreferredCache = host,
                Aliases = null
            });
        }
    }
}
