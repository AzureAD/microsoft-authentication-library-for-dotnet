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
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;

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

        // The following cache could be private, but we keep it public so that internal unit test can take a peek into it.
        // Keys are host strings.
        public static readonly ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry> InstanceCache =
            new ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry>();

        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public static async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntry(string host, bool validateAuthority,
            CallState callState)
        {
            InstanceDiscoveryMetadataEntry entry = null;
            if (!InstanceCache.TryGetValue(host, out entry))
            {
                await semaphore.WaitAsync().ConfigureAwait(false); // SemaphoreSlim.WaitAsync() will not block current thread
                try
                {
                    if (!InstanceCache.TryGetValue(host, out entry))
                    {
                        await DiscoverAsync(host, validateAuthority, callState).ConfigureAwait(false);
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

        public static string FormatAuthorizeEndpoint(string host, string tenant)
        {
            return $"https://{host}/{tenant}/oauth2/authorize";
        }

        // No return value. Modifies InstanceCache directly.
        private static async Task DiscoverAsync(string host, bool validateAuthority, CallState callState)
        {
            string tentativeAuthorizeEndpoint = FormatAuthorizeEndpoint(host, "irrelevant");
            string instanceDiscoveryHost = WhitelistedAuthorities.Contains(host) ? host : DefaultTrustedAuthority;
            string instanceDiscoveryEndpoint =
                $"https://{instanceDiscoveryHost}/common/discovery/instance?api-version=1.1&authorization_endpoint={tentativeAuthorizeEndpoint}";
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
                // The pre-existing implementation (https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/pull/796/files#diff-e4febd8f40f03e71bcae0f990f9690eaL99)
                // has been coded in this way: it catches the AdalServiceException and then translate it into 2 validation-relevant exceptions.
                // So the following implementation absorbs these specific exceptions when the validateAuthority flag is false.
                // All other unexpected exceptions will still bubble up, as always.
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
                foreach (var aliasedAuthority in entry?.Aliases ?? Enumerable.Empty<string>())
                {
                    InstanceCache.TryAdd(aliasedAuthority, entry);
                }
            }

            AddMetadataEntry(host);
        }

        // To populate a host into the cache as-is, when it is not already there
        public static bool AddMetadataEntry(string host)
        {
            return InstanceCache.TryAdd(host, new InstanceDiscoveryMetadataEntry
            {
                PreferredNetwork = host,
                PreferredCache = host,
                Aliases = null
            });
        }
    }
}
