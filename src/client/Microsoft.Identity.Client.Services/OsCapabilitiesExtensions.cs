// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class OsCapabilitiesExtensions // TODO MSAL5: move these to their parent types
    {
        /// <summary>
        /// Returns true if MSAL can use a system browser.
        /// </summary>
        /// <remarks>
        /// On Windows, Mac and Linux a system browser can always be used, except in cases where there is no UI, e.g. SSH connection.
        /// On Android, the browser must support tabs.
        /// </remarks>
        public static bool IsSystemWebViewAvailable(this IPublicClientApplication publicClientApplication)
        {
            if (publicClientApplication is PublicClientApplication pca)
            {
                return pca.IsSystemWebViewAvailable;
            }

            throw new ArgumentException("This extension method is only available for the PublicClientApplication implementation " +
                "of the IPublicClientApplication interface.");

        }

        /// <summary>
        /// Returns true if MSAL can use an embedded webview (browser). 
        /// </summary>
        /// <remarks>
        /// Currently there are no embedded webviews on Mac and Linux. On Windows, app developers or users should install 
        /// the WebView2 runtime and this property will inform if the runtime is available, see https://aka.ms/msal-net-webview2
        /// </remarks>
        public static bool IsEmbeddedWebViewAvailable(this IPublicClientApplication publicClientApplication)
        {
            if (publicClientApplication is PublicClientApplication pca)
            {
                return pca.IsEmbeddedWebViewAvailable();
            }

            throw new ArgumentException("This extension method is only available for the PublicClientApplication implementation " +
                "of the IPublicClientApplication interface.");
        }

        /// <summary>
        /// Returns false when the program runs in headless OS, for example when SSH-ed into a Linux machine.
        /// Browsers (webviews) and brokers cannot be used if there is no UI support. 
        /// Instead, please use <see cref="PublicClientApplication.AcquireTokenWithDeviceCode(IEnumerable{string}, Func{DeviceCodeResult, Task})"/>
        /// or <see cref="PublicClientApplication.AcquireTokenByIntegratedWindowsAuth(IEnumerable{string})"/>
        /// </summary>
        public static bool IsUserInteractive(this IPublicClientApplication publicClientApplication)
        {
            if (publicClientApplication is PublicClientApplication pca)
            {
                return pca.IsUserInteractive();
            }

            throw new ArgumentException("This extension method is only available for the PublicClientApplication implementation " +
                "of the IPublicClientApplication interface.");
        }

       

        /// <summary>
        /// Returns the certificate used to create this <see cref="ConfidentialClientApplication"/>, if any.
        /// </summary>
        public static X509Certificate2 GetCertificate(this IConfidentialClientApplication confidentialClientApplication)
        {

            if (confidentialClientApplication is ConfidentialClientApplication cca)
            {
                return cca.Certificate;
            }

            throw new ArgumentException("This extension method is only available for the ConfidentialClientApplication implementation " +
                "of the IConfidentialClientApplication interface.");
        }
    }
}
