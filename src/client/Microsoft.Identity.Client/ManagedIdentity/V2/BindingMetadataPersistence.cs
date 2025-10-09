// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Provides persistence mechanisms for certificate binding metadata in the Windows certificate store.
    /// This class enables MSAL to store and retrieve relationships between Managed Identities 
    /// and their associated certificates without requiring additional storage.
    /// </summary>
    /// <remarks>
    /// This class uses the X509Certificate2.FriendlyName property to store encoded metadata that links
    /// certificates to specific managed identities and token types. The metadata includes:
    /// - Identity key (hashed for privacy)
    /// - Token type (Bearer or PoP)
    /// - Client ID
    /// - Tenant ID
    /// - MTLS authentication endpoint
    /// 
    /// The persistence mechanism enables MSAL to find previously created certificates for an identity
    /// across application restarts, reducing the need to repeatedly mint new certificates.
    /// </remarks>
    internal static class BindingMetadataPersistence
    {
        // Prefix that identifies certificates managed by MSAL for Managed Identities
        private const string Prefix = "MSAL_MI_MTLS|v1|";
        private const char Sep = '|';

        /// <summary>
        /// Creates a structured FriendlyName value containing encoded binding metadata.
        /// </summary>
        /// <param name="identityKey">The identity key to associate with this certificate</param>
        /// <param name="tokenType">The token type (Bearer or PoP)</param>
        /// <param name="resp">The certificate response containing endpoint and identity information</param>
        /// <returns>A formatted string for use as certificate FriendlyName</returns>
        public static string BuildFriendlyName(string identityKey, string tokenType, CertificateRequestResponse resp)
        {
            try
            {
                if (resp == null || string.IsNullOrEmpty(identityKey) || string.IsNullOrEmpty(tokenType))
                    return null;

                // Hash the identity key for privacy while maintaining stable identification
                string hid = HashId(identityKey);
                
                // Encode the endpoint to avoid conflicts with separator character
                string ep = Base64UrlNoPad(Encoding.UTF8.GetBytes(resp.MtlsAuthenticationEndpoint ?? string.Empty));
                string tenant = resp.TenantId ?? string.Empty;
                string client = resp.ClientId ?? string.Empty;

                return string.Concat(Prefix, tokenType, Sep, hid, Sep, client, Sep, tenant, Sep, ep);
            }
            catch { return null; }
        }

        /// <summary>
        /// Attempts to recover binding metadata from certificates in the store.
        /// Finds the freshest valid certificate matching the identity key and token type.
        /// </summary>
        /// <param name="identityKey">The identity key to search for</param>
        /// <param name="tokenType">The token type (Bearer or PoP)</param>
        /// <param name="logger">Logger for diagnostic information</param>
        /// <param name="resp">Output parameter for the recovered certificate response</param>
        /// <param name="subject">Output parameter for the certificate subject</param>
        /// <param name="thumbprint">Output parameter for the certificate thumbprint</param>
        /// <returns>True if binding metadata was successfully recovered, false otherwise</returns>
        public static bool TryRehydrateFromStore(
            string identityKey,
            string tokenType,
            ILoggerAdapter logger,
            out CertificateRequestResponse resp,
            out string subject,
            out string thumbprint)
        {
            resp = null;
            subject = null;
            thumbprint = null;

            try
            {
                var hid = HashId(identityKey);
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                // Find all certificates with our prefix in the FriendlyName
                var candidates = store.Certificates.OfType<X509Certificate2>()
                    .Where(c => !string.IsNullOrEmpty(c.FriendlyName) &&
                                c.FriendlyName.StartsWith(Prefix, StringComparison.Ordinal))
                    .ToList();

                X509Certificate2 freshest = null;
                CertificateRequestResponse freshestResp = null;

                // Find the freshest valid certificate matching our identity and token type
                foreach (var c in candidates)
                {
                    // Parse the FriendlyName to extract the encoded metadata
                    if (!TryParse(c.FriendlyName, out var tType, out var h, out var clientId, out var tenantId, out var ep))
                        continue;

                    // Must match the requested token type
                    if (!StringComparer.OrdinalIgnoreCase.Equals(tType, tokenType))
                        continue;

                    // Must match the hashed identity key
                    if (!StringComparer.Ordinal.Equals(h, hid))
                        continue;

                    // Certificate must be currently valid
                    if (!MtlsBindingStore.IsCurrentlyValid(c))
                        continue;

                    // Keep track of the freshest certificate (furthest expiration date)
                    if (freshest == null || c.NotAfter.ToUniversalTime() > freshest.NotAfter.ToUniversalTime())
                    {
                        freshest = c;
                        freshestResp = new CertificateRequestResponse
                        {
                            ClientId = clientId,
                            TenantId = tenantId,
                            MtlsAuthenticationEndpoint = ep
                        };
                    }
                }

                if (freshest == null || freshestResp == null)
                    return false;

                resp = freshestResp;
                subject = freshest.Subject;
                thumbprint = freshest.Thumbprint;
                logger?.Info("[IMDSv2] Rehydrated binding metadata from certificate store (FriendlyName tag).");
                return true;
            }
            catch (Exception ex)
            {
                logger?.Verbose(() => $"[IMDSv2] Store rehydration failed: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parses a FriendlyName value to extract the encoded binding metadata.
        /// </summary>
        private static bool TryParse(string friendlyName, out string tokenType, out string hid, out string clientId, out string tenantId, out string endpoint)
        {
            tokenType = hid = clientId = tenantId = endpoint = null;

            if (string.IsNullOrEmpty(friendlyName) || !friendlyName.StartsWith(Prefix, StringComparison.Ordinal))
                return false;

            try
            {
                var payload = friendlyName.Substring(Prefix.Length);
                var parts = payload.Split(Sep);
                if (parts.Length < 5)
                    return false;

                tokenType = parts[0];
                hid = parts[1];
                clientId = parts[2];
                tenantId = parts[3];

                // endpoint is base64-url-no-pad; join remainder in case it contained separators
                var epEncoded = string.Join(Sep.ToString(), parts.Skip(4));
                var epBytes = Base64UrlNoPadDecode(epEncoded);
                endpoint = Encoding.UTF8.GetString(epBytes ?? Array.Empty<byte>());

                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Creates a stable, shortened hash of an identity key for storage efficiency.
        /// </summary>
        private static string HashId(string id)
        {
            using var sha = SHA256.Create();
            var h = sha.ComputeHash(Encoding.UTF8.GetBytes(id ?? string.Empty));
            // 12 bytes (24 hex chars) is plenty for collision avoidance while keeping FriendlyName compact
            return ToHex(h, 12);
        }

        /// <summary>
        /// Converts bytes to a hexadecimal string representation.
        /// </summary>
        private static string ToHex(byte[] bytes, int takeBytes)
        {
            if (bytes == null)
                return string.Empty;
            int n = Math.Max(0, Math.Min(takeBytes, bytes.Length));
            var sb = new StringBuilder(n * 2);
            for (int i = 0; i < n; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Encodes binary data as base64url without padding to avoid separator conflicts.
        /// </summary>
        private static string Base64UrlNoPad(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;
            var s = Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            return s;
        }

        /// <summary>
        /// Decodes base64url-formatted string back to binary data.
        /// </summary>
        private static byte[] Base64UrlNoPadDecode(string s)
        {
            if (string.IsNullOrEmpty(s))
                return Array.Empty<byte>();
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2:
                    s += "==";
                    break;
                case 3:
                    s += "=";
                    break;
            }
            try
            { return Convert.FromBase64String(s); }
            catch { return Array.Empty<byte>(); }
        }
    }
}
