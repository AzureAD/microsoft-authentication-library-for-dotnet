// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET8_0_OR_GREATER && SUPPORTS_CONFIDENTIAL_CLIENT
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal static class LinuxRdpCertificateReader
    {
        internal const string ConfigurationPath = "/etc/xrdp/xrdp.ini";

        internal static X509Certificate2 Read(ILoggerAdapter logger, CancellationToken cancellationToken)
        {
            return Read(File.ReadAllText, logger, cancellationToken);
        }

        internal static X509Certificate2 Read(
            Func<string, string> readFile, ILoggerAdapter logger, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var paths = GetCertificatePaths(readFile(ConfigurationPath));
                cancellationToken.ThrowIfCancellationRequested();
                logger.Info("[ManagedIdentity] Selecting the configured xrdp TLS certificate.");
                string certificatePem = readFile(paths.Certificate);
                cancellationToken.ThrowIfCancellationRequested();
                string keyPem = readFile(paths.Key);
                cancellationToken.ThrowIfCancellationRequested();
                using var certificate = X509Certificate2.CreateFromPem(certificatePem, keyPem);
                EndorsedUnattestedManagedIdentitySource.ValidateCertificate(certificate);
                return new X509Certificate2(certificate);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException ||
                ex is SecurityException || ex is CryptographicException || ex is ArgumentException)
            {
                throw new MsalClientException(RdpCertificateReader.CertificateUnavailable,
                    "The xrdp configuration or its PEM certificate/private key could not be read. " +
                    "Configure an existing xrdp TLS certificate with an accessible matching RSA private key. " +
                    "No key was created and no permissions were changed.", ex);
            }
        }

        internal static (string Certificate, string Key) GetCertificatePaths(string configuration)
        {
            string certificate = "/etc/xrdp/cert.pem";
            string key = "/etc/xrdp/key.pem";
            string securityLayer = "negotiate";
            bool inGlobals = false;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var reader = new StringReader(configuration);
            string line;
            while ((line = reader.ReadLine()) is not null)
            {
                // xrdp treats both characters as comment delimiters, including inline comments.
                int comment = line.IndexOfAny(new[] { '#', ';' });
                line = (comment < 0 ? line : line.Substring(0, comment)).Trim();
                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
                {
                    if (inGlobals)
                    {
                        break;
                    }

                    inGlobals = string.Equals(line, "[Globals]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                int separator = line.IndexOf('=');
                if (!inGlobals || separator < 0)
                {
                    continue;
                }

                string name = line.Substring(0, separator).Trim();
                if (!string.Equals(name, "certificate", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(name, "key_file", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(name, "security_layer", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string value = line.Substring(separator + 1).Trim();
                if (!seen.Add(name) || value.StartsWith("$", StringComparison.Ordinal))
                {
                    throw InvalidConfiguration();
                }

                if (string.Equals(name, "security_layer", StringComparison.OrdinalIgnoreCase))
                {
                    securityLayer = value;
                }
                else if (value.Length > 0)
                {
                    // Relative paths and daemon-specific environment expansion are ambiguous
                    // in the application's process. Do not select a different certificate.
                    if (!value.StartsWith("/", StringComparison.Ordinal) ||
                        value.IndexOfAny(new[] { '\0', '[', ']' }) >= 0)
                    {
                        throw InvalidConfiguration();
                    }

                    if (string.Equals(name, "certificate", StringComparison.OrdinalIgnoreCase))
                    {
                        certificate = value;
                    }
                    else
                    {
                        key = value;
                    }
                }
            }

            if (!inGlobals ||
                (!string.Equals(securityLayer, "tls", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(securityLayer, "negotiate", StringComparison.OrdinalIgnoreCase)))
            {
                throw InvalidConfiguration();
            }

            return (certificate, key);
        }

        private static MsalClientException InvalidConfiguration()
        {
            return new MsalClientException(RdpCertificateReader.CertificateUnavailable,
                "Automatic xrdp certificate selection requires a [Globals] section with TLS or negotiate security, " +
                "and unique certificate/key_file settings using absolute PEM paths or the xrdp defaults. " +
                "Environment-variable paths are not supported. No alternative binding key was selected.");
        }
    }
}
#endif
