// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if SUPPORTS_CONFIDENTIAL_CLIENT
using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal static class RdpCertificateReader
    {
        internal const string CertificateUnavailable = "managed_identity_rdp_certificate_unavailable";
        internal const string CertificateNotFound = EndorsedUnattestedManagedIdentitySource.RdpCertificateNotFound;

        internal static X509Certificate2 Read(ILoggerAdapter logger, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if NET8_0_OR_GREATER
            if (OperatingSystem.IsLinux())
            {
                return LinuxRdpCertificateReader.Read(logger, cancellationToken);
            }

            if (!OperatingSystem.IsWindows())
#else
            if (!DesktopOsHelper.IsWindows())
#endif
            {
                throw new MsalClientException(MsalError.MtlsNotSupportedForManagedIdentity,
                    "Automatic RDP certificate selection requires Windows or Linux with .NET 8 or later.");
            }

            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\cimv2\TerminalServices",
                    "SELECT SSLCertificateSHA1Hash FROM Win32_TSGeneralSetting WHERE TerminalName = 'RDP-tcp'");
                searcher.Options.Timeout = TimeSpan.FromSeconds(30);
                using var listeners = searcher.Get();
                if (listeners.Count == 0)
                {
                    throw new MsalClientException(CertificateNotFound, "No RDP-Tcp listener was found.");
                }
                if (listeners.Count != 1)
                {
                    throw new MsalClientException(CertificateUnavailable,
                        "Expected exactly one RDP-Tcp listener. No binding key was created.");
                }

                string thumbprint = null;
                foreach (ManagementObject listener in listeners)
                {
                    using (listener)
                    {
                        thumbprint = listener["SSLCertificateSHA1Hash"] as string;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                logger.Info("[ManagedIdentity] Selecting the configured RDP listener certificate.");
                return FindCertificate(thumbprint, name =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var store = new X509Store(name, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                    return store.Certificates;
                }, logger);
            }
            catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.InvalidNamespace ||
                ex.ErrorCode == ManagementStatus.InvalidClass || ex.ErrorCode == ManagementStatus.NotFound)
            {
                throw new MsalClientException(CertificateNotFound, "No RDP listener configuration was found.", ex);
            }
            catch (Exception ex) when (ex is ManagementException || ex is COMException ||
                ex is UnauthorizedAccessException || ex is SecurityException || ex is CryptographicException)
            {
                throw new MsalClientException(CertificateUnavailable,
                    "The configured RDP certificate could not be read using the current Windows identity. " +
                    "No key was created and no certificate or private-key permissions were changed.", ex);
            }
        }

        internal static X509Certificate2 FindCertificate(
            string thumbprint, Func<string, X509Certificate2Collection> readStore, ILoggerAdapter logger)
        {
            thumbprint = string.Concat((thumbprint ?? string.Empty).Where(c => !char.IsWhiteSpace(c)));
            if (thumbprint.Length == 0)
            {
                throw new MsalClientException(CertificateNotFound, "The RDP-Tcp listener has no configured certificate.");
            }
            if (thumbprint.Length != 40 || !thumbprint.All(Uri.IsHexDigit))
            {
                throw new MsalClientException(CertificateUnavailable,
                    "The RDP-Tcp listener did not report a valid certificate thumbprint.");
            }

            foreach (string name in new[] { "Remote Desktop", "My" })
            {
                X509Certificate2Collection certificates;
                try
                {
                    certificates = readStore(name);
                }
                catch (CryptographicException ex) when (ex.HResult == unchecked((int)0x80070002))
                {
                    logger.Warning($"Certificate store LocalMachine\\{name} is absent; checking the next store.");
                    continue;
                }

                try
                {
                    foreach (var certificate in certificates)
                    {
                        if (string.Equals(certificate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
                        {
                            EndorsedUnattestedManagedIdentitySource.ValidateCertificate(certificate);
                            return new X509Certificate2(certificate);
                        }
                    }
                }
                finally
                {
                    foreach (var certificate in certificates)
                    {
                        certificate.Dispose();
                    }
                }
            }

            throw new MsalClientException(CertificateNotFound,
                "The configured RDP listener certificate was not found.");
        }
    }
}
#endif
