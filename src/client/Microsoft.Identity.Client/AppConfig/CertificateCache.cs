// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if NET6_0 || NET6_WIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.AppConfig
{
    internal class CertificateCache
    {
        private static readonly Lazy<CertificateCache> s_instance = new Lazy<CertificateCache>(() => new CertificateCache());
        private X509Certificate2 _cachedCertificate;
        private readonly object _certificateLock = new();

        private CertificateCache()
        {
        }

        public static CertificateCache Instance() => s_instance.Value;

        /// <summary>
        /// Manages a cached X.509 certificate, ensuring that the certificate is valid and not expired. 
        /// If the certificate is expired or not present, it disposes of the old certificate and retrieves 
        /// a new one through the provided delegate. The 30-day buffer allows for proactive certificate rotation.
        /// </summary>
        /// <param name="certificateFunc"></param>
        /// <returns></returns>
        public X509Certificate2 GetOrAddCertificate(Func<X509Certificate2> certificateFunc)
        {
            lock (_certificateLock)
            {
                //if cached cert exist and still valid return it
                if (_cachedCertificate != null && !CertificateNeedsRotation(_cachedCertificate))
                {
                    return _cachedCertificate;
                }

                //delete the cached cert if it needs to be rotated
                if (_cachedCertificate != null && CertificateNeedsRotation(_cachedCertificate))
                {
                    // Delete the cached certificate if it is expired
                    _cachedCertificate.Dispose();
                    _cachedCertificate = null;
                }

                //cached the newly created certificate
                _cachedCertificate = certificateFunc();
                return _cachedCertificate;
            }
        }

        private static bool CertificateNeedsRotation(X509Certificate2 certificate, double rotationPercentageThreshold = 70)
        {
            DateTime now = DateTime.UtcNow;

            // Calculate the total duration of the certificate's validity
            TimeSpan certificateLifetime = certificate.NotAfter - certificate.NotBefore;

            // Calculate how much time has passed since the certificate's issuance
            TimeSpan elapsedTime = now - certificate.NotBefore;

            // Calculate the current percentage of the certificate's lifetime that has passed
            double percentageElapsed = (elapsedTime.TotalMilliseconds / certificateLifetime.TotalMilliseconds) * 100.0;

            // Check if the percentage elapsed exceeds the rotation threshold
            return percentageElapsed >= rotationPercentageThreshold;
        }
    }
}
#endif
